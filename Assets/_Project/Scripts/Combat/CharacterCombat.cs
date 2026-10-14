using System;
using UnityEngine;

namespace Combat
{
    /// <summary>
    /// 피격 판정과 사망 판정만 담당하는 공용 전투 컴포넌트.
    /// 체력은 외부(옵서버)가 Health에 밀어넣고, 줄어든 순간을 피격으로 보아 이벤트로만 방출한다.
    /// UnityEngine 외 어떤 시스템도 참조하지 않으므로 이 폴더만 통째로 이식할 수 있다.
    /// </summary>
    public class CharacterCombat : MonoBehaviour
    {
        #region Serialized Fields
        [Tooltip("약점 콜라이더. 없으면 치명타 없음")]
        [SerializeField] private SphereCollider _weakPoint;
        #endregion

        #region Private Fields
        private float _health;
        private bool _isDead;
        #endregion

        #region Properties
        /// <summary>
        /// 현재 체력. 외부(옵서버)가 설정한다.
        /// 값이 줄어들면 피격으로 보고 OnHit을, 0 이하가 되는 순간 한 번만 OnDeath를 방출한다.
        /// </summary>
        public float Health
        {
            get => _health;
            set
            {
                bool isDamaged = value < _health;
                _health = value;

                
                //if (_isDead) return;
                //if (_health <= 0) return;

                if (isDamaged) OnHit?.Invoke();
                CheckDeath();
            }
        }

        public bool IsDead => _health <= 0;
        #endregion

        #region Events
        public event Action OnHit;
        public event Action OnDeath;
        #endregion

        #region Public Methods
        /// <summary>
        /// 맞은 지점에서 탄도를 따라 연장한 선이 약점 반지름 안을 지나는지 거리로 판정한다.
        /// 약점이 없거나 꺼져 있거나, 맞은 지점·방향 정보가 없으면 일반 피격으로 본다.
        /// </summary>
        /// <param name="hitPoint">효과가 실제로 닿은 월드 위치</param>
        /// <param name="direction">효과가 날아온 월드 방향</param>
        /// <returns>약점 중심과 탄도선 사이의 최단 거리가 반지름 이하이면 true</returns>
        public bool IsCriticalHit(Vector3? hitPoint, Vector3? direction)
        {
            if (hitPoint == null || direction == null) return false;
            if (!_weakPoint || !_weakPoint.enabled || !_weakPoint.gameObject.activeInHierarchy) return false;

            Vector3 center = _weakPoint.transform.TransformPoint(_weakPoint.center);
            Vector3 scale = _weakPoint.transform.lossyScale;
            float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            float radius = _weakPoint.radius * maxScale;

            // 맞은 지점에서 멈추지 않고 계속 날아갔다고 보고, 약점 중심을 탄도선에 투영한 최근접점과 비교한다
            Vector3 forward = direction.Value.normalized;
            float along = Mathf.Max(0.0f, Vector3.Dot(center - hitPoint.Value, forward));
            Vector3 closest = hitPoint.Value + forward * along;

            return (center - closest).sqrMagnitude <= radius * radius;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Hp의 값이 0이하로 처음 떨어질 때 실행
        /// </summary>
        private void CheckDeath()
        {
            //if (_isDead) return;
            if (_health > 0.0f) return;

            //_isDead = true;
            OnDeath?.Invoke();
        }
        #endregion
    }
}
