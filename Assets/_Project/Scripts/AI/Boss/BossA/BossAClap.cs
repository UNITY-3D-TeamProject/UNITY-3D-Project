using System.Collections.Generic;
using UnityEngine;
using Movement;
using Core.Effect;
using AI.Boss.Logic;

namespace AI.Boss.BossA
{
    /// <summary>
    /// A6 박수치기. 한 줄의 양 끝 칸을 양 주먹으로 쾅 내려친 뒤(1타) 주먹을 펴고 바닥 위로 줄을 따라 쓸어 가운데로 모아 박수친다(2타).
    /// 칸은 무너뜨리지 않고 손도 머물지 않으며 어깨 포격도 없다. 최종 단계 전용이다.
    /// 예고는 맞는 칸에만 띄운다. 1타는 양 끝 칸, 1타 직후 그 사이 칸으로 바꿔 2타 간격 동안 채운다.
    /// 다른 내려치기와 구분되게 예고 시작부터 손이 주먹을 쥐고 떠오르고, 사이 칸 위에 상자 예고를 박수 순간까지 세운다.
    /// 1타는 양 끝 칸 위에 있으면 맞는다. 2타는 B 돌진처럼 쓸어 오는 손에 닿으면 맞고(한 패턴에 한 번),
    /// 그랩처럼 붙잡혀 손바닥 앞에 붙은 채 가운데까지 끌려간다. 박수 순간 놓고 이동 잠금은 _holdSeconds 뒤에 풀린다.
    /// </summary>
    public class BossAClap : BossAPatternBase
    {
        #region Serialized Fields
        [Tooltip("임시값. 스펙에 정해진 시간이 없어 A2·A4 와 같게 둔다")]
        [SerializeField, Min(0.0f)] private float _telegraphSeconds = 1.5f;
        [Tooltip("1타에서 2타(손이 가운데서 맞부딪치는 순간)까지의 간격(초)")]
        [SerializeField, Min(0.0f)] private float _strikeInterval = 1.0f;
        [Tooltip("임시값. 맞부딪친 두 손 사이 거리(m). 손바닥이 서로 닿아 보이게 맞춘다")]
        [SerializeField, Min(0.0f)] private float _clapGap = 8.0f;
        [Tooltip("임시값. 2타에서 손(이 오브젝트) 중심과 플레이어 사이 수평 거리가 이 안이면 닿은 것으로 본다(m)")]
        [SerializeField, Min(0.0f)] private float _hitRadius = 6.0f;
        [Tooltip("임시값. 박수 순간 놓은 뒤에도 이동을 잠가 두는 시간(초). 그랩과 같게 둔다")]
        [SerializeField, Min(0.0f)] private float _holdSeconds = 0.7f;

        [SerializeField] private SOEffectData _clapVfx;

        [Header("Wall Telegraph")]
        [Tooltip("1타 예고부터 박수 순간까지 사이 칸 위에 세우는 상자 예고(가운데 기준 큐브). 비워 두면 바닥 예고만 띄운다")]
        [SerializeField] private GameObject _wallPrefab;
        [Tooltip("임시값. 상자 예고 높이(m). 바닥에서부터 이만큼 세운다")]
        [SerializeField, Min(0.0f)] private float _wallHeight = 6.0f;
        #endregion

        #region Private Fields
        private readonly BossHand[] _clapHands = new BossHand[2];
        private readonly Vector3[] _palmDirections = new Vector3[2];
        private int[] _innerTiles;
        private int _pendingClapCount;
        private bool _isClapping;
        private bool _isHitApplied;
        private Collider[] _passThroughHandColliders;
        private Collider _passThroughPlayerCollider;
        private int _captureHandIndex = -1;
        private float _floorY;
        private float _heldHeight;
        private float _holdRemaining;
        private bool _isPlayerHeld;
        private bool _isPlayerLocked;
        private Transform _wall;
        #endregion

