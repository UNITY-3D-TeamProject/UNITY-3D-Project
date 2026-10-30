using UnityEngine;

namespace AI.Boss.BossA
{
    /// <summary>
    /// 보스 A 메시를 바인드 포즈 기준 3×3 칸(좌우 × 높이)으로 나누고, 일정 시간마다 무작위 칸을 골라 셰이더에 넘긴다.
    /// 고른 칸은 BossACorruptedArmor.hlsl 에서 가로 띠가 가끔 옆으로 튀고(버텍스) 보스 B 손처럼 짧은 스파크가 번쩍인다.
    /// 값은 BossAMaterialSpace 처럼 MaterialPropertyBlock 으로 넘겨 머티리얼 에셋을 바꾸지 않는다.
    /// 여러 조각으로 된 모델은 _renderers 에 조각을 모두 넣어 같은 칸을 공유하게 한다.
    /// </summary>
    [DisallowMultipleComponent]
    public class BossAGlitch : MonoBehaviour
    {
        #region Static Fields
        private static readonly int MaskId = Shader.PropertyToID("_BossAGlitchMask");
        private static readonly int BoundsId = Shader.PropertyToID("_BossAGlitchBounds");
        private static readonly int ShiftId = Shader.PropertyToID("_BossAGlitchShift");
        private static readonly int SparkId = Shader.PropertyToID("_BossAGlitchSpark");
        private static readonly int SparkColorId = Shader.PropertyToID("_BossAGlitchSparkColor");
        #endregion

        #region Serialized Fields
        [Header("Cells")]
        [Tooltip("같은 칸을 공유할 렌더러들. 비워 두면 이 오브젝트의 렌더러만 쓴다")]
        [SerializeField] private Renderer[] _renderers;
        [Tooltip("칸으로 나눌 바인드 포즈 범위(X = 좌우 최소, Y = 높이 최소, 너비, 높이). 너비·높이가 0이면 이 오브젝트 스킨드 메시의 bounds 를 쓴다")]
        [SerializeField] private Rect _cellArea;
        [Tooltip("칸을 다시 고르는 간격 범위(초). 매번 이 사이에서 무작위로 정한다")]
        [SerializeField] private Vector2 _intervalRange = new Vector2(2.0f, 3.0f);
        [Tooltip("한 번에 고르는 칸 수 범위(포함). 매번 이 사이에서 무작위로 정한다")]
        [SerializeField] private Vector2Int _cellCountRange = new Vector2Int(2, 3);
        [Tooltip("고를 수 있는 칸 비트 마스크. 칸 번호 = 행(아래→위) × 3 + 열(왼→오른). 기본값은 정점이 없는 아래 가운데(1번)를 뺀 509")]
        [SerializeField] private int _candidateCells = 509;

        [Header("Band Shift")]
        [Tooltip("칸 한 줄(몸 높이의 1/3)을 나누는 가로 띠 수")]
        [SerializeField, Min(1)] private int _bandsPerCell = 8;
        [Tooltip("띠가 옆으로 밀리는 최대 거리. 칸 너비에 대한 비율")]
        [SerializeField, Range(0.0f, 1.0f)] private float _shiftAmount = 0.08f;
        [Tooltip("띠 밀림을 다시 정하는 횟수(초당)")]
        [SerializeField, Min(0.0f)] private float _shiftRate = 8.0f;
        [Tooltip("갱신할 때마다 띠 하나가 밀릴 확률")]
        [SerializeField, Range(0.0f, 1.0f)] private float _shiftChance = 0.15f;

        [Header("Sparks")]
        [Tooltip("칸마다 스파크가 한 번 번쩍이는 주기(초). 주기 안에서 번쩍이는 시점은 무작위")]
        [SerializeField, Min(0.05f)] private float _sparkPeriod = 1.2f;
        [Tooltip("한 번 번쩍일 때 보이는 시간(초). 끝 45% 동안 사라진다")]
        [SerializeField, Min(0.01f)] private float _sparkDuration = 0.32f;
        [Tooltip("스파크 줄기 굵기. 칸 너비에 대한 비율")]
        [SerializeField, Min(0.0f)] private float _sparkWidth = 0.008f;
        [Tooltip("스파크 색. 기본값은 보스 B 손 스파크(M_BossBHandSpark)와 같다")]
        [SerializeField, ColorUsage(false, true)] private Color _sparkColor = new Color(3.2f, 0.32f, 0.06f, 1.0f);
        [Tooltip("스파크 발광 세기. 색에 곱한다")]
        [SerializeField, Min(0.0f)] private float _sparkIntensity = 1.0f;
        #endregion

        #region Private Fields
        private Renderer[] _targets;
        private MaterialPropertyBlock _properties;
        private System.Random _random;
        private Vector4 _bounds;
        private int _mask;
        private float _nextPickTime;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _targets = ((_renderers != null) && (_renderers.Length > 0)) ? _renderers : new[] { GetComponent<Renderer>() };
            _properties = new MaterialPropertyBlock();
            _random = new System.Random();

            if ((_cellArea.width > 0.0f) && (_cellArea.height > 0.0f))
            {
                _bounds = new Vector4(_cellArea.x, _cellArea.y, _cellArea.width, _cellArea.height);
                return;
            }

            // 메시 공간 = 바인드 포즈 공간(BossAMaterialSpace). X 가 좌우, Z 가 위
            Bounds meshBounds = TryGetComponent(out SkinnedMeshRenderer skinned) && skinned.sharedMesh ? skinned.sharedMesh.bounds : new Bounds();
            _bounds = new Vector4(meshBounds.min.x, meshBounds.min.z, meshBounds.size.x, meshBounds.size.z);
        }

        private void OnEnable()
        {
            _nextPickTime = 0.0f;
        }

        private void LateUpdate()
        {
            if (Time.time >= _nextPickTime)
            {
                _mask = BossAGlitchCells.PickMask(_random, _candidateCells, _cellCountRange.x, _cellCountRange.y);
                _nextPickTime = Time.time + Random.Range(_intervalRange.x, _intervalRange.y);
            }

            ApplyProperties(_mask);
        }

        private void OnDisable()
        {
            if (_targets != null) ApplyProperties(0);
        }
        #endregion

        #region Private Methods
        private void ApplyProperties(int mask)
        {
            foreach (Renderer target in _targets)
            {
                if (!target) continue;

                target.GetPropertyBlock(_properties);
                _properties.SetFloat(MaskId, mask);
                _properties.SetVector(BoundsId, _bounds);
                _properties.SetVector(ShiftId, new Vector4(_bandsPerCell, _shiftAmount, _shiftRate, _shiftChance));
                _properties.SetVector(SparkId, new Vector4(_sparkPeriod, _sparkDuration, _sparkWidth, _sparkIntensity));
                _properties.SetColor(SparkColorId, _sparkColor);
                target.SetPropertyBlock(_properties);
            }
        }
        #endregion
    }
}
