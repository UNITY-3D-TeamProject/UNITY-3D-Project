using UnityEngine;
using Attribute.Core;
using Core.ObjectPool;

namespace Projectile
{
    /// <summary>
    /// 오브젝트 풀에서 총알을 꺼내고 비행 정보를 설정하는 컴포넌트.
    /// 비차지/차지 총알 키 중 하나로 꺼내며, 피격 효과는 각 총알 프리팹이 갖는다.
    /// 생성 위치·방향·효과 주체는 호출하는 쪽이 매번 넘긴다.
    /// </summary>
    public class BulletFactory : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Pool Keys")]
        [Tooltip("비차지샷으로 꺼낼 총알의 ObjectPoolManager 키")]
        [SerializeField] private string _bulletKey = "Bullet";
        [Tooltip("차지샷으로 꺼낼 총알의 ObjectPoolManager 키")]
        [SerializeField] private string _chargedBulletKey = "ChargedBullet";
        #endregion

        #region Public Methods
        /// <summary>
        /// 비차지 총알을 생성하고 방향·주체를 설정한다.
        /// </summary>
        /// <param name="position">생성 위치(월드 좌표)</param>
        /// <param name="direction">비행 방향. zero 면 BulletController 의 기본 방향(월드 z+)을 사용한다.</param>
        /// <param name="cursor">효과를 발생시키는 주체. 효과의 ValueSource 가 Attribute 일 때 필요하다.</param>
        public void Create(Vector3 position, Vector3 direction, IEffectTarget cursor = null)
        {
            Create(position, direction, false, cursor);
        }

        /// <summary>
        /// 차지 여부에 맞는 총알을 풀에서 꺼내고 방향·주체를 설정한다.
        /// </summary>
        /// <param name="position">생성 위치(월드 좌표)</param>
        /// <param name="direction">비행 방향. zero 면 BulletController 의 기본 방향(월드 z+)을 사용한다.</param>
        /// <param name="isCharged">차지샷이면 true</param>
        /// <param name="cursor">효과를 발생시키는 주체. 효과의 ValueSource 가 Attribute 일 때 필요하다.</param>
        public void Create(Vector3 position, Vector3 direction, bool isCharged, IEffectTarget cursor = null)
        {
            if (ObjectPoolManager.Instance == null)
            {
                Debug.LogError($"[{name}] 씬에 ObjectPoolManager가 없어 총알을 생성할 수 없습니다.", this);
                return;
            }

            string key = isCharged ? _chargedBulletKey : _bulletKey;
            Quaternion rotation = (direction == Vector3.zero) ? Quaternion.identity : Quaternion.LookRotation(direction);

            // 키 미등록 등으로 실패하면 매니저가 에러 로그를 남기고 null 을 돌려준다
            BulletController bullet = ObjectPoolManager.Instance.Get<BulletController>(key, position, rotation);
            if (bullet == null) return;

            bullet.Initialize(direction, cursor);
        }
        #endregion
    }
}
