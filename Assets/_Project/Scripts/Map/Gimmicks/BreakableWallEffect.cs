using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.Audio;

namespace Map.Gimmicks
{
    /// <summary>
    /// 부서지는 벽의 콘크리트 붕괴 연출. BreakableWallStages.Hit 을 받아 맞은 자리에 금 데칼·튀는 조각·먼지를 만들고,
    /// 마지막 발에는 벽을 조각으로 나눠 맞은 곳에서 먼 순서로 무너뜨린다.
    /// 조각은 충돌체 없이 이 컴포넌트가 직접 떨어뜨리므로 플레이어·총알을 막지 않는다(바닥 = Debris Space 의 y 0).
    /// Debris Space·Decal Space 는 스케일 1 인 공간이어야 하고, 원점은 벽 아래 가운데, 앞면은 -z 쪽이다(단위 m).
    /// </summary>
    public class BreakableWallEffect : MonoBehaviour
    {
        #region Constants
        private const float GRAVITY = 11.0f;
        private const float SPALL_LIFE = 2.2f;
        private const float SHRINK_TIME = 0.4f;
        private const float SHAKE_TIME = 0.15f;
        private const float SHAKE_AMOUNT = 0.035f;
        private const float DECAL_GROW_TIME = 0.1f;
        private const float COLLAPSE_SPEED = 6.5f;
        private const float STAY_TOP_CHANCE = 0.6f;
        #endregion

        #region Serialized Fields
        [Header("References")]
        [SerializeField] private BreakableWallStages _stages;

        [Tooltip("스케일 1 인 공간. 튀는 조각과 무너진 조각이 들어간다. 벽 몸체가 꺼져도 켜져 있어야 한다.")]
        [SerializeField] private Transform _debrisSpace;

        [Tooltip("스케일 1 인 데칼 공간. 벽 몸체 아래에 두어 부서지면 같이 꺼진다.")]
        [SerializeField] private Transform _decalSpace;

        [Tooltip("맞을 때 떨리는 겉모습 (선택)")]
        [SerializeField] private Transform _shakeTarget;

        [Header("Wall")]
        [SerializeField] private Vector2 _wallSize = new Vector2(5.5f, 10.0f);
        [SerializeField, Min(0.05f)] private float _thickness = 0.5f;
        [Tooltip("무너질 때 나눌 조각 수 (가로, 세로)")]
        [SerializeField] private Vector2Int _grid = new Vector2Int(7, 12);
        [Tooltip("조각 앞뒤 면. 벽 면과 같은 텍스처를 쓰면 조각이 벽 그대로 보인다.")]
        [SerializeField] private Material _faceMaterial;
        [Tooltip("조각 옆면")]
        [SerializeField] private Material _sideMaterial;

        [Header("Hit")]
        [Tooltip("맞은 횟수별 금 데칼 (1발, 2발 ...)")]
        [SerializeField] private Material[] _crackDecals;
        [Tooltip("맞은 횟수별 금 데칼 크기(m)")]
        [SerializeField] private float[] _crackSizes = { 1.4f, 2.3f };
        [SerializeField] private ParticleSystem _dust;
        [SerializeField] private ParticleSystem _bits;
        [SerializeField] private ParticleSystem _flash;

        [Header("Sound")]
        [Tooltip("마지막 발에 벽이 무너질 때 맞은 자리에서 낼 소리 (비우면 무음)")]
        [SerializeField] private SOAudioData _breakSound;
        #endregion

        #region Private Fields
        private class Piece
        {
            public Transform Transform;
            public Mesh Mesh;
            public Vector3 Velocity;
            public Vector3 Spin;
            public float Delay;
            public float Radius;
            public float Life = -1.0f;
            public bool IsActive;
            public bool IsSettled;
        }

