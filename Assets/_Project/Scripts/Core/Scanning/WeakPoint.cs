using UnityEngine;

namespace Core.Scanning
{
    /// <summary>
    /// 적의 약점 부위. 약점 위치의 콜라이더가 있는 오브젝트에 붙이고, 이 부위를 맞혔을 때 적용할 데미지 배율을 가진다.
    /// 배율을 실제 피해에 곱하는 곳(총알, 근접 판정)은 타 파트 코드이므로 GetMultiplier 로 조회해 쓰도록 요청한다.
    /// 스캔으로 드러나는 연출은 WeakPointReaction 이 맡고, 배율은 드러남과 무관하게 항상 적용된다.
    /// </summary>
    public class WeakPoint : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Settings")]
        [Tooltip("이 부위를 맞혔을 때 데미지에 곱할 배율")]
        [SerializeField, Min(1.0f)] private float _damageMultiplier = 2.0f;
        #endregion

        #region Properties
        /// <summary>이 부위를 맞혔을 때 데미지에 곱할 배율.</summary>
        public float DamageMultiplier => _damageMultiplier;
        #endregion

        #region Public Methods
        /// <summary>
        /// 맞은 콜라이더에 적용할 데미지 배율을 구한다.
        /// </summary>
        /// <param name="hitCollider">맞은 콜라이더</param>
        /// <returns>약점이면 그 배율, 아니면 1</returns>
        public static float GetMultiplier(Collider hitCollider)
        {
            // 맞은 콜라이더에 WeakPoint 가 붙어 있으면 약점을 맞힌 것이다.
            if (hitCollider && hitCollider.TryGetComponent(out WeakPoint weakPoint))
            {
                return weakPoint.DamageMultiplier;
            }

            // 약점이 아니면 데미지를 그대로 적용한다(1배).
            return 1.0f;
        }
        #endregion
    }
}
