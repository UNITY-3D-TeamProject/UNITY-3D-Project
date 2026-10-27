using System.Collections.Generic;
using UnityEngine;

namespace AI.Boss.BossB
{
    /// <summary>
    /// B 의 등장 연출. 보스전이 시작되기 전에 A 머리 위 공중에서 Spell 모션을 재생한 뒤 착지 지점으로 내려와 쿵 하고 내려앉고, 잠깐 멈춘다.
    /// 아레나가 연결되기 전에 쓰므로 칸 정보는 쓰지 않는다. 연출 동안 맞지 않도록 B 의 콜라이더를 모두 꺼 둔다.
    /// 공중 위치와 착지 위치는 BossBController.PlayEntrance 로 받는다.
    /// 몸은 직접 옮긴다: 공중에서는 제자리에 고정, 내려올 때는 중력 가속도로 떨어지면서 수평으로 착지 위치에 맞춘다.
    /// </summary>
    public class BossBEntrance : BossBSkillBase
    {
        #region Nested Types
        private enum EEntrancePhase
        {
            Hover,
            Fall,
            Landed,
        }
        #endregion

        #region Serialized Fields
        [Header("Entrance")]
        [Tooltip("공중에서 Spell 모션을 재생하는 시간(초). Spell 클립 길이에 맞춘다")]
        [SerializeField, Min(0.0f)] private float _hoverSeconds = 1.2f;
        [Tooltip("착지한 뒤 스킬이 끝나기까지 멈춰 있는 시간(초)")]
        [SerializeField, Min(0.0f)] private float _landingPauseSeconds = 0.5f;
        #endregion

        #region Private Fields
        private readonly List<Collider> _disabledColliders = new List<Collider>();
        private CharacterController _characterController;
        private EEntrancePhase _phase;
        private Vector3 _hoverPosition;
        private Vector3 _landingPosition;
        private float _gravity;
        private float _fallSeconds;
        private float _fallElapsed;
        private float _timer;
        #endregion

        #region Public Methods
        /// <summary>
        /// 등장 연출의 공중 위치와 착지 위치를 정한다. 스킬을 발동하기 전에 부른다.
        /// </summary>
        public void SetPositions(Vector3 hoverPosition, Vector3 landingPosition)
        {
            _hoverPosition = hoverPosition;
            _landingPosition = landingPosition;
        }
        #endregion

        #region Protected Methods
        protected override void Execute()
        {
            if (!SelfMotor) return;

            _characterController = SelfMotor.GetComponentInParent<CharacterController>();
            if (!_characterController) return;

            // 중력 가속도로 떨어지는 데 걸리는 시간. 높이가 0 이하면 바로 착지한다
            _gravity = Mathf.Max(SelfMotor.Gravity, 0.01f);
            float fallHeight = Mathf.Max(0.0f, _hoverPosition.y - _landingPosition.y);
            _fallSeconds = Mathf.Sqrt(2.0f * fallHeight / _gravity);

            float holdSeconds = _hoverSeconds + _fallSeconds + _landingPauseSeconds;
            if (!BeginRun(false, holdSeconds, false)) return;

            base.Execute();

            DisableColliders();
            _characterController.transform.position = _hoverPosition;

            _phase = EEntrancePhase.Hover;
            _timer = _hoverSeconds;
        }

        protected override void Tick(float deltaTime)
        {
            switch (_phase)
            {
                case EEntrancePhase.Hover:
                    TickHover(deltaTime);
                    break;
                case EEntrancePhase.Fall:
                    TickFall(deltaTime);
                    break;
                default:
                    TickLanded(deltaTime);
                    break;
            }
        }

        protected override void OnRunEnded()
        {
            // 착지 전에 끊겼어도 공중에 남지 않게 착지 위치로 내려놓고 콜라이더를 되살린다
            if (_characterController && (_phase != EEntrancePhase.Landed))
            {
                _characterController.transform.position = _landingPosition;
            }

            RestoreColliders();
            _phase = EEntrancePhase.Hover;
        }
        #endregion

        #region Private Methods
        private void TickHover(float deltaTime)
        {
            _timer -= deltaTime;
            if (_timer > 0.0f) return;

            _phase = EEntrancePhase.Fall;
            _fallElapsed = 0.0f;
            TickFall(0.0f);
        }

        private void TickFall(float deltaTime)
        {
            _fallElapsed += deltaTime;
            if (_fallElapsed >= _fallSeconds)
            {
                Land();
                return;
            }

            float ratio = _fallElapsed / _fallSeconds;
            float height = _hoverPosition.y - (0.5f * _gravity * _fallElapsed * _fallElapsed);

            Vector3 position = Vector3.Lerp(_hoverPosition, _landingPosition, ratio);
            position.y = Mathf.Max(height, _landingPosition.y);
            _characterController.transform.position = position;
        }

        private void Land()
        {
            _characterController.transform.position = _landingPosition;
            RestoreColliders();

            _phase = EEntrancePhase.Landed;
            _timer = _landingPauseSeconds;
            Boss.NotifyEntranceLanded();
        }

        private void TickLanded(float deltaTime)
        {
            _timer -= deltaTime;
            if (_timer <= 0.0f) EndRun();
        }

        // 켜져 있던 콜라이더만 끈다. 연출 동안 플레이어 사격·충돌을 받지 않는다
        private void DisableColliders()
        {
            _disabledColliders.Clear();

            foreach (Collider body in Boss.GetComponentsInChildren<Collider>())
            {
                if (!body.enabled) continue;

                body.enabled = false;
                _disabledColliders.Add(body);
            }
        }

        private void RestoreColliders()
        {
            foreach (Collider body in _disabledColliders)
            {
                if (body) body.enabled = true;
            }
            _disabledColliders.Clear();
        }
        #endregion
    }
}
