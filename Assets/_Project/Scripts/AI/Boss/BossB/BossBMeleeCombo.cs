using UnityEngine;

namespace AI.Boss.BossB
{
    /// <summary>
    /// B 의 근접 콤보. 정해진 간격으로 2~3타를 치고, 마지막 타만 약하게 밀쳐 낸다.
    /// 각 타는 그 순간 대상이 범위 안일 때만 맞는다. 마지막 타 뒤 짧게 굳은 다음 끝난다.
    /// </summary>
    public class BossBMeleeCombo : BossBSkillBase
    {
        #region Serialized Fields
        [Header("Combo")]
        [SerializeField, Range(2, 3)] private int _hitCount = 3;
        [Tooltip("발동 후 첫 타까지(초)")]
        [SerializeField, Min(0.0f)] private float _firstHitDelay = 0.4f;
        [Tooltip("타 사이 간격(초)")]
        [SerializeField, Min(0.0f)] private float _hitInterval = 0.6f;
        [Tooltip("마지막 타 뒤 굳어 있는 시간(초)")]
        [SerializeField, Min(0.0f)] private float _recoverySeconds = 0.5f;

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
        private int _hitIndex;
        #endregion

        #region Protected Methods
        protected override void Execute()
        {
            if (!BeginRun(true, GetEndTime())) return;

            _elapsed = 0.0f;
            _hitIndex = 0;

            base.Execute();
        }

        protected override void Tick(float deltaTime)
        {
            _elapsed += deltaTime;

            if (_hitIndex < _hitCount)
            {
                if (_elapsed < GetHitTime(_hitIndex)) return;

                Strike(_hitIndex == (_hitCount - 1));
                _hitIndex++;
                return;
            }

            if (_elapsed >= GetEndTime()) EndRun();
        }
        #endregion

        #region Private Methods
        private void Strike(bool isLast)
        {
            // 타마다 대상 쪽으로 다시 돌아선다
            RefreshHold(GetEndTime() - _elapsed);

            Vector3 origin = Controller.Position;
            if (!TryHitTarget(origin, _range) || !isLast) return;

            KnockbackTarget(GetDirectionToTarget(), _lastKnockbackDistance, _lastKnockbackSeconds);
        }

        private float GetHitTime(int index)
        {
            return _firstHitDelay + (index * _hitInterval);
        }

        private float GetEndTime()
        {
            return GetHitTime(_hitCount - 1) + _recoverySeconds;
        }
        #endregion
    }
}
