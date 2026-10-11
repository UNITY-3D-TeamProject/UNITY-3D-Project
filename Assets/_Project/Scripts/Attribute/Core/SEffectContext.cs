using UnityEngine;
using Attribute.Effect;

namespace Attribute.Core
{
    /// <summary>
    /// 효과 적용의 출처 정보. SOAttributeEffect.Apply 에서 어트리뷰트 변경 콜백까지 함께 전달된다.
    /// default 는 출처 없음(코스트 지불, 값 복원 등)을 뜻한다.
    /// </summary>
    public readonly struct SEffectContext
    {
        /// <summary>효과를 발생시킨 주체. null 이면 주체 없음.</summary>
        public readonly IEffectTarget Cursor;

        /// <summary>효과가 시작된 월드 위치. null 이면 위치 없음(독, 회복 등).</summary>
        public readonly Vector3? Origin;

        /// <summary>값을 바꾼 효과 에셋. SOAttributeEffect.Apply 가 채운다. null 이면 효과 없이 바뀐 값(코스트 지불, 값 복원 등).</summary>
        public readonly SOAttributeEffect Effect;

        /// <param name="cursor">효과를 발생시킨 주체</param>
        /// <param name="origin">효과가 시작된 월드 위치</param>
        /// <param name="effect">값을 바꾼 효과 에셋. 호출부는 넣지 않는다(Apply 가 덮어씀)</param>
        public SEffectContext(IEffectTarget cursor, Vector3? origin = null, SOAttributeEffect effect = null)
        {
            Cursor = cursor;
            Origin = origin;
            Effect = effect;
        }
    }
}
