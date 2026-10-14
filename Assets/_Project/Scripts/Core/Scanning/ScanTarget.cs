using System.Collections.Generic;
using UnityEngine;

namespace Core.Scanning
{
    /// <summary>
    /// 스캔 파동에 반응하는 대상. 스캔에 닿은 시점부터 스캔 지속 시간만큼 반응을 유지하고, 이후 서서히 사라지게 한다.
    /// Scan 태그가 달려 있으면 Awake 에서 등록 목록(Registered)에 들어가고, ScanWave 는 이 목록만 돌며 판정 콜라이더에 파동이 닿았는지 확인한다.
    /// 어떻게 보일지는 같은 오브젝트에 붙은 ScanReactionBase 들이 결정한다(자식의 반응은 찾지 않는다).
    /// 스킬과 파동의 생명주기와 무관하게 대상이 스스로 타이머를 소유하므로, 파동이 사라져도 끝까지 진행한다.
    /// 재스캔은 유지 시간만 연장하고 처음 스캔의 연출을 그대로 둔다. 스킬 쿨타임으로 스캔이 겹치지 않는 것을 전제로 한다.
    /// </summary>
    public class ScanTarget : MonoBehaviour
    {
        #region Constants
        /// <summary>스캔에 반응하는 대상(ScanTarget 이 붙은 오브젝트)에 다는 태그. ScanWave 는 이 태그가 달린 대상에만 알린다.</summary>
        public const string SCAN_TAG = "Scan";
        #endregion

        #region Static Fields
        // 스캔 파동이 판정할 대상 목록. Awake 에서 등록하고 OnDestroy 에서 뺀다.
        // 대기 중에는 스스로 enabled = false 를 하므로 OnEnable/OnDisable 로 관리하지 않는다. 비활성 대상은 파동 쪽에서 건너뛴다.
        private static readonly List<ScanTarget> RegisteredTargets = new List<ScanTarget>();
        #endregion

        #region Enums
        // 스캔 반응의 진행 단계. Idle -> Active -> FadingOut -> Idle 순으로 돈다.
        private enum EScanState
        {
            // 스캔에 닿지 않은 평소 상태. Update 가 꺼져 있다.
            Idle,
            // 닿은 뒤 활성 시간 동안. 세기가 0 에서 1 로 올라간다.
            Active,
            // 활성 시간이 끝나 사라지는 중. 세기가 1 에서 0 으로 내려간다.
            FadingOut,
        }
        #endregion

        #region Serialized Fields
        [Header("Fade")]
        [Tooltip("닿은 뒤 반응 세기가 0에서 1이 되기까지의 시간(초). 튐 방지용 짧은 값")]
        [SerializeField, Min(0.0f)] private float _fadeInTime = 0.2f;
        [Tooltip("활성 시간이 끝난 뒤 반응이 완전히 사라지기까지의 시간(초)")]
        [SerializeField, Min(0.0f)] private float _fadeOutTime = 1.0f;

        [Header("References")]
        [Tooltip("이 대상의 반응 목록. 비어 있으면 Awake 에서 같은 오브젝트의 ScanReactionBase 를 모두 찾는다.")]
        // 스캔에 닿았을 때 눈에 보이는 반응 하나를 만드는 부모 클래스 
        // 여러 반응이 필요한경우를 대비해서 배열로 만든다.
        [SerializeField] private ScanReactionBase[] _reactions;
        [Tooltip("파동이 닿았는지 판정할 콜라이더. 비어 있으면 Awake 에서 이 오브젝트와 자식의 콜라이더를 모두 쓴다. 몬스터 약점은 몸체 콜라이더(CharacterController)를 연결한다.")]
        // 약점처럼 자기 콜라이더가 스캔 전에 꺼져 있는 대상은 다른 오브젝트의 콜라이더로 판정해야 하므로 직접 지정할 수 있게 한다.
        [SerializeField] private Collider[] _scanColliders;
        #endregion

        #region Private Fields
        // 현재 진행 단계
        private EScanState _state = EScanState.Idle;
        // 마지막으로 받은 스캔 번호. 새 스캔인지 같은 스캔의 반복인지 구분하는 데 쓴다.
        private int _lastScanId;
        // 반응을 시작시킨(대기 상태에서 처음 닿은) 스캔 번호. 재스캔은 시간만 연장하므로 이 스캔의 정보만 반응에 전달한다.
        private int _revealScanId;
        // 활성 상태가 끝나는 시각(Time.time 기준)
        private float _activeUntil;
        // 현재 반응 세기(0~1). 모든 반응에 전달된다.
        private float _fade;
        #endregion

