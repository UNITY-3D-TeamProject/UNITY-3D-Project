using UnityEngine;

namespace AI.Boss.BossA
{
    /// <summary>
    /// A1·A1Double. 선택 결과의 칸을 내려쳐 피해를 주고 무너뜨린다. 한 손이면 A1, 양손이면 A1Double 이며 스킬 이름으로 구분한다.
    /// A1Double(_isDouble)은 최종 단계 전용이라 양손이 서로 다른 칸을 친 직후 어깨 포격이 붙는다.
    /// </summary>
    public class BossASlam : BossAPatternBase
    {
        #region Serialized Fields
        [SerializeField, Min(0.0f)] private float _telegraphSeconds = 1.2f;
        [Tooltip("A1Double 이면 켠다. 양손이 친 직후 어깨 포격을 쏜다")]
        [SerializeField] private bool _isDouble;
        #endregion

        #region Properties
        protected override float TelegraphSeconds => _telegraphSeconds;
        protected override bool UsesCannon => _isDouble;
        #endregion
    }
}
