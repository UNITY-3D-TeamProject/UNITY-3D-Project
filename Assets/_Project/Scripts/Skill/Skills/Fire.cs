using System.Collections;
using UnityEngine;
using Projectile;
using Skill.Core;

namespace Skill.Skills
{
    /// <summary>
    /// 누르고 있는 동안 일정 간격으로 사격하는 연사 스킬.
    /// 발동 시 첫 발을 쏘고, 이후 간격마다 조건 확인과 코스트 지불을 거쳐 사격한다.
    /// 지불에 실패하거나 Stop 이 호출되면 사격을 멈춘다.
    /// </summary>
    public class Fire : SkillBase
    {
        #region Serialized Fields
        [Header("Fire")]
        [Tooltip("사격 간격(초)")]
        [SerializeField, Min(0.0f)] private float _duration = 0.1f;

        [Header("References")]
        [Tooltip("발사할 총알 프리팹")]
        [SerializeField] private BulletController _bulletPrefab;
        [Tooltip("총알이 생성될 위치")]
        [SerializeField] private Transform _firePoint;
        #endregion

        #region Private Fields
        private WaitForSeconds _waitDuration;
        private Coroutine _fireCoroutine;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            _waitDuration = new WaitForSeconds(_duration);

            Debug.Assert(_bulletPrefab != null, $"[{name}] 총알 프리팹이 연결되지 않았습니다.");
            Debug.Assert(_firePoint != null, $"[{name}] 발사 지점이 연결되지 않았습니다.");
        }

        private void OnDisable()
        {
            Stop();
        }
        #endregion

        #region Public Methods
        public override void Stop()
        {
            if (_fireCoroutine == null) return;

            StopCoroutine(_fireCoroutine);
            _fireCoroutine = null;
        }
        #endregion

        #region Protected Methods
        protected override void Execute()
        {
            Instantiate(_bulletPrefab, _firePoint.position, _firePoint.rotation);
            base.Execute();

            // 연사 중 재발동(코루틴 내부 TryExecute)이면 한 발만 쏘고 코루틴은 새로 시작하지 않는다.
            if (_fireCoroutine != null) return;
            _fireCoroutine = StartCoroutine(CoFire());
        }
        #endregion

        #region Coroutines
        private IEnumerator CoFire()
        {
            while (true)
            {
                yield return _waitDuration;

                // 조건 불충족 또는 코스트 지불 불가 시 사격 종료
                if (!TryExecute()) break;
            }

            _fireCoroutine = null;
        }
        #endregion
    }
}
