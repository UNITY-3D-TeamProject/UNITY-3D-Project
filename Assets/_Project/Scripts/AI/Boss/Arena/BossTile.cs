using UnityEngine;
using UnityEngine.AI;

namespace AI.Boss.Arena
{
    /// <summary>
    /// 아레나 칸 하나의 겉모습. 붕괴하면 바닥·기둥을 끄고 NavMesh 구멍을 낸다. 상태 판단은 하지 않고 BossArena 가 시킨 대로만 한다.
    /// </summary>
    public class BossTile : MonoBehaviour
    {
        #region Serialized Fields
        [Tooltip("붕괴 때 끌 렌더러. 바닥과 바로 아래 기둥을 함께 넣는다.")]
        [SerializeField] private Renderer[] _renderers;
        [Tooltip("붕괴 때 끌 콜라이더. 바닥과 바로 아래 기둥을 함께 넣는다.")]
        [SerializeField] private Collider[] _colliders;
        [Tooltip("붕괴 중에만 켜는 NavMesh 구멍(carve). 기본 꺼짐.")]
        [SerializeField] private NavMeshObstacle _holeObstacle;
        [Tooltip("칸 예고용 반투명 쿼드. 기본 꺼짐.")]
        [SerializeField] private GameObject _telegraph;
        #endregion

        #region Private Fields
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Renderer _telegraphRenderer;
        private MaterialPropertyBlock _telegraphBlock;
        private Vector3 _telegraphScale;
        private Color _telegraphColor;
        private Color _activeColor;
        private float _fillSeconds;
        private float _fillElapsed;
        private bool _isFilling;
        private bool _isTelegraphCached;
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            if (!_isFilling) return;

            _fillElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(_fillElapsed / _fillSeconds);
            ApplyFill(progress);

            if (progress >= 1.0f) _isFilling = false;
        }
        #endregion

        #region Public Methods
        public void SetCollapsed(bool isCollapsed)
        {
            foreach (Renderer tileRenderer in _renderers)
            {
                if (tileRenderer) tileRenderer.enabled = !isCollapsed;
            }

            foreach (Collider tileCollider in _colliders)
            {
                if (tileCollider) tileCollider.enabled = !isCollapsed;
            }

            if (_holeObstacle) _holeObstacle.enabled = isCollapsed;
        }

        /// <param name="fillSeconds">켤 때 중앙에서 가득 차기까지의 시간(초). 0 이하면 처음부터 가득 찬다</param>
        /// <param name="color">예고 색(알파는 머티리얼 값 유지). null 이면 머티리얼 색</param>
        public void SetTelegraph(bool isOn, float fillSeconds = 0.0f, Color? color = null)
        {
            if (!_telegraph) return;

            CacheTelegraph();

            _activeColor = _telegraphColor;
            if (color.HasValue)
            {
                _activeColor = color.Value;
                _activeColor.a = _telegraphColor.a;
            }

            _isFilling = isOn && (fillSeconds > 0.0f);
            _fillSeconds = fillSeconds;
            _fillElapsed = 0.0f;
            ApplyFill(_isFilling ? 0.0f : 1.0f);

            _telegraph.SetActive(isOn);
        }
        #endregion

        #region Private Methods
        /// <summary>BossArena.Awake 가 이 칸의 Awake 보다 먼저 SetTelegraph 를 부를 수 있어, Awake 가 아니라 첫 사용 때 한 번 캐시한다.</summary>
        private void CacheTelegraph()
        {
            if (_isTelegraphCached) return;

            _isTelegraphCached = true;
            _telegraphScale = _telegraph.transform.localScale;
            _telegraphRenderer = _telegraph.GetComponent<Renderer>();
            if (_telegraphRenderer && _telegraphRenderer.sharedMaterial.HasProperty(BaseColorId))
            {
                _telegraphBlock = new MaterialPropertyBlock();
                _telegraphColor = _telegraphRenderer.sharedMaterial.GetColor(BaseColorId);
            }
        }

        /// <summary>예고 쿼드를 중앙 기준으로 progress 만큼 키우고 알파를 같은 비율로 올린다.</summary>
        private void ApplyFill(float progress)
        {
            _telegraph.transform.localScale = new Vector3(
                _telegraphScale.x * progress,
                _telegraphScale.y * progress,
                _telegraphScale.z);

            if (_telegraphBlock == null) return;

            Color color = _activeColor;
            color.a *= progress;
            _telegraphBlock.SetColor(BaseColorId, color);
            _telegraphRenderer.SetPropertyBlock(_telegraphBlock);
        }
        #endregion
    }
}
