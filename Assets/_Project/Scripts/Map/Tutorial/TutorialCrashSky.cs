using UnityEngine;

namespace Map.Tutorial
{
    /// <summary>
    /// 튜토리얼 크래시 스카이박스(Project/TutorialCrashSkybox)의 진행도를 올린다.
    /// 구간 트리거(TriggerPlate 등)의 UnityEvent 에서 RaiseTo 를 부르면 목표값이 정해지고,
    /// 현재값이 그 목표까지 아주 천천히 따라 올라간다. 값은 내려가지 않는다.
    /// 노래 파일을 얻는 순간에는 TriggerWarning 을 불러 87% 멈춤 → WARNING 을 바로 띄운다.
    /// 진행도 기준(셰이더 기본값): 0.5 = 87%에서 멈춤, 0.53 = 경고 시작, 1 = 완전히 붉어짐.
    /// </summary>
    public class TutorialCrashSky : MonoBehaviour
    {
        #region Constants
        private const string PROGRESS_PROPERTY = "_TutorialCrashProgress";
        #endregion

        #region Serialized Fields
        [Header("Progress")]
        [Tooltip("씬 시작 때 진행도 (0~1)")]
        [SerializeField, Range(0.0f, 1.0f)] private float _startProgress;

        [Tooltip("목표값을 따라가는 속도 (초당 진행도). 작을수록 느리게 변한다.")]
        [SerializeField] private float _riseSpeed = 0.01f;

        [Tooltip("TriggerWarning 때 바로 맞출 진행도. 셰이더 Warning At(0.53)보다 커야 경고가 뜬다.")]
        [SerializeField, Range(0.0f, 1.0f)] private float _warningProgress = 0.54f;
        #endregion

        #region Private Fields
        private static readonly int ProgressId = Shader.PropertyToID(PROGRESS_PROPERTY);
        private float _current;
        private float _target;
        #endregion

        #region Properties
        /// <summary>현재 진행도 (0~1).</summary>
        public float Progress => _current;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _current = _startProgress;
            _target = _startProgress;
            Shader.SetGlobalFloat(ProgressId, _current);
        }

        private void Update()
        {
            if (_current >= _target)
            {
                return;
            }

            _current = Mathf.MoveTowards(_current, _target, _riseSpeed * Time.deltaTime);
            Shader.SetGlobalFloat(ProgressId, _current);
        }

        private void OnDisable()
        {
            // 다른 씬·에디터 미리보기가 붉은 상태로 남지 않게 되돌린다.
            Shader.SetGlobalFloat(ProgressId, 0.0f);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 목표 진행도를 올린다. 지금 목표보다 낮은 값은 무시한다.
        /// </summary>
        /// <param name="target">목표 진행도 (0~1)</param>
        public void RaiseTo(float target)
        {
            _target = Mathf.Max(_target, Mathf.Clamp01(target));
        }

        /// <summary>
        /// 노래 파일 획득 순간: 진행도를 경고 지점까지 바로 올려 87% 멈춤 → WARNING 을 띄운다.
        /// </summary>
        public void TriggerWarning()
        {
            RaiseTo(_warningProgress);
            _current = Mathf.Max(_current, _warningProgress);
            Shader.SetGlobalFloat(ProgressId, _current);
        }
        #endregion
    }
}
