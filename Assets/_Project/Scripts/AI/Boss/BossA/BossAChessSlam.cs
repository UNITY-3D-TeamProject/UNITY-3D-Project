using UnityEngine;

namespace AI.Boss.BossA
{
    /// <summary>
    /// A5 체스판. 선택 결과의 +자 또는 X자 칸들을 예고하고, 손 칸 두 곳을 내려친 직후 어깨 포격을 쏜 뒤 예고한 칸이 함께 무너진다.
    /// 모양과 손 칸은 BossAPatternSelector 가 정한다. 최종 단계 전용이다.
    /// </summary>
    public class BossAChessSlam : BossAPatternBase
    {
        #region Serialized Fields
        [Tooltip("임시값. 스펙에 정해진 시간이 없어 A2·A4 와 같게 둔다")]
        [SerializeField, Min(0.0f)] private float _telegraphSeconds = 1.5f;
        #endregion

        #region Properties
        protected override float TelegraphSeconds => _telegraphSeconds;
        protected override bool UsesCannon => true;
        #endregion
    }
}
