using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Platform
{
    /// <summary>
    /// 발판(데이터 쪼가리)이 삭제 지점에 도달할 때마다 소각 이펙트를 내고, 소각로 위 "Deleting... N%" 진행 바를 채운다.
    /// 시각 연출 전용이며 충돌/판정 기능은 없다.
    /// </summary>
    public class DataIncinerator : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("소각 이벤트를 발생시키는 스포너 (씬 오브젝트라 프리팹에서는 직접 연결)")]
        [SerializeField] private PlatformSpawner _spawner;
        [SerializeField] private Image _fillImage;
        [SerializeField] private Text _percentText;
        [SerializeField] private ParticleSystem _bitsEffect;
        [SerializeField] private ParticleSystem _flashEffect;

        [Header("Progress")]
        [Tooltip("발판 1개가 소각될 때 늘어나는 진행도 (1.0 = 100%)")]
        [SerializeField, Range(0.01f, 1.0f)] private float _progressPerPlatform = 0.05f;

        [Tooltip("진행 바가 차오르는 속도 (초당 진행도)")]
        [SerializeField, Min(0.0f)] private float _fillSpeed = 1.5f;

        [Tooltip("100%에 도달한 뒤 초기화되기까지 대기하는 시간(초)")]
        [SerializeField, Min(0.0f)] private float _completeHoldSeconds = 0.6f;

        [Header("Effects")]
        [Tooltip("발판 1개가 소각될 때 튀는 데이터 조각 파티클 수")]
        [SerializeField, Min(0)] private int _bitsPerPlatform = 12;
        #endregion

        #region Private Fields
        private float _targetProgress;
        private float _displayedProgress;
        private int   _shownPercent = -1;
        private bool  _isCompleting;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_fillImage != null, $"[{name}] 진행 바 Fill Image가 연결되지 않았습니다.");
            Debug.Assert(_percentText != null, $"[{name}] 퍼센트 Text가 연결되지 않았습니다.");
        }

        private void OnEnable()
        {
            if (_spawner != null)
            {
                _spawner.OnPlatformReleased += HandlePlatformReleased;
            }
        }

        private void OnDisable()
        {
            if (_spawner != null)
            {
                _spawner.OnPlatformReleased -= HandlePlatformReleased;
            }
        }

        private void Update()
        {
            _displayedProgress = Mathf.MoveTowards(_displayedProgress, _targetProgress, _fillSpeed * Time.deltaTime);

            _fillImage.fillAmount = _displayedProgress;
            RefreshPercentText();
        }
        #endregion

        #region Private Methods
        private void HandlePlatformReleased(Vector3 worldPosition)
        {
            EmitBurst(worldPosition);

            if (_isCompleting) return;

            _targetProgress = Mathf.Min(1.0f, _targetProgress + _progressPerPlatform);

            if (_targetProgress >= 1.0f)
            {
                StartCoroutine(CoComplete());
            }
        }

        private void EmitBurst(Vector3 worldPosition)
        {
            if (_bitsEffect != null)
            {
                _bitsEffect.transform.position = worldPosition;
                _bitsEffect.Emit(_bitsPerPlatform);
            }

            if (_flashEffect != null)
            {
                _flashEffect.transform.position = worldPosition;
                _flashEffect.Emit(1);
            }
        }

        private void RefreshPercentText()
        {
            int percent = Mathf.RoundToInt(_displayedProgress * 100.0f);
            if (percent == _shownPercent) return;

            _shownPercent = percent;
            _percentText.text = (percent >= 100) ? "Deleted!" : $"Deleting... {percent}%";
        }
        #endregion

        #region Coroutines
        private IEnumerator CoComplete()
        {
            _isCompleting = true;

            yield return new WaitUntil(() => _displayedProgress >= 1.0f);
            yield return new WaitForSeconds(_completeHoldSeconds);

            // 바가 서서히 비워지며 다음 삭제 주기가 시작된다.
            _targetProgress = 0.0f;
            _isCompleting = false;
        }
        #endregion
    }
}
