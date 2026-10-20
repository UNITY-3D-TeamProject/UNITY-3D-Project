using UnityEngine;
using UnityEngine.Serialization;

namespace AI.Boss.BossB
{
    /// <summary>
    /// B 의 근접 콤보 1타. 콤보는 이 스킬 여러 개(Combo1·Combo2·Combo3 ...)를 BT 가 이어서 부르는 것으로 만들고,
    /// 스킬마다 타 모션을 따로 재생한다. 마지막 타 스킬만 약하게 밀쳐 낸다.
    /// 타는 그 순간 대상이 범위 안일 때만 맞는다. 타 뒤 짧게 굳은 다음 끝난다.
    /// </summary>
    public class BossBMeleeCombo : BossBSkillBase
    {
        #region Serialized Fields
        [Header("Combo")]
        [Tooltip("발동 후 타까지(초)")]
        [SerializeField, Min(0.0f), FormerlySerializedAs("_firstHitDelay")] private float _hitDelay = 0.4f;
        [Tooltip("타 뒤 굳어 있는 시간(초)")]
        [SerializeField, Min(0.0f)] private float _recoverySeconds = 0.5f;
        [Tooltip("콤보의 마지막 타인지. 마지막 타만 넉백한다")]
        [SerializeField] private bool _isLastHit;

        [Header("Range")]
        [Tooltip("B 위치에서 대상까지 수평 거리가 이 안이면 맞는다(m). 임시값")]
        [SerializeField, Min(0.0f)] private float _range = 4.0f;

        [Header("Knockback")]
        [Tooltip("마지막 타에서 밀어내는 거리(m)")]
        [SerializeField, Min(0.0f)] private float _lastKnockbackDistance = 2.0f;
        [SerializeField, Min(0.05f)] private float _lastKnockbackSeconds = 0.2f;
        #endregion

        #region Private Fields
        private float _elapsed;
        private bool _isStruck;
        #endregion

        #region Protected Methods
        protected override void Execute()
        {
            if (!BeginRun(true, GetEndTime())) return;

            _elapsed = 0.0f;
            _isStruck = false;

            base.Execute();
        }

        protected override void Tick(float deltaTime)
        {
            _elapsed += deltaTime;

            if (!_isStruck)
            {
                if (_elapsed < _hitDelay) return;

                Strike();
                _isStruck = true;
                return;
            }

            if (_elapsed >= GetEndTime()) EndRun();
        }
        #endregion

        #region Private Methods
        private void Strike()
        {
            // 타 순간 대상 쪽으로 다시 돌아선다
            RefreshHold(GetEndTime() - _elapsed);

            Vector3 origin = Controller.Position;
            if (!TryHitTarget(origin, _range) || !_isLastHit) return;

            KnockbackTarget(GetDirectionToTarget(), _lastKnockbackDistance, _lastKnockbackSeconds);
        }

        private float GetEndTime()
        {
            return _hitDelay + _recoverySeconds;
        }
        #endregion
    }
}