        #region Properties
        protected override float TelegraphSeconds => _telegraphSeconds;
        protected override float StaySeconds => 0.0f;
        protected override bool IsFistStrike => true;

        // 예고가 시작될 때 주먹을 쥐고 미리 떠올라 다른 내려치기 패턴과 구분되게 한다
        protected override bool IsHoverDuringTelegraph => true;
        #endregion

        #region Unity Lifecycle
        protected override void OnDisable()
        {
            base.OnDisable();
            SetHandPassThrough(false);
        }

        private void OnDestroy()
        {
            if (_wall) Destroy(_wall.gameObject);
        }
        #endregion

        #region Public Methods
        public override void Stop()
        {
            _isClapping = false;
            HideWall();
            ReleasePlayer();
            base.Stop();
        }
        #endregion

        #region Protected Methods
        protected override void BuildTargets()
        {
            base.BuildTargets();

            int[] tiles = Controller.CurrentChoice.Tiles;
            _innerTiles = new int[ArenaGrid.SIZE - 2];
            for (int i = 0; i < _innerTiles.Length; i++)
            {
                _innerTiles[i] = tiles[i + 1];
            }

            // 상자 예고는 1타 예고부터 박수 순간까지 사이 칸 위에 세워 둔다
            GetRowLayout(tiles, out Vector3 middle, out Vector3 rowDirection, out float tileStep);
            ShowWall(middle, rowDirection, tileStep);
        }

        protected override int[] GetTargetTiles()
        {
            // 1타는 줄의 양 끝 칸만 맞는다
            int[] tiles = Controller.CurrentChoice.Tiles;
            return new[] { tiles[0], tiles[ArenaGrid.SIZE - 1] };
        }

        protected override void OnImpact()
        {
            // 1타는 양 끝 칸. 칸은 무너뜨리지 않는다
            int[] tiles = Controller.CurrentChoice.Tiles;
            if (IsPlayerOnTile(tiles[0]) || IsPlayerOnTile(tiles[ArenaGrid.SIZE - 1])) ApplyHitToPlayer();
        }

        protected override bool StartFollowUp()
        {
            // 손이 가운데에서 맞부딪칠 때까지 사이 칸 예고를 채운다
            ReplaceTelegraph(_innerTiles, _strikeInterval);

            // 줄 방향(왼손 쪽 끝 → 오른손 쪽 끝)을 따라 바닥 위로 쓸어 가운데 양옆으로 모은다
            GetRowLayout(Controller.CurrentChoice.Tiles, out Vector3 middle, out Vector3 rowDirection, out _);

            Vector3 halfGap = rowDirection * (_clapGap * 0.5f);

            _isHitApplied = false;
            _isClapping = true;
            _pendingClapCount = 0;
            _captureHandIndex = -1;
            _floorY = middle.y;
            _clapHands[0] = Controller.GetHand(true);
            _clapHands[1] = Controller.GetHand(false);
            _palmDirections[0] = rowDirection;
            _palmDirections[1] = -rowDirection;

            // 모이는 손 콜라이더가 플레이어를 밀고 가 두 손 사이에 끼우지 않게 손이 돌아올 때까지 서로 통과시킨다
            SetHandPassThrough(true);

            StartClap(_clapHands[0], middle - halfGap, _palmDirections[0]);
            StartClap(_clapHands[1], middle + halfGap, _palmDirections[1]);

            if (_pendingClapCount == 0) EndClap();
            return true;
        }

