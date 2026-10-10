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
        private enum EScanState
        {
            Idle,
            Active,
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
        [SerializeField] private ScanReactionBase[] _reactions;
        #endregion

        #region Private Fields
        private EScanState _state = EScanState.Idle;
        private int _lastScanId;
        private float _activeUntil;
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
            enabled = false;
        }

        private void Update()
        {
            if (_state == EScanState.Active)
            {
                _fade = StepFade(1.0f, _fadeInTime);
                if (Time.time >= _activeUntil)
                {
                    _state = EScanState.FadingOut;
                }
            }
            else if (_state == EScanState.FadingOut)
            {
                _fade = StepFade(0.0f, _fadeOutTime);
            }

            NotifyFade();

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

            _lastScanId = hit.ScanId;
            _activeUntil = Time.time + hit.Duration;
            _state = EScanState.Active;
            enabled = true;

            if (!wasIdle) return;

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

            foreach (ScanReactionBase reaction in _reactions)
            {
                reaction.OnScanEnd();
            }

            enabled = false;
        }

        private void NotifyFade()
        {
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
            if (time <= 0.0f) return target;

            return Mathf.MoveTowards(_fade, target, Time.deltaTime / time);
        }
        #endregion
    }
}
