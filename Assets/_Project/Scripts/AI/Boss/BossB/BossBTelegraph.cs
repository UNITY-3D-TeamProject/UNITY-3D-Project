using UnityEngine;
using DTT.AreaOfEffectRegions;

namespace AI.Boss.BossB
{
    /// <summary>
    /// B 스킬이 쓰는 바닥 예고 판 하나. 프리팹을 만들어 두고 위치·크기를 맞추며, 알파를 0 에서 머터리얼 알파까지 올려 나타나게 한다.
    /// 판 크기는 프리팹 루트의 로컬 X(폭)·Z(길이) 스케일로 맞춘다. 알파는 MaterialPropertyBlock 의 _BaseColor 로 바꿔 공유 머터리얼을 건드리지 않는다.
    /// 프리팹에 DTT LineRegion·CircleRegion 이 있으면 크기 조절 대신 Length·Width·Radius 를 넣고, 알파 대신 FillProgress 로 채운다.
    /// DTT 는 공유 머터리얼에 채움 값을 쓰므로 그런 프리팹에는 머터리얼을 복제하는 AoeVfxPlayer 를 함께 붙여 둔다.
    /// </summary>
    public class BossBTelegraph
    {
        #region Private Fields
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private GameObject _instance;
        private LineRegion _line;
        private CircleRegion _circle;
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
            _line = _instance.GetComponent<LineRegion>();
            _circle = _instance.GetComponent<CircleRegion>();
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
        /// <param name="position">판의 중심</param>
        /// <param name="width">로컬 X 크기(m). 원이면 지름</param>
        /// <param name="length">로컬 Z 크기(m)</param>
        public void Place(Vector3 position, Quaternion rotation, float width, float length)
        {
            if (!_instance) return;

            if (_line)
            {
                // DTT 직선은 기준점이 시작점이라 중심에서 반 길이만큼 뒤로 물린다
                Vector3 start = position - (rotation * Vector3.forward * (length * 0.5f));
                _instance.transform.SetPositionAndRotation(start, rotation);
                _line.Angle = 0.0f;
                _line.Length = length;
                _line.Width = width;
                return;
            }

            _instance.transform.SetPositionAndRotation(position, rotation);
            if (_circle)
            {
                _circle.Radius = width * 0.5f;
                return;
            }

            _instance.transform.localScale = new Vector3(width, _baseScale.y, length);
        }

        /// <summary>
        /// 알파를 머터리얼 알파 × progress 로 맞춘다. DTT 예고면 progress 만큼 채운다.
        /// </summary>
        /// <param name="progress">0 이면 투명, 1 이면 머터리얼 알파 그대로</param>
        public void SetFade(float progress)
        {
            if (_line)
            {
                _line.FillProgress = progress;
                return;
            }

            if (_circle)
            {
                _circle.FillProgress = progress;
                return;
            }

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
            _line = null;
            _circle = null;
            _renderer = null;
        }
        #endregion
    }
}
