using System;
using UnityEngine;

namespace Visual
{
    /// <summary>
    /// 렌더러를 일정 주기로 켜고 끄는 깜빡임 컴포넌트.
    /// 언제 멈출지는 판단하지 않으며, Stop 을 받을 때까지 깜빡인다.
    /// </summary>
    public class RendererBlinker : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("깜빡일 렌더러. 비어 있으면 자신과 하위에서 켜져 있는 모든 렌더러를 사용한다.")]
        [SerializeField] private Renderer[] _renderers;
        [Header("Settings")]
        [Tooltip("켜기/끄기 전환 간격(초)")]
        [SerializeField, Min(0.01f)] private float _interval = 0.1f;
        #endregion

        #region Private Fields
        private bool _isPlaying;
        private bool _isVisible = true;
        private float _elapsedTime;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // 원래 꺼져 있던 렌더러가 Stop 에서 켜지지 않도록 켜져 있는 것만 모은다
            if (_renderers == null || _renderers.Length == 0)
                _renderers = Array.FindAll(GetComponentsInChildren<Renderer>(), target => target.enabled);
        }

        private void Update()
        {
            if (!_isPlaying) return;

            _elapsedTime += Time.deltaTime;
            if (_elapsedTime < _interval) return;

            _elapsedTime -= _interval;
            SetVisible(!_isVisible);
        }

        private void OnDisable()
        {
            Stop();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 깜빡임을 시작한다. 이미 깜빡이는 중이면 그대로 이어간다.
        /// </summary>
        public void Play()
        {
            if (_isPlaying) return;

            _isPlaying = true;
            _elapsedTime = 0.0f;
            SetVisible(false);
        }

        /// <summary>
        /// 깜빡임을 멈추고 렌더러를 켠 상태로 되돌린다.
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;
            SetVisible(true);
        }
        #endregion

        #region Private Methods
        private void SetVisible(bool isVisible)
        {
            _isVisible = isVisible;
            foreach (Renderer target in _renderers)
            {
                if (target) target.enabled = isVisible;
            }
        }
        #endregion
    }
}
