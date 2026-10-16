using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Visual
{
    /// <summary>
    /// 메인 카메라가 기준점에 가까워지면 렌더러를 그림자만 남기고 숨기는 컴포넌트.
    /// 렌더러의 enabled 대신 shadowCastingMode 를 바꾸므로 RendererBlinker 와 충돌하지 않는다.
    /// 판정은 Cinemachine 이 카메라를 옮긴 뒤인 렌더 직전에 한다.
    /// </summary>
    public class CameraNearHider : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("숨길 렌더러. 비어 있으면 자신과 하위에서 켜져 있는 모든 렌더러를 사용한다.")]
        [SerializeField] private Renderer[] _renderers;
        [Tooltip("카메라와의 거리를 잴 기준점")]
        [SerializeField] private Transform _pivot;
        [Header("Settings")]
        [Tooltip("카메라와 기준점 사이 거리가 이 값 이하이면 숨긴다")]
        [SerializeField, Min(0.0f)] private float _hideDistance = 1.0f;
        #endregion

        #region Private Fields
        private ShadowCastingMode[] _originalModes;
        private bool _isHidden;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_renderers == null || _renderers.Length == 0)
                _renderers = Array.FindAll(GetComponentsInChildren<Renderer>(), target => target.enabled);

            // 숨김을 풀 때 되돌릴 원래 그림자 설정을 기억한다
            _originalModes = new ShadowCastingMode[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i]) _originalModes[i] = _renderers[i].shadowCastingMode;
            }
        }

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;
            SetHidden(false);
        }
        #endregion

        #region Private Methods
        private void HandleBeginCameraRendering(ScriptableRenderContext context, Camera targetCamera)
        {
            if (!_pivot || targetCamera != Camera.main) return;

            float distance = Vector3.Distance(targetCamera.transform.position, _pivot.position);
            SetHidden(distance <= _hideDistance);
        }

        private void SetHidden(bool isHidden)
        {
            if (_isHidden == isHidden) return;

            _isHidden = isHidden;
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i]) _renderers[i].shadowCastingMode = isHidden ? ShadowCastingMode.ShadowsOnly : _originalModes[i];
            }
        }
        #endregion
    }
}
