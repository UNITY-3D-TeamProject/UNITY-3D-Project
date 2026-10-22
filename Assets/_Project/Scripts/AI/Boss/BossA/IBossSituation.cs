using UnityEngine;
using Attribute.Effect;
using Movement;
using AI.Boss.Logic;

namespace AI.Boss.BossA
{
    /// <summary>
    /// 진행(BossEncounter)이 구현하고 A 가 읽는 상황 창구. A 는 플레이어와 B 를 직접 알지 못한다.
    /// </summary>
    public interface IBossSituation
    {
        /// <summary>패턴을 고를 때마다 새로 채운 상황 스냅샷.</summary>
        SBossSituation GetSnapshot();

        /// <summary>플레이어 몸 위치 기준 Transform.</summary>
        Transform PlayerBody { get; }

        /// <summary>넉백을 줄 플레이어 모터.</summary>
        CharacterMotor PlayerMotor { get; }

        /// <summary>플레이어에게 피해 Effect 를 적용한다 (cursor 는 호출 측이 아닌 진행이 정한다).</summary>
        /// <returns>적용했으면 true</returns>
        bool TryApplyToPlayer(SOAttributeEffect effect);

        /// <summary>플레이어의 이동·점프·구르기 입력을 잠그거나 푼다. 넉백은 잠긴 동안에도 적용된다.</summary>
        void SetPlayerMoveLocked(bool isLocked);

        /// <summary>플레이어를 붙잡힌 상태로 두거나 푼다. 붙잡힌 동안은 중력이 멈춰 PlayerMotor 외부 변위로만 움직인다.</summary>
        void SetPlayerHeld(bool isHeld);

        /// <summary>플레이어 카메라를 사격 쉐이크와 같은 임펄스로 흔든다.</summary>
        /// <param name="force">차지샷 쉐이크(1) 대비 세기</param>
        void ShakePlayerCamera(float force);
    }
}