        #region Properties
        /// <summary>스캔 파동이 판정할 대상 목록. 비활성 오브젝트도 들어 있으므로 쓰는 쪽에서 확인한다.</summary>
        // 외부에서 목록을 수정(Add,Remove)하지 못하도록 읽기 전용으로 감싸서 제공하는 C# 인터페이스
        // ScanWave.cs 의 Update() 내부에서 읽어서 쓰고 있다.
        public static IReadOnlyList<ScanTarget> Registered => RegisteredTargets;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // 자식에 별도 ScanTarget 이 있어도 서로의 반응을 건드리지 않도록 같은 오브젝트의 반응만 찾는다.
            if ((_reactions == null) || (_reactions.Length == 0))
            {
                // ScanReaction에 대한 것들을 가져와서 _reactions 배열에 담는다.
                _reactions = GetComponents<ScanReactionBase>();
            }

            if ((_scanColliders == null) || (_scanColliders.Length == 0))
            {
                // 자식 오브젝트의 콜라이더까지 전부 찾는다. (비활성화 되어있는 오브젝트까지 찾겠다.)
                _scanColliders = GetComponentsInChildren<Collider>(true);
            }

            Debug.Assert(_reactions.Length > 0, $"[{name}] 스캔 반응(ScanReactionBase)이 없습니다.");
            Debug.Assert(_scanColliders.Length > 0, $"[{name}] 스캔 판정 콜라이더가 없습니다.");
            Debug.Assert(CompareTag(SCAN_TAG), $"[{name}] '{SCAN_TAG}' 태그가 없어 스캔 파동이 이 대상을 찾지 못합니다.");

            // Scan 태그가 달린 대상만 파동의 판정 목록에 들어간다.
            if (CompareTag(SCAN_TAG)) RegisteredTargets.Add(this);

            // 닿기 전에는 Update 비용을 쓰지 않는다.
            // 이 컴포넌트를 끈다. => Update 안돌아간다.
            enabled = false;
        }

        private void OnDestroy()
        {
            RegisteredTargets.Remove(this);
        }

        private void Update()
        {
            if (_state == EScanState.Active)
            {
                // 활성 중에는 세기를 1 로 끌어올린다(짧은 페이드 인).
                _fade = StepFade(1.0f, _fadeInTime);

                // 활성 시간이 끝나면 사라지는 단계로 넘어간다.
                if (Time.time >= _activeUntil)
                {
                    _state = EScanState.FadingOut;
                }
            }
            else if (_state == EScanState.FadingOut)
            {
                // 사라지는 중에는 세기를 0 으로 내린다.
                _fade = StepFade(0.0f, _fadeOutTime);
            }

            // 갱신된 세기를 모든 반응에 전달한다.
            UpdateFade();

            // 완전히 사라졌으면 반응을 끝내고 Update 를 다시 끈다.
            if ((_state == EScanState.FadingOut) && (_fade <= 0.0f))
            {
                EndScan();
            }
        }