        protected override void OnTick(float deltaTime)
        {
            // 놓은 뒤의 이동 잠금은 패턴 단계와 따로 센다
            if (_isPlayerLocked && !_isPlayerHeld)
            {
                _holdRemaining -= deltaTime;
                if (_holdRemaining <= 0.0f) ReleasePlayer();
            }

            // 패턴이 끝나거나 끊긴 뒤 두 손이 평소 자리로 돌아오면 충돌을 되돌린다
            if (!_isClapping && (_passThroughPlayerCollider != null) && AreClapHandsIdle()) SetHandPassThrough(false);

            if (!_isClapping) return;

            if (_captureHandIndex >= 0) DragPlayer();
            else TryCapture();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 박수 줄의 가운데 칸 중심, 줄 방향(왼손 쪽 끝 → 오른손 쪽 끝, 수평 단위 벡터), 이웃 칸 중심 사이 거리를 구한다.
        /// </summary>
        private void GetRowLayout(int[] tiles, out Vector3 middle, out Vector3 rowDirection, out float tileStep)
        {
            Vector3 leftEnd = Arena.GetTileCenter(tiles[0]);
            Vector3 rightEnd = Arena.GetTileCenter(tiles[ArenaGrid.SIZE - 1]);
            middle = Arena.GetTileCenter(tiles[ArenaGrid.SIZE / 2]);
            rowDirection = rightEnd - leftEnd;
            rowDirection.y = 0.0f;
            tileStep = rowDirection.magnitude / (ArenaGrid.SIZE - 1);
            rowDirection.Normalize();
        }

        private void StartClap(BossHand hand, Vector3 position, Vector3 palmDirection)
        {
            if (!hand) return;

            _pendingClapCount++;
            hand.Clap(position, palmDirection, _strikeInterval, OnHandClapDone);
        }

        private void OnHandClapDone()
        {
            _pendingClapCount--;
            if (_pendingClapCount > 0) return;

            EndClap();
        }

        private void EndClap()
        {
            if (!_isClapping) return;

            if (_clapHands[0] && _clapHands[1])
            {
                Vector3 position = (_clapHands[0].ClapPoint + _clapHands[1].ClapPoint) * 0.5f;
                PlayImpactSound(position);
                if (_clapVfx && _clapVfx.Prefab)
                    EffectManager.Instance?.Play(_clapVfx, position, Quaternion.LookRotation(_palmDirections[0]) * _clapVfx.Prefab.transform.localRotation);
            }

            // 맞부딪친 순간 붙잡힌 플레이어를 가운데에 놓는다
            if (_captureHandIndex >= 0) DragPlayer();
            else TryCapture();
            DropPlayer();
            HideWall();

            _isClapping = false;
            FinishFollowUp();
        }

        /// <summary>
        /// 쓸어 오는 손에 플레이어가 닿았으면 피해를 주고 붙잡는다. 모터가 없으면 피해만 준다.
        /// </summary>
        private void TryCapture()
        {
            if (_isHitApplied) return;

            Transform body = Situation.PlayerBody;
            if (!body) return;

            for (int i = 0; i < _clapHands.Length; i++)
            {
                BossHand hand = _clapHands[i];
                if (!hand) continue;

                Vector3 offset = body.position - hand.transform.position;
                offset.y = 0.0f;
                if (offset.sqrMagnitude > (_hitRadius * _hitRadius)) continue;

                _isHitApplied = true;
                ApplyHitToPlayer();

                // 모터가 없으면 끌고 갈 수 없으므로 피해만 준다
                if (!Situation.PlayerMotor) return;

                _captureHandIndex = i;
                _heldHeight = Mathf.Max(0.0f, body.position.y - _floorY);
                LockPlayer();
                _isPlayerHeld = true;
                Situation.SetPlayerHeld(true);
                DragPlayer();
                return;
            }
        }

        /// <summary>
        /// 붙잡힌 플레이어를 잡은 손의 손바닥 앞(맞부딪칠 때 가운데가 되는 자리)으로 옮긴다. 손은 이 뒤에 움직일 수 있어 한 프레임 뒤따라간다.
        /// </summary>
        private void DragPlayer()
        {
            Transform body = Situation.PlayerBody;
            CharacterMotor motor = Situation.PlayerMotor;
            BossHand hand = _clapHands[_captureHandIndex];
            if (!body || !motor || !hand) return;

            Vector3 handPosition = hand.transform.position;
            Vector3 target = new Vector3(handPosition.x, _floorY + _heldHeight, handPosition.z) +
                             (_palmDirections[_captureHandIndex] * (_clapGap * 0.5f));
            motor.AddExternalDisplacement(target - body.position);
        }

        /// <summary>
        /// 붙잡힌 플레이어를 놓는다. 중력이 돌아오고, 이동 잠금은 _holdSeconds 뒤에 풀린다.
        /// </summary>
        private void DropPlayer()
        {
            _captureHandIndex = -1;
            ReleaseHeldPlayer();
            if (_isPlayerLocked) _holdRemaining = _holdSeconds;
        }

        private void LockPlayer()
        {
            _holdRemaining = float.PositiveInfinity;
            _isPlayerLocked = true;
            Situation.SetPlayerMoveLocked(true);
        }

        /// <summary>
        /// 붙잡음과 이동 잠금을 모두 바로 푼다.
        /// </summary>
        private void ReleasePlayer()
        {
            _captureHandIndex = -1;
            ReleaseHeldPlayer();
            if (!_isPlayerLocked) return;

            _isPlayerLocked = false;
            if (Controller && (Controller.Situation != null)) Controller.Situation.SetPlayerMoveLocked(false);
        }

        private void ReleaseHeldPlayer()
        {
            if (!_isPlayerHeld) return;

            _isPlayerHeld = false;
            if (Controller && (Controller.Situation != null)) Controller.Situation.SetPlayerHeld(false);
        }

        /// <summary>
        /// 사이 칸 전체를 덮고 바닥에서 _wallHeight 만큼 선 상자 예고를 켠다.
        /// </summary>
        /// <param name="middle">줄 가운데 칸 중심(바닥 높이)</param>
        /// <param name="rowDirection">줄 방향(수평 단위 벡터)</param>
        /// <param name="tileStep">이웃 칸 중심 사이 거리(m)</param>
        private void ShowWall(Vector3 middle, Vector3 rowDirection, float tileStep)
        {
            if (!_wallPrefab || (_wallHeight <= 0.0f)) return;

            // 보스가 움직여도 끌려가지 않게 부모 없이 월드에 둔다
            if (!_wall) _wall = Instantiate(_wallPrefab).transform;

            _wall.SetPositionAndRotation(
                middle + (Vector3.up * (_wallHeight * 0.5f)),
                Quaternion.LookRotation(rowDirection, Vector3.up));
            _wall.localScale = new Vector3(tileStep, _wallHeight, tileStep * _innerTiles.Length);
            _wall.gameObject.SetActive(true);
        }

        private void HideWall()
        {
            if (_wall) _wall.gameObject.SetActive(false);
        }

        private bool AreClapHandsIdle()
        {
            foreach (BossHand hand in _clapHands)
            {
                if (hand && !hand.IsIdle) return false;
            }
            return true;
        }

        /// <summary>
        /// 두 손의 콜라이더와 플레이어 콜라이더 사이 충돌을 끄거나 되돌린다. 다른 대상(총알 등)과의 충돌은 그대로다.
        /// </summary>
        private void SetHandPassThrough(bool isOn)
        {
            if (isOn)
            {
                // 이미 꺼 둔 상태면 되돌리기 전 목록을 덮어쓰지 않는다
                if (_passThroughPlayerCollider) return;

                Transform body = Situation.PlayerBody;
                _passThroughPlayerCollider = body ? body.GetComponent<Collider>() : null;

                var colliders = new List<Collider>();
                foreach (BossHand hand in _clapHands)
                {
                    if (hand) colliders.AddRange(hand.GetComponentsInChildren<Collider>());
                }
                _passThroughHandColliders = colliders.ToArray();
            }

            if (!_passThroughPlayerCollider || (_passThroughHandColliders == null)) return;

            foreach (Collider handCollider in _passThroughHandColliders)
            {
                if (handCollider) Physics.IgnoreCollision(handCollider, _passThroughPlayerCollider, isOn);
            }

            if (isOn) return;

            _passThroughHandColliders = null;
            _passThroughPlayerCollider = null;
        }
        #endregion
    }
}
