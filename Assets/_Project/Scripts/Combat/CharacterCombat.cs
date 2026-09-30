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

                if (_isDead) return;

                if (isDamaged) OnHit?.Invoke();
                CheckDeath();
            }
        }

        public bool IsDead => _isDead;
        #endregion

        #region Events
        public event Action OnHit;
        public event Action OnDeath;
        #endregion

        #region Private Methods
        /// <summary>
        /// Hp의 값이 0이하로 처음 떨어질 때 실행
        /// </summary>
        private void CheckDeath()
        {
            if (_isDead) return;
            if (_health > 0.0f) return;

            _isDead = true;
            OnDeath?.Invoke();
        }
        #endregion
    }
}
