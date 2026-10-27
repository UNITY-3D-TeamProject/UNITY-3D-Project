using System;
using UnityEngine;

namespace AI.Boss.BossA
{
    /// <summary>
    /// 스킨드 메시의 월드 좌표를 몸통 본의 바인드 포즈 좌표로 바꿔 전용 재질에 전달한다.
    /// 원본 메시나 머터리얼을 수정하지 않고 가슴 코어가 몸체를 따라가게 한다.
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public class BossAMaterialSpace : MonoBehaviour
    {
        #region Static Fields
        private static readonly int WorldToRestId = Shader.PropertyToID("_BossAWorldToRest");
        private static readonly int NormalToRestId = Shader.PropertyToID("_BossANormalToRest");
        #endregion

        #region Serialized Fields
        [Tooltip("몸통과 머리를 움직이는 본. 원본 보스A 모델에서는 RigHead")]
        [SerializeField] private Transform _anchor;
        #endregion

        #region Private Fields
        private SkinnedMeshRenderer _renderer;
        private MaterialPropertyBlock _properties;
        private Matrix4x4 _restToAnchor;
        private bool _isReady;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            Initialize();
            UpdateMaterialSpace();
        }

        private void OnValidate()
        {
            _isReady = false;
        }

        private void LateUpdate()
        {
            if (!_isReady) Initialize();
            UpdateMaterialSpace();
        }
        #endregion

        #region Private Methods
        private void Initialize()
        {
            _isReady = false;
            if (!TryGetComponent(out _renderer) || !_renderer.sharedMesh || !_anchor) return;

            int index = Array.IndexOf(_renderer.bones, _anchor);
            Matrix4x4[] bindPoses = _renderer.sharedMesh.bindposes;
            if ((index < 0) || (index >= bindPoses.Length)) return;

            _restToAnchor = bindPoses[index];
            _properties ??= new MaterialPropertyBlock();
            _isReady = true;
        }

        private void UpdateMaterialSpace()
        {
            if (!_isReady || !_renderer || !_anchor) return;

            Matrix4x4 restToWorld = _anchor.localToWorldMatrix * _restToAnchor;
            _renderer.GetPropertyBlock(_properties);
            _properties.SetMatrix(WorldToRestId, restToWorld.inverse);
            _properties.SetMatrix(NormalToRestId, restToWorld.transpose);
            _renderer.SetPropertyBlock(_properties);
        }
        #endregion
    }
}
