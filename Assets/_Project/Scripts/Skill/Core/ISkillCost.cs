using System;

namespace Skill.Core
{
    /// <summary>
    /// 스킬 사용 코스트. 스킬과 같은 GameObject 에 붙이면 SkillBase 가 자동으로 참조한다.
    /// </summary>
    public interface ISkillCost
    {
        /// <summary>코스트를 위한 값을 참조하기 위한 함수 주입</summary>
        void SetGetAttribute(Func<string, float> getAttribute);

        /// <summary>코스트 지불을 요청하기 위한 함수 주입. (키, 소모량)을 받아 지불 성공 여부를 반환한다.</summary>
        void SetRequestPay(Func<string, float, bool> requestPay);

        /// <summary>코스트를 지불할 수 있는지 여부.</summary>
        bool CanPay();

        /// <summary>코스트를 지불한다. 스킬 발동 직전에 호출된다.</summary>
        /// <returns>지불에 성공했으면 true</returns>
        bool Pay();
    }
}
