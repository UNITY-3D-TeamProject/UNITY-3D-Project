using System;
using UnityEngine;
using UnityEngine.Serialization;
using Attribute.Core;
using Projectile;

namespace Mediator.SubMediators
{
    /// <summary>
    /// 캐릭터의 생성 요청(총알 등)을 실제 생성 컴포넌트로 전달하는 접착 컴포넌트.
    /// 생성에 필요한 외부 값(발사 방향, 효과 주체)은 CharacterMediator 가 주입한다.
    /// </summary>
    public class SpawnMediator : MediatorBase
    {
        #region Serialized Fields
        [Header("References")]
        [FormerlySerializedAs("_bulletSpawner")]
        [SerializeField] private BulletFactory _bulletFactory;
        #endregion

        #region Private Fields
        private Func<Vector3> _getFireDirection;
        private IEffectTarget _effectCursor;
        #endregion

        #region Public Methods
        /// <summary>
        /// 총알 발사 방향을 얻기 위한 델리게이트 주입
        /// </summary>
        public void SetGetFireDirection(Func<Vector3> getFireDirection)
        {
            if (getFireDirection == null) return;
            _getFireDirection = getFireDirection;
        }

        /// <summary>
        /// 총알 발사 방향 델리게이트 제거
        /// </summary>
        public void ClearGetFireDirection()
        {
            _getFireDirection = null;
        }

        /// <summary>
        /// 총알 효과를 발생시키는 주체(cursor) 설정
        /// </summary>
        public void SetEffectCursor(IEffectTarget effectCursor)
        {
            if (effectCursor == null) return;
            _effectCursor = effectCursor;
        }

        /// <summary>
        /// 총알 효과를 발생시키는 주체(cursor) 제거
        /// </summary>
        public void ClearEffectCursor()
        {
            _effectCursor = null;
        }

        /// <summary>
        /// 주입된 발사 방향과 효과 주체로 BulletFactory 에 총알 생성을 요청한다.
        /// </summary>
        /// <param name="position">총알 생성 위치</param>
        public void CommandSpawnBullet(Vector3 position)
        {
            if (!_bulletFactory) return;

            Vector3 direction = _getFireDirection?.Invoke() ?? Vector3.zero;
            _bulletFactory.Create(position, direction, _effectCursor);
        }
        #endregion

        #region Protected Methods
        protected override void InitAttributeCallback()
        {
        }

        protected override void InitValue()
        {
        }
        #endregion
    }
}
