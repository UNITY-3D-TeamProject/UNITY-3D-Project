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
        [Tooltip("복구될 때 솟아오르기 시작하는 깊이(m). 0 이면 연출 없이 바로 나타난다.")]
        [SerializeField, Min(0.0f)] private float _riseDepth = 4.0f;
        [Tooltip("복구될 때 제자리까지 솟아오르는 시간(초). 0 이면 연출 없이 바로 나타난다.")]
        [SerializeField, Min(0.0f)] private float _riseSeconds = 0.35f;
        [Tooltip("솟아오를 때 제자리를 넘었다가 내려앉는 정도(EaseOutBack 계수). 0 이면 넘치지 않는다.")]
        [SerializeField, Min(0.0f)] private float _riseOvershoot = 2.0f;
        #endregion

        #region Private Fields
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Renderer _telegraphRenderer;
        private MaterialPropertyBlock _telegraphBlock;
        private Vector3 _telegraphScale;
        private Vector3 _telegraphPosition;
        private Vector3 _telegraphCenter;
        private Vector3 _telegraphFillAxes = new Vector3(1.0f, 1.0f, 0.0f);
        private Color _telegraphColor;
        private Color _activeColor;
        private float _fillSeconds;
        private float _fillElapsed;
        private bool _isFilling;
        private bool _isTelegraphCached;
        private Vector3 _restPosition;
        private float _riseElapsed;
        private bool _isRising;
        private bool _isCollapsed;
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            if (_isRising) UpdateRise();

            if (!_isFilling) return;

            _fillElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(_fillElapsed / _fillSeconds);
            ApplyFill(progress);

            if (progress >= 1.0f) _isFilling = false;
        }
        #endregion

        #region Public Methods
        /// <summary>무너졌던 칸이 복구될 때만 아래에서 솟아오른다. 처음 시작 때(무너진 적 없음)는 연출하지 않는다.</summary>
        public void SetCollapsed(bool isCollapsed)
        {
            if (isCollapsed) StopRise();
            else if (_isCollapsed && (_riseDepth > 0.0f) && (_riseSeconds > 0.0f)) StartRise();
            _isCollapsed = isCollapsed;

            foreach (Renderer tileRenderer in _renderers)
            {
                if (tileRenderer) tileRenderer.enabled = !isCollapsed;
            }

            foreach (Collider tileCollider in _colliders)
            {
                if (tileCollider) tileCollider.enabled = !isCollapsed;
            }

            // 솟는 동안은 구멍을 유지해 아직 올라오지 않은 칸 위로 NavMesh 가 이어지지 않게 한다.
            if (_holeObstacle) _holeObstacle.enabled = isCollapsed || _isRising;
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
        /// <summary>솟고 있지 않을 때의 위치가 제자리이므로 그 자리를 기억하고 깊이만큼 내려서 시작한다.</summary>
        private void StartRise()
        {
            _restPosition = transform.position;
            _riseElapsed = 0.0f;
            _isRising = true;
            transform.position = _restPosition + (Vector3.down * _riseDepth);
        }

        private void UpdateRise()
        {
            _riseElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(_riseElapsed / _riseSeconds);
            transform.position = _restPosition + (Vector3.down * (_riseDepth * (1.0f - EaseOutBack(progress))));

            if (progress >= 1.0f) StopRise();
        }

        /// <summary>솟는 중이면 제자리로 되돌리고, 무너진 상태가 아니면 NavMesh 구멍을 닫는다.</summary>
        private void StopRise()
        {
            if (!_isRising) return;

            _isRising = false;
            transform.position = _restPosition;
            if (_holeObstacle) _holeObstacle.enabled = _isCollapsed;
        }

        /// <summary>끝 무렵 1 을 살짝 넘었다가 1 로 돌아오는 곡선. 넘침 정도는 <c>_riseOvershoot</c>.</summary>
        private float EaseOutBack(float t)
        {
            float u = t - 1.0f;
            return 1.0f + ((_riseOvershoot + 1.0f) * u * u * u) + (_riseOvershoot * u * u);
        }

        /// <summary>BossArena.Awake 가 이 칸의 Awake 보다 먼저 SetTelegraph 를 부를 수 있어, Awake 가 아니라 첫 사용 때 한 번 캐시한다.</summary>
        private void CacheTelegraph()
        {
            if (_isTelegraphCached) return;

            _isTelegraphCached = true;
            _telegraphScale = _telegraph.transform.localScale;
            _telegraphPosition = _telegraph.transform.localPosition;
            CacheFillPivot();
            _telegraphRenderer = _telegraph.GetComponent<Renderer>();
            if (_telegraphRenderer && _telegraphRenderer.sharedMaterial.HasProperty(BaseColorId))
            {
                _telegraphBlock = new MaterialPropertyBlock();
                _telegraphColor = _telegraphRenderer.sharedMaterial.GetColor(BaseColorId);
            }
        }

        /// <summary>
        /// 메시 기준점이 가운데가 아니어도(예: 모서리 기준인 맵 예고 메시) 가운데서 채워지도록,
        /// 메시 bounds 중심을 채움 기준점으로, 가장 얇은 축을 뺀 두 축을 채움 축으로 정한다.
        /// </summary>
        private void CacheFillPivot()
        {
            MeshFilter meshFilter = _telegraph.GetComponent<MeshFilter>();
            if (!meshFilter || !meshFilter.sharedMesh) return;

            Bounds bounds = meshFilter.sharedMesh.bounds;
            _telegraphCenter = bounds.center;

            Vector3 size = bounds.size;
            if (size.x <= size.y && size.x <= size.z) _telegraphFillAxes = new Vector3(0.0f, 1.0f, 1.0f);
            else if (size.y <= size.z) _telegraphFillAxes = new Vector3(1.0f, 0.0f, 1.0f);
            else _telegraphFillAxes = new Vector3(1.0f, 1.0f, 0.0f);
        }

        /// <summary>예고 메시를 bounds 중심 기준으로 progress 만큼 키우고 알파를 같은 비율로 올린다.</summary>
        private void ApplyFill(float progress)
        {
            Vector3 scale = new Vector3(
                _telegraphScale.x * Mathf.Lerp(1.0f, progress, _telegraphFillAxes.x),
                _telegraphScale.y * Mathf.Lerp(1.0f, progress, _telegraphFillAxes.y),
                _telegraphScale.z * Mathf.Lerp(1.0f, progress, _telegraphFillAxes.z));

            // 스케일이 바뀌어도 메시 중심이 부모 기준 같은 자리에 남도록 위치를 보정한다.
            Vector3 centerShift = Vector3.Scale(_telegraphScale - scale, _telegraphCenter);
            _telegraph.transform.localPosition = _telegraphPosition + (_telegraph.transform.localRotation * centerShift);
            _telegraph.transform.localScale = scale;

            if (_telegraphBlock == null) return;

            Color color = _activeColor;
            color.a *= progress;
            _telegraphBlock.SetColor(BaseColorId, color);
            _telegraphRenderer.SetPropertyBlock(_telegraphBlock);
        }
        #endregion
    }
}
