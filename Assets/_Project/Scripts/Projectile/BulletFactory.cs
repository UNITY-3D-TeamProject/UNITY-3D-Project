using UnityEngine;
using Attribute.Core;
using Attribute.Effect;

namespace Projectile
{
    /// <summary>
    /// 총알 프리팹을 생성하고 비행 정보를 설정하는 컴포넌트.
    /// 생성 위치·방향·효과 주체는 호출하는 쪽이 매번 넘긴다.
    /// </summary>
    public class BulletFactory : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Bullet")]
        [Tooltip("생성할 총알 프리팹")]
        [SerializeField] private BulletController _bulletPrefab;
        [Tooltip("총알이 맞힌 대상에 적용할 효과")]
        [SerializeField] private SOAttributeEffect _hitEffect;
        #endregion

        #region Public Methods
        /// <summary>
        /// 총알을 생성하고 방향·효과·주체를 설정한다.
        /// </summary>
        /// <param name="position">생성 위치(월드 좌표)</param>
        /// <param name="direction">비행 방향. zero 면 BulletController 의 기본 방향(월드 z+)을 사용한다.</param>
        /// <param name="cursor">효과를 발생시키는 주체. 효과의 ValueSource 가 Attribute 일 때 필요하다.</param>
        public void Create(Vector3 position, Vector3 direction, IEffectTarget cursor = null)
        {
            if (_bulletPrefab == null)
            {
                Debug.LogError($"[{name}] 총알 프리팹이 연결되지 않았습니다.", this);
                return;
            }

            Quaternion rotation = (direction == Vector3.zero) ? Quaternion.identity : Quaternion.LookRotation(direction);

            BulletController bullet = Instantiate(_bulletPrefab, position, rotation);
            bullet.Initialize(direction, _hitEffect, cursor);
        }
        #endregion
    }
}
