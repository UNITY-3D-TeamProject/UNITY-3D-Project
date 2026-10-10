using UnityEngine;

namespace Core.Scanning
{
    /// <summary>
    /// IScannable 의 기본 구현. 스캔에 닿은 시점부터 스캔 지속 시간만큼 반응을 유지하고, 이후 서서히 사라지게 한다.
    /// 어떻게 보일지는 같은 오브젝트(또는 자식)의 ScanReactionBase 들이 결정한다.
    /// 스킬과 파동의 생명주기와 무관하게 대상이 스스로 타이머를 소유하므로, 파동이 사라져도 끝까지 진행한다.
    /// </summary>
    public class ScanTarget : MonoBehaviour, IScannable
    {
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
        [Tooltip("이 대상의 반응 목록. 비어 있으면 Awake 에서 자식의 ScanReactionBase 를 모두 찾는다.")]
        // 스캔에 닿았을 때 눈에 보이는 반응 하나를 만드는 부모 클래스 
        // 여러 반응이 필요한경우를 대비해서 배열로 만든다.
        [SerializeField] private ScanReactionBase[] _reactions;
        #endregion

        #region Private Fields
        // 현재 진행 단계
        private EScanState _state = EScanState.Idle;
        // 마지막으로 받은 스캔 번호. 새 스캔인지 같은 스캔의 반복인지 구분하는 데 쓴다.
        private int _lastScanId;
        // 활성 상태가 끝나는 시각(Time.time 기준)
        private float _activeUntil;
        // 현재 반응 세기(0~1). 모든 반응에 전달된다.
        private float _fade;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if ((_reactions == null) || (_reactions.Length == 0))
            {
                _reactions = GetComponentsInChildren<ScanReactionBase>(true);
            }

            // 닿기 전에는 Update 비용을 쓰지 않는다.
            // 이 컴포넌트를 끈다.
            enabled = false;
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
            NotifyFade();

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
        /// <inheritdoc />
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

            // 닿아 있는 동안 파동 반경 같은 최신 값을 각 반응에 넘긴다.
            foreach (ScanReactionBase reaction in _reactions)
            {
                reaction.OnScanUpdate(hit);
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 새 스캔에 닿았을 때 타이머를 (재)시작한다. 이미 진행 중이면 반응을 새로 시작하지 않고 시간만 연장한다.
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

            // 처음 닿은 경우만 세기를 0 에서 시작하고 각 반응에 시작을 알린다.
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
        private void NotifyFade()
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