        private void OnDisable()
        {
            // 비활성화/파괴로 중간에 끊겨도 반응이 남지 않게 되돌린다.
            if (_state != EScanState.Idle)
            {
                EndScan();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 스캔 파동에 닿았음을 알린다.
        /// 파동이 닿아 있는 동안 프레임당 한 번씩 호출되며(콜라이더가 여러 개여도 한 번), 같은 스캔이 반복해서 들어오므로 안전해야 한다.
        /// </summary>
        /// <param name="hit">닿은 스캔의 정보</param>
        // ScanWave가 닿으면 ScanTarget의 OnScanned를 부른다.
        public void OnScanned(in SScanHit hit)
        {
            // 대기 중이거나, 직전과 다른 번호의 스캔이면 새 스캔으로 본다.
            // 같은 번호는 파동이 닿아 있는 동안 매 프레임 들어오는 반복 호출이다.
            bool isNewScan = (_state == EScanState.Idle) || (hit.ScanId != _lastScanId);
            if (isNewScan)
            {
                BeginScan(hit);
            }
            else if (_state != EScanState.Active)
            {
                // 같은 스캔의 파동이 활성 시간이 끝난 뒤에 한 번 더 닿은 경우는 무시한다.
                return;
            }

            // 재스캔은 시간만 연장하므로, 반응을 시작시킨 스캔이 아니면 연출 값(파동 중심·반경)을 넘기지 않는다.
            if (hit.ScanId != _revealScanId) return;

            // 닿아 있는 동안 파동 반경 같은 최신 값을 각 반응에 넘긴다.
            foreach (ScanReactionBase reaction in _reactions)
            {
                reaction.OnScanUpdate(hit);
            }
        }

        /// <summary>
        /// 판정 콜라이더 중 하나가 파동 구 범위 안에 들어왔는지 확인한다. 꺼진 콜라이더는 판정에서 뺀다.
        /// 범위 상자로 먼저 거르고, ClosestPoint 를 쓸 수 있는 콜라이더는 실제 표면까지의 거리로 확정한다.
        /// </summary>
        /// <param name="origin">파동 중심(월드 좌표)</param>
        /// <param name="radius">파동의 현재 반경(m)</param>
        /// <returns>범위 안에 있으면 true</returns>
        
        // ScanWave.Update()에서 매프레임 계산해서 넘겨준다.
        public bool IsInWaveRange(Vector3 origin, float radius)
        {
            float sqrRadius = radius * radius;

            foreach (Collider scanCollider in _scanColliders)
            {
                // 꺼진 콜라이더는 범위가 0 이라 판정에 쓸 수 없다.
                if (!scanCollider || !scanCollider.enabled || !scanCollider.gameObject.activeInHierarchy) continue;

                // 범위 상자에도 닿지 않으면 실제 표면에도 닿지 않는다.
                if (scanCollider.bounds.SqrDistance(origin) > sqrRadius) continue;

                // ClosestPoint 를 못 쓰는 콜라이더(CharacterController, 오목한 MeshCollider 등)는 범위 상자 결과를 쓴다.
                if (!CanUseClosestPoint(scanCollider)) return true;

                if ((scanCollider.ClosestPoint(origin) - origin).sqrMagnitude <= sqrRadius) return true;
            }

            return false;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Collider.ClosestPoint 가 지원하는 콜라이더인지 확인한다(Box, Sphere, Capsule, 볼록 MeshCollider).
        /// </summary>
        /// <param name="scanCollider">확인할 콜라이더</param>
        /// <returns>지원하면 true</returns>
        private static bool CanUseClosestPoint(Collider scanCollider)
        {
            if (scanCollider is MeshCollider meshCollider) return meshCollider.convex;

            return (scanCollider is BoxCollider) || (scanCollider is SphereCollider) || (scanCollider is CapsuleCollider);
        }

        /// <summary>
        /// 새 스캔에 닿았을 때 타이머를 (재)시작한다. 이미 진행 중이면(재스캔) 반응을 새로 시작하지 않고 시간만 연장한다.
        /// 따라서 OnScanBegin 은 새 스캔마다가 아니라 대기 상태에서 처음 닿았을 때만 호출된다.
        /// 사라지는 중에 재스캔되면 다시 활성 상태로 돌아간다.
        /// </summary>
        /// <param name="hit">닿은 스캔의 정보</param>
        private void BeginScan(in SScanHit hit)
        {
            bool wasIdle = _state == EScanState.Idle;

            // 마지막 스캔 번호와 종료 시각을 갱신한다. 이미 진행 중이었다면 이 값만 바뀌어 시간이 연장된다.
            _lastScanId = hit.ScanId;
            _activeUntil = Time.time + hit.Duration;
            _state = EScanState.Active;

            // 이제부터 Update 에서 페이드와 타이머를 처리한다.
            enabled = true;

            // 진행 중이던 반응은 다시 시작하지 않는다(화면이 튀지 않게).
            if (!wasIdle) return;

            // 처음 닿은 경우만 이 스캔을 연출 기준으로 삼고, 세기를 0 에서 시작하며 각 반응에 시작을 알린다.
            _revealScanId = hit.ScanId;
            _fade = 0.0f;
            foreach (ScanReactionBase reaction in _reactions)
            {
                reaction.OnScanBegin(hit);
            }
        }

        /// <summary>
        /// 반응을 끝내고 원래 상태로 돌린다.
        /// </summary>
        private void EndScan()
        {
            _state = EScanState.Idle;
            _fade = 0.0f;

            // 각 반응이 원래 상태로 되돌아가게 한다.
            foreach (ScanReactionBase reaction in _reactions)
            {
                reaction.OnScanEnd();
            }

            // 다음 스캔에 닿을 때까지 Update 비용을 쓰지 않는다.
            enabled = false;
        }

        /// <summary>
        /// 현재 세기와 사라지는 중인지 여부를 모든 반응에 전달한다.
        /// </summary>
        private void UpdateFade()
        {
            // 올라가는 중과 내려가는 중을 반응이 구분할 수 있게 같이 넘긴다.
            bool isFadingOut = _state == EScanState.FadingOut;
            foreach (ScanReactionBase reaction in _reactions)
            {
                reaction.OnFade(_fade, isFadingOut);
            }
        }

        /// <summary>
        /// 목표 세기를 향해 이번 프레임만큼 fade 를 움직인다. 시간이 0이면 즉시 목표에 도달한다.
        /// </summary>
        /// <param name="target">목표 세기(0 또는 1)</param>
        /// <param name="time">0에서 1(또는 반대)까지 걸리는 시간(초)</param>
        /// <returns>갱신된 fade</returns>
        private float StepFade(float target, float time)
        {
            // 시간이 0 이하면 0 나눗셈을 피하고 즉시 목표로 보낸다.
            if (time <= 0.0f) return target;

            // 프레임 시간 / 총 시간 만큼 이동하므로 time 초 뒤에 정확히 목표에 닿는다.
            return Mathf.MoveTowards(_fade, target, Time.deltaTime / time);
        }
        #endregion
    }
}
