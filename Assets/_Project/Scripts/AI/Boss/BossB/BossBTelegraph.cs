using UnityEngine;

namespace AI.Boss.BossB
{
    /// <summary>
    /// B 스킬이 쓰는 바닥 예고 판 하나. 프리팹을 만들어 두고 위치·크기를 맞추며, 알파를 0 에서 머터리얼 알파까지 올려 나타나게 한다.
    /// 판 크기는 프리팹 루트의 로컬 X(폭)·Z(길이) 스케일로 맞춘다. 알파는 MaterialPropertyBlock 의 _BaseColor 로 바꿔 공유 머터리얼을 건드리지 않는다.
    /// </summary>
    public class BossBTelegraph
    {
        #region Private Fields
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private GameObject _instance;
        private Renderer _renderer;
        private MaterialPropertyBlock _block;
        private Color _baseColor;
        private Vector3 _baseScale;
        #endregion

        #region Public Methods
        /// <summary>
        /// 예고 판을 투명한 상태로 만든다. 이미 있으면 지우고 새로 만든다. 프리팹이 없으면 아무것도 하지 않는다.
        /// </summary>
        public void Show(GameObject prefab)
        {
            Hide();
            if (!prefab) return;

            _instance = Object.Instantiate(prefab);
            _baseScale = _instance.transform.localScale;
            _renderer = _instance.GetComponentInChildren<Renderer>();
            _block = null;
            if (_renderer && _renderer.sharedMaterial && _renderer.sharedMaterial.HasProperty(BaseColorId))
            {
                _block = new MaterialPropertyBlock();
                _baseColor = _renderer.sharedMaterial.GetColor(BaseColorId);
            }

            SetFade(0.0f);
        }

        /// <summary>
        /// 예고 판을 놓는다. 높이(Y) 스케일은 프리팹 그대로 둔다.
        /// </summary>
        /// <param name="width">로컬 X 크기(m)</param>
        /// <param name="length">로컬 Z 크기(m)</param>
        public void Place(Vector3 position, Quaternion rotation, float width, float length)
        {
            if (!_instance) return;

            _instance.transform.SetPositionAndRotation(position, rotation);
            _instance.transform.localScale = new Vector3(width, _baseScale.y, length);
        }

        /// <summary>
        /// 알파를 머터리얼 알파 × progress 로 맞춘다.
        /// </summary>
        /// <param name="progress">0 이면 투명, 1 이면 머터리얼 알파 그대로</param>
        public void SetFade(float progress)
        {
            if (!_instance || (_block == null)) return;

            Color color = _baseColor;
            color.a *= Mathf.Clamp01(progress);
            _block.SetColor(BaseColorId, color);
            _renderer.SetPropertyBlock(_block);
        }

        /// <summary>
        /// 예고 판을 지운다. 없으면 아무것도 하지 않는다.
        /// </summary>
        public void Hide()
        {
            if (!_instance) return;

            Object.Destroy(_instance);
            _instance = null;
            _renderer = null;
        }
        #endregion
    }
}
