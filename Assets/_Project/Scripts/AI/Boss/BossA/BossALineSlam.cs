using UnityEngine;

namespace AI.Boss.BossA
{
    /// <summary>
    /// A2. 한 줄 전체에 예고를 띄우고 양 끝 칸을 양손으로 내려친다. 나머지 칸은 손 없이 함께 무너진다.
    /// </summary>
    public class BossALineSlam : BossAPatternBase
    {
        #region Serialized Fields
        [SerializeField, Min(0.0f)] private float _telegraphSeconds = 1.5f;
        #endregion

        #region Properties
        protected override float TelegraphSeconds => _telegraphSeconds;
        #endregion
    }
}
