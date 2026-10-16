using UnityEngine;

namespace AI.Crowd
{
    /// <summary>
    /// 플레이어에게 앞이 막힌 군중 개체의 표정을 짜증으로 바꾼다.
    /// CrowdController.IsBlockedByPlayer 를 읽어 Animator 의 IsAnnoyed 를 켜고, 막힘이 풀린 뒤에도 잠시 유지한다.
    /// 같은 프리팹 안의 외형 연출이라 CrowdAppearance 처럼 중재자를 거치지 않는다.
    /// </summary>
    public class CrowdExpression : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("막힘 여부를 읽을 조종부. 비어 있으면 같은 오브젝트에서 찾는다.")]
        [SerializeField] private CrowdController _crowdController;
        [Tooltip("표정 레이어를 가진 모델의 Animator. 비어 있으면 자식에서 찾는다.")]
        [SerializeField] private Animator _animator;

        [Header("Settings")]
        [Tooltip("플레이어에게 막힌 상태가 풀린 뒤에도 짜증 표정을 유지하는 시간(초). 막힘이 잠깐 끊겨도 표정이 깜빡이지 않게 한다.")]
        [SerializeField, Min(0.0f)] private float _holdDuration = 1.5f;
        #endregion

        #region Private Fields
        private float _lastBlockedTime;
        private bool _isAnnoyed;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (!_crowdController) _crowdController = GetComponent<CrowdController>();
            if (!_animator) _animator = GetComponentInChildren<Animator>();
        }

        private void OnEnable()
        {
            // 풀에서 다시 꺼낼 때 이전 짜증 표정이 남지 않게 한다
            _lastBlockedTime = float.NegativeInfinity;
            _isAnnoyed = false;
            if (_animator) _animator.SetBool(AnimHash.IsAnnoyed, false);
        }

        private void Update()
        {
            if (!_crowdController || !_animator) return;

            if (_crowdController.IsBlockedByPlayer) _lastBlockedTime = Time.time;

            SetAnnoyed((Time.time - _lastBlockedTime) < _holdDuration);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 값이 바뀔 때만 Animator 파라미터를 설정한다.
        /// </summary>
        /// <param name="isAnnoyed">짜증 표정이면 true</param>
        private void SetAnnoyed(bool isAnnoyed)
        {
            if (_isAnnoyed == isAnnoyed) return;

            _isAnnoyed = isAnnoyed;
            _animator.SetBool(AnimHash.IsAnnoyed, isAnnoyed);
        }
        #endregion

        #region Nested Types
        private static class AnimHash
        {
            public static readonly int IsAnnoyed = Animator.StringToHash("IsAnnoyed");
        }
        #endregion
    }
}
