namespace AI.Boss.Logic
{
    /// <summary>
    /// B 의 공격 선택 규칙. Unity 를 참조하지 않는다.
    /// </summary>
    public static class BossBAttackRule
    {
        #region Public Methods
        /// <summary>
        /// 가까우면 콤보. 멀면 사이에 구멍이 있을 때 도약을 먼저 고르고, 구멍이 없을 때만 돌진한다.
        /// 구멍이 있는데 도약을 못 쓰면 돌진하지 않는다(급정거는 돌진 중에 구멍이 생겼을 때만 일어나야 한다).
        /// </summary>
        /// <param name="distance">B 와 플레이어 사이 거리</param>
        /// <param name="comboRange">콤보를 쓰는 최대 거리</param>
        /// <param name="isChargeReady">돌진 쿨타임이 끝났는지</param>
        /// <param name="isLeapReady">도약 쿨타임이 끝났는지</param>
        /// <param name="isLeapOpen">도약이 열린 단계인지(1-2 부터)</param>
        /// <param name="hasHoleBetween">B 와 플레이어 사이에 구멍이 있는지</param>
        /// <returns>고른 공격. 없으면 None(추격)</returns>
        public static EBossBAttack Choose(
            float distance,
            float comboRange,
            bool isChargeReady,
            bool isLeapReady,
            bool isLeapOpen,
            bool hasHoleBetween)
        {
            if (distance <= comboRange) return EBossBAttack.Combo;

            bool canLeap = isLeapOpen && isLeapReady;
            if (hasHoleBetween && canLeap) return EBossBAttack.Leap;
            if (isChargeReady && !hasHoleBetween) return EBossBAttack.Charge;
            if (canLeap) return EBossBAttack.Leap;

            return EBossBAttack.None;
        }
        #endregion
    }
}
