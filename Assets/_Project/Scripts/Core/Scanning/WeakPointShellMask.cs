using System.Collections.Generic;
using UnityEngine;

namespace Core.Scanning
{
    /// <summary>
    /// 스캔 홀로그램 셸(Project/ScanHologram)에 약점 구의 위치와 반경을 넘겨, 약점 구 안에 들어온 외곽선·격자만 다른 색으로 보이게 한다.
    /// 구는 약점의 SphereCollider 값을 그대로 쓰므로, 색이 바뀐 범위와 맞는 범위가 일치한다.
    /// 꺼져 있는 약점은 넘기지 않으므로 WeakPointReaction 이 약점을 켜고 끄는 것에 색도 따라간다.
    /// 셸 렌더러는 SilhouetteReaction 에서 가져온다(런타임에 자동 생성된 셸 포함). 셸이 꺼져 있는 동안(스캔 전)은 아무것도 하지 않는다.
    /// </summary>
    public class WeakPointShellMask : MonoBehaviour
    {
        #region Constants
        // 셰이더의 MAX_WEAK_POINTS 와 같아야 한다.
        private const int MAX_WEAK_POINTS = 4;
        #endregion

        #region Static Fields
        private static readonly int WeakPointsId = Shader.PropertyToID("_WeakPoints");
        private static readonly int WeakPointCountId = Shader.PropertyToID("_WeakPointCount");
        #endregion

        #region Serialized Fields
        [Header("References")]
        [Tooltip("홀로그램 셸 렌더러(M_ScanHologram)를 켜고 끄는 SilhouetteReaction. 비어 있으면 같은 오브젝트에서 찾는다.")]
        // 셸이 런타임에 만들어질 수 있어 렌더러를 직접 연결하지 않고, 반응에서 목록을 읽는다.
        [SerializeField] private SilhouetteReaction _silhouette;
        [Tooltip("약점 콜라이더. 최대 4개까지 반영한다. 비어 있으면 자식 중 WeakPoint 태그가 달린 SphereCollider 를 찾는다.")]
        [SerializeField] private SphereCollider[] _weakPoints;
        #endregion

        #region Private Fields
        // 렌더러에 이미 있는 값(SilhouetteReaction 의 색 등)을 유지한 채 약점 값만 덮어쓰기 위한 블록
        private MaterialPropertyBlock _propertyBlock;
        // 셰이더에 넘길 약점 구 목록(xyz 월드 위치, w 월드 반경). 배열 크기는 처음 넘긴 크기로 고정되므로 항상 최대 크기로 넘긴다.
        private readonly Vector4[] _weakPointData = new Vector4[MAX_WEAK_POINTS];
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // 프리팹마다 연결하지 않아도 되도록 비어 있으면 직접 찾는다.
            if (!_silhouette) _silhouette = GetComponent<SilhouetteReaction>();
            if ((_weakPoints == null) || (_weakPoints.Length == 0)) _weakPoints = FindTaggedWeakPoints();

            Debug.Assert(_silhouette != null, $"[{name}] SilhouetteReaction 이 없습니다.");
            Debug.Assert(
                _weakPoints.Length <= MAX_WEAK_POINTS,
                $"[{name}] 약점은 최대 {MAX_WEAK_POINTS}개까지 반영됩니다.");

            _propertyBlock = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            // 애니메이션으로 본이 움직인 뒤의 위치를 넘기기 위해 LateUpdate 에서 갱신한다.
            // LateUpdate 는 모든 Awake 뒤라서 자동 생성된 셸 목록도 이미 채워져 있다.
            if (!_silhouette) return;

            Renderer[] shellRenderers = _silhouette.SilhouetteRenderers;
            if (shellRenderers == null) return;

            int count = 0;
            foreach (SphereCollider weakPoint in _weakPoints)
            {
                if (count >= MAX_WEAK_POINTS) break;

                // 꺼진(스캔 중이 아닌) 약점은 색을 칠하지 않는다.
                if (!weakPoint || !weakPoint.enabled || !weakPoint.gameObject.activeInHierarchy) continue;

                _weakPointData[count] = GetWorldSphere(weakPoint);
                count++;
            }

            foreach (Renderer shellRenderer in shellRenderers)
            {
                // 꺼진 셸(스캔 전)은 건너뛴다.
                if (!shellRenderer || !shellRenderer.enabled) continue;

                shellRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetVectorArray(WeakPointsId, _weakPointData);
                _propertyBlock.SetFloat(WeakPointCountId, count);
                shellRenderer.SetPropertyBlock(_propertyBlock);
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 자식 중 WeakPoint 태그가 달린 SphereCollider 를 모두 찾는다. 평소에는 꺼져 있으므로 비활성 자식까지 찾는다.
        /// </summary>
        /// <returns>찾은 약점 콜라이더 목록</returns>
        private SphereCollider[] FindTaggedWeakPoints()
        {
            List<SphereCollider> weakPoints = new List<SphereCollider>();
            foreach (SphereCollider sphere in GetComponentsInChildren<SphereCollider>(true))
            {
                if (sphere.CompareTag(WeakPointReaction.WEAK_POINT_TAG)) weakPoints.Add(sphere);
            }

            return weakPoints.ToArray();
        }

        /// <summary>
        /// SphereCollider 의 월드 중심과 월드 반경을 구한다. 반경은 물리 엔진처럼 가장 큰 축 스케일을 곱한다.
        /// </summary>
        /// <param name="sphere">약점 콜라이더</param>
        /// <returns>xyz 월드 중심, w 월드 반경</returns>
        private static Vector4 GetWorldSphere(SphereCollider sphere)
        {
            Transform sphereTransform = sphere.transform;
            Vector3 center = sphereTransform.TransformPoint(sphere.center);
            Vector3 scale = sphereTransform.lossyScale;
            float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));

            return new Vector4(center.x, center.y, center.z, sphere.radius * maxScale);
        }
        #endregion
    }
}