        private readonly List<Piece> _pieces = new List<Piece>();
        private Vector3 _shakeOrigin;
        private float _shakeRemaining;
        private bool _isCollapsed;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_stages != null, $"[{name}] BreakableWallStages가 연결되지 않았습니다.");
            Debug.Assert(_debrisSpace != null, $"[{name}] Debris Space가 연결되지 않았습니다.");
        }

        private void OnEnable()
        {
            if (_stages != null) _stages.Hit += HandleHit;
        }

        private void OnDisable()
        {
            if (_stages != null) _stages.Hit -= HandleHit;
        }

        private void Update()
        {
            UpdateShake();
            UpdatePieces(Time.deltaTime);
        }

        private void OnDestroy()
        {
            foreach (Piece piece in _pieces)
            {
                if (piece.Mesh != null) Destroy(piece.Mesh);
            }
        }
        #endregion

        #region Private Methods
        private void HandleHit(int hitCount, Vector3? hitPoint, Vector3? direction)
        {
            float halfThick = _thickness * 0.5f;

            // 위치를 모르면 앞면 1.6 m 높이를 맞은 것으로 본다
            Vector3 local = hitPoint.HasValue ? _debrisSpace.InverseTransformPoint(hitPoint.Value) : new Vector3(0.0f, 1.6f, -halfThick);
            float side = (local.z < 0.0f) ? -1.0f : 1.0f;
            local.x = Mathf.Clamp(local.x, (-_wallSize.x * 0.5f) + 0.1f, (_wallSize.x * 0.5f) - 0.1f);
            local.y = Mathf.Clamp(local.y, 0.1f, _wallSize.y - 0.1f);
            local.z = side * halfThick;

            Vector3 normal = new Vector3(0.0f, 0.0f, side);
            Vector3 push = direction.HasValue ? _debrisSpace.InverseTransformDirection(direction.Value).normalized : -normal;

            PlayHitParticles(local, normal, hitCount >= _stages.HitsToBreak);
            StartShake();

            if (hitCount >= _stages.HitsToBreak)
            {
                GimmickUtility.PlaySfx(_breakSound, _debrisSpace.TransformPoint(local));
                Collapse(local, push);
                return;
            }

            AddCrackDecal(local, side, hitCount);
            SpawnSpall(local, normal, 5 + (hitCount * 2));
        }

        private void AddCrackDecal(Vector3 local, float side, int hitCount)
        {
            if ((_decalSpace == null) || (_crackDecals == null) || (_crackDecals.Length == 0)) return;

            int index = Mathf.Min(hitCount - 1, _crackDecals.Length - 1);
            float size = _crackSizes[Mathf.Min(hitCount - 1, _crackSizes.Length - 1)];

            // 데칼이 벽 밖으로 크게 나가지 않게 중심을 안쪽으로 당긴다
            float marginX = Mathf.Min(size * 0.35f, _wallSize.x * 0.5f);
            float x = Mathf.Clamp(local.x, (-_wallSize.x * 0.5f) + marginX, (_wallSize.x * 0.5f) - marginX);
            float y = Mathf.Clamp(local.y, size * 0.35f, _wallSize.y - (size * 0.35f));

            GameObject decal = GameObject.CreatePrimitive(PrimitiveType.Quad);
            decal.name = $"Crack_{hitCount}";
            Destroy(decal.GetComponent<Collider>());
            decal.transform.SetParent(_decalSpace, false);
            decal.transform.localPosition = new Vector3(x, y, side * ((_thickness * 0.5f) + 0.012f + (hitCount * 0.003f)));
            decal.transform.localRotation = Quaternion.Euler(0.0f, (side < 0.0f) ? 0.0f : 180.0f, Random.Range(0.0f, 360.0f));

            MeshRenderer decalRenderer = decal.GetComponent<MeshRenderer>();
            decalRenderer.sharedMaterial = _crackDecals[index];
            decalRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            StartCoroutine(CoGrow(decal.transform, size));
        }

        private void SpawnSpall(Vector3 local, Vector3 normal, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Random.insideUnitCircle * 0.25f;
                float radius = Random.Range(0.07f, 0.16f);
                float depth = Random.Range(0.05f, 0.1f);
                Vector2 center = new Vector2(local.x + offset.x, local.y + offset.y);
                Vector2[] poly = MakeIrregular(center, radius, 5);

                float surface = local.z;
                Piece piece = CreatePiece($"Spall_{i}", poly, surface, surface - (normal.z * depth));
                piece.IsActive = true;
                piece.Life = SPALL_LIFE + Random.Range(0.0f, 0.5f);
                piece.Velocity = (normal * Random.Range(1.5f, 3.5f)) + (Vector3.up * Random.Range(1.0f, 2.5f)) + new Vector3(offset.x, offset.y, 0.0f) * 6.0f;
                piece.Spin = Random.insideUnitSphere * 12.0f;
            }
        }

        private void Collapse(Vector3 hit, Vector3 push)
        {
            if (_isCollapsed) return;
            _isCollapsed = true;

            float halfW = _wallSize.x * 0.5f, halfT = _thickness * 0.5f;
            int cols = _grid.x, rows = _grid.y;
            float cellW = _wallSize.x / cols, cellH = _wallSize.y / rows;

            // 격자 꼭짓점을 흔들어 조각 모양을 불규칙하게 만든다 (바깥 테두리는 곧게)
            var points = new Vector2[cols + 1, rows + 1];
            for (int j = 0; j <= rows; j++)
            {
                for (int i = 0; i <= cols; i++)
                {
                    bool isEdgeX = (i == 0) || (i == cols);
                    bool isEdgeY = (j == 0) || (j == rows);
                    float jx = isEdgeX ? 0.0f : Random.Range(-0.35f, 0.35f) * cellW;
                    float jy = isEdgeY ? 0.0f : Random.Range(-0.35f, 0.35f) * cellH;
                    points[i, j] = new Vector2(-halfW + (i * cellW) + jx, (j * cellH) + jy);
                }
            }

            Vector2 hit2 = new Vector2(hit.x, hit.y);
            for (int j = 0; j < rows; j++)
            {
                for (int i = 0; i < cols; i++)
                {
                    Vector2[] poly = { points[i, j], points[i + 1, j], points[i + 1, j + 1], points[i, j + 1] };
                    Piece piece = CreatePiece($"Chunk_{i}_{j}", poly, -halfT, halfT);

                    // 맨 윗줄 일부는 걸린 채 남는다
                    bool isStaying = (j == rows - 1) && (Random.value < STAY_TOP_CHANCE);
                    if (isStaying) continue;

                    Vector2 center = (poly[0] + poly[1] + poly[2] + poly[3]) * 0.25f;
                    Vector2 away = center - hit2;
                    piece.Delay = (away.magnitude / COLLAPSE_SPEED) + Random.Range(0.0f, 0.08f);
                    piece.Velocity = (push * Random.Range(1.2f, 2.6f))
                                   + ((Vector3)away.normalized * Random.Range(0.5f, 1.5f))
                                   + (Vector3.up * Random.Range(0.0f, 1.2f));
                    piece.Spin = Random.insideUnitSphere * Random.Range(0.5f, 3.0f);
                }
            }

            StartCoroutine(CoFloorDust());
        }

        private Piece CreatePiece(string pieceName, Vector2[] poly, float zFront, float zBack)
        {
            Mesh mesh = BuildPrism(poly, zFront, zBack, out Vector3 center, out float radius);

            var go = new GameObject(pieceName);
            go.transform.SetParent(_debrisSpace, false);
            go.transform.localPosition = center;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { _faceMaterial, _sideMaterial };

            var piece = new Piece { Transform = go.transform, Mesh = mesh, Radius = radius };
            _pieces.Add(piece);
            return piece;
        }

        /// <summary>볼록 다각형(벽 면 좌표)을 zFront ~ zBack 두께로 늘린 조각. 앞뒤 면 UV 는 벽 전체 기준이라 벽 텍스처가 이어진다.</summary>
        private Mesh BuildPrism(Vector2[] poly, float zFront, float zBack, out Vector3 center, out float radius)
        {
            Vector2 c2 = Vector2.zero;
            foreach (Vector2 p in poly) c2 += p;
            c2 /= poly.Length;
            center = new Vector3(c2.x, c2.y, (zFront + zBack) * 0.5f);

            radius = 0.0f;
            foreach (Vector2 p in poly) radius = Mathf.Max(radius, (p - c2).magnitude);

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var faces = new List<int>();
            var sides = new List<int>();
            float dirFront = Mathf.Sign(zFront - zBack);
            Vector3 c3 = center;

            Vector2 WallUV(Vector2 p) => new Vector2((p.x + (_wallSize.x * 0.5f)) / _wallSize.x, p.y / _wallSize.y);

            void AddTriangle(List<int> list, int a, int b, int c, Vector3 wantedNormal)
            {
                Vector3 n = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
                if (Vector3.Dot(n, wantedNormal) < 0.0f)
                {
                    (b, c) = (c, b);
                }

                list.Add(a);
                list.Add(b);
                list.Add(c);
            }

            // 앞면과 뒷면
            foreach (float z in new[] { zFront, zBack })
            {
                Vector3 wanted = new Vector3(0.0f, 0.0f, (z == zFront) ? dirFront : -dirFront);
                int start = vertices.Count;
                foreach (Vector2 p in poly)
                {
                    vertices.Add(new Vector3(p.x, p.y, z) - c3);
                    uvs.Add(WallUV(p));
                }

                for (int i = 1; i < poly.Length - 1; i++)
                {
                    AddTriangle(faces, start, start + i, start + i + 1, wanted);
                }
            }

            // 옆면
            for (int i = 0; i < poly.Length; i++)
            {
                Vector2 a = poly[i], b = poly[(i + 1) % poly.Length];
                Vector2 mid = (a + b) * 0.5f;
                Vector3 wanted = new Vector3(mid.x - c2.x, mid.y - c2.y, 0.0f);
                int start = vertices.Count;
                vertices.Add(new Vector3(a.x, a.y, zFront) - c3);
                vertices.Add(new Vector3(b.x, b.y, zFront) - c3);
                vertices.Add(new Vector3(b.x, b.y, zBack) - c3);
                vertices.Add(new Vector3(a.x, a.y, zBack) - c3);
                float len = (b - a).magnitude;
                uvs.Add(new Vector2(0.0f, 0.0f));
                uvs.Add(new Vector2(len, 0.0f));
                uvs.Add(new Vector2(len, 1.0f));
                uvs.Add(new Vector2(0.0f, 1.0f));
                AddTriangle(sides, start, start + 1, start + 2, wanted);
                AddTriangle(sides, start, start + 2, start + 3, wanted);
            }

            var mesh = new Mesh { name = "WallPiece" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(faces, 0);
            mesh.SetTriangles(sides, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector2[] MakeIrregular(Vector2 center, float radius, int count)
        {
            var poly = new Vector2[count];
            float startAngle = Random.Range(0.0f, Mathf.PI * 2.0f);
            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + ((i / (float)count) * Mathf.PI * 2.0f) + Random.Range(-0.2f, 0.2f);
                float r = radius * Random.Range(0.65f, 1.1f);
                poly[i] = center + (new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r);
            }

            return poly;
        }

        private void UpdatePieces(float deltaTime)
        {
            for (int i = _pieces.Count - 1; i >= 0; i--)
            {
                Piece piece = _pieces[i];
                if (!piece.IsActive)
                {
                    if (piece.Delay <= 0.0f) continue;

                    piece.Delay -= deltaTime;
                    if (piece.Delay <= 0.0f) piece.IsActive = true;
                    continue;
                }

                if (piece.Life > 0.0f)
                {
                    piece.Life -= deltaTime;
                    if (piece.Life <= 0.0f)
                    {
                        Destroy(piece.Transform.gameObject);
                        Destroy(piece.Mesh);
                        _pieces.RemoveAt(i);
                        continue;
                    }

                    if (piece.Life < SHRINK_TIME) piece.Transform.localScale = Vector3.one * (piece.Life / SHRINK_TIME);
                }

                if (piece.IsSettled) continue;

                Transform t = piece.Transform;
                piece.Velocity.y -= GRAVITY * deltaTime;
                Vector3 position = t.localPosition + (piece.Velocity * deltaTime);
                t.localRotation = Quaternion.Euler(piece.Spin * (Mathf.Rad2Deg * deltaTime)) * t.localRotation;

                // 바닥에 닿으면 튀다가 멈춘다
                float rest = piece.Radius * 0.45f;
                if (position.y < rest)
                {
                    position.y = rest;
                    if (piece.Velocity.y < -1.0f)
                    {
                        piece.Velocity.y *= -0.25f;
                        piece.Velocity.x *= 0.6f;
                        piece.Velocity.z *= 0.6f;
                        piece.Spin *= 0.5f;
                    }
                    else
                    {
                        piece.IsSettled = true;
                    }
                }

                t.localPosition = position;
            }
        }

        private void PlayHitParticles(Vector3 local, Vector3 normal, bool isBreaking)
        {
            Vector3 position = _debrisSpace.TransformPoint(local + (normal * 0.05f));
            Quaternion rotation = Quaternion.LookRotation(_debrisSpace.TransformDirection(normal));

            Emit(_flash, position, rotation, 1);
            Emit(_dust, position, rotation, isBreaking ? 40 : 16);
            Emit(_bits, position, rotation, isBreaking ? 30 : 14);
        }

        private static void Emit(ParticleSystem system, Vector3 position, Quaternion rotation, int count)
        {
            if (system == null) return;

            system.transform.SetPositionAndRotation(position, rotation);
            system.Emit(count);
        }

        private void StartShake()
        {
            if (_shakeTarget == null) return;
            if (_shakeRemaining <= 0.0f) _shakeOrigin = _shakeTarget.localPosition;
            _shakeRemaining = SHAKE_TIME;
        }

        private void UpdateShake()
        {
            if ((_shakeTarget == null) || (_shakeRemaining <= 0.0f)) return;

            _shakeRemaining -= Time.deltaTime;
            if (_shakeRemaining <= 0.0f)
            {
                _shakeTarget.localPosition = _shakeOrigin;
                return;
            }

            // 면과 같은 평면 안에서만 흔든다 (앞뒤로 흔들면 면이 몸체 안으로 들어가 가려진다)
            float amount = SHAKE_AMOUNT * (_shakeRemaining / SHAKE_TIME);
            _shakeTarget.localPosition = _shakeOrigin + new Vector3(Random.Range(-amount, amount), Random.Range(-amount, amount) * 0.4f, 0.0f);
        }
        #endregion

        #region Coroutines
        private IEnumerator CoGrow(Transform target, float size)
        {
            for (float t = 0.0f; t < DECAL_GROW_TIME; t += Time.deltaTime)
            {
                if (target == null) yield break;

                float k = Mathf.SmoothStep(0.25f, 1.0f, t / DECAL_GROW_TIME);
                target.localScale = Vector3.one * (size * k);
                yield return null;
            }

            if (target != null) target.localScale = Vector3.one * size;
        }

        private IEnumerator CoFloorDust()
        {
            yield return new WaitForSeconds(0.6f);

            for (int i = 0; i < 10; i++)
            {
                float x = Random.Range(-_wallSize.x * 0.5f, _wallSize.x * 0.5f);
                Vector3 position = _debrisSpace.TransformPoint(new Vector3(x, 0.2f, Random.Range(-1.0f, 1.0f)));
                Emit(_dust, position, Quaternion.LookRotation(_debrisSpace.up), 3);
            }
        }
        #endregion
    }
}
