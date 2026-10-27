using System;
using UnityEngine;

namespace AI.Boss.BossB
{
    /// <summary>
    /// 기존 보스B 본을 기준으로 머리와 손목의 셰이더 좌표를 유지한다.
    /// 메시와 애니메이션을 수정하지 않고 발광 영역이 각 부위를 따라가게 한다.
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public class BossBMaterialSpace : MonoBehaviour
    {
        #region Static Fields
        private static readonly int BodyWorldToRestId = Shader.PropertyToID("_BossBBodyWorldToRest");
        private static readonly int LeftPalmWorldToRestId = Shader.PropertyToID("_BossBLeftPalmWorldToRest");
        private static readonly int RightPalmWorldToRestId = Shader.PropertyToID("_BossBRightPalmWorldToRest");
        #endregion

        #region Serialized Fields
        [Tooltip("머리 외장을 움직이는 RigRibcage 본")]
        [SerializeField] private Transform _body;
        [Tooltip("왼손 손목을 움직이는 RigLArmPalm 본")]
        [SerializeField] private Transform _leftPalm;
        [Tooltip("오른손 손목을 움직이는 RigRArmPalm 본")]
        [SerializeField] private Transform _rightPalm;
        #endregion

        #region Private Fields
        private SkinnedMeshRenderer _renderer;
        private MaterialPropertyBlock _properties;
        private Matrix4x4 _restToBody;
        private Matrix4x4 _restToLeftPalm;
        private Matrix4x4 _restToRightPalm;
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
            if (!TryGetComponent(out _renderer) || !_renderer.sharedMesh ||
                !_body || !_leftPalm || !_rightPalm) return;

            Transform[] bones = _renderer.bones;
            Matrix4x4[] bindPoses = _renderer.sharedMesh.bindposes;
            int bodyIndex = Array.IndexOf(bones, _body);
            int leftIndex = Array.IndexOf(bones, _leftPalm);
            int rightIndex = Array.IndexOf(bones, _rightPalm);
            if ((bodyIndex < 0) || (bodyIndex >= bindPoses.Length) ||
                (leftIndex < 0) || (leftIndex >= bindPoses.Length) ||
                (rightIndex < 0) || (rightIndex >= bindPoses.Length)) return;

            _restToBody = bindPoses[bodyIndex];
            _restToLeftPalm = bindPoses[leftIndex];
            _restToRightPalm = bindPoses[rightIndex];
            _properties ??= new MaterialPropertyBlock();
            _isReady = true;
        }

        private void UpdateMaterialSpace()
        {
            if (!_isReady || !_renderer || !_body || !_leftPalm || !_rightPalm) return;

            _renderer.GetPropertyBlock(_properties);
            _properties.SetMatrix(BodyWorldToRestId, (_body.localToWorldMatrix * _restToBody).inverse);
            _properties.SetMatrix(LeftPalmWorldToRestId, (_leftPalm.localToWorldMatrix * _restToLeftPalm).inverse);
            _properties.SetMatrix(RightPalmWorldToRestId, (_rightPalm.localToWorldMatrix * _restToRightPalm).inverse);
            _renderer.SetPropertyBlock(_properties);
        }
        #endregion
    }
}
