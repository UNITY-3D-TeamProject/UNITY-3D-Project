using System;
using UnityEngine;

namespace Tutorial
{
    /// <summary>
    /// PlatformSpawner 가 발판 사이 간격과 높낮이를 정할 때 쓰는 난이도 단계 하나.
    /// 간격은 발판 가장자리 사이의 수평 거리이므로 발판끼리 겹치거나 붙어 나오지 않는다.
    /// </summary>
    [Serializable]
    public struct SPlatformGapTier
    {
        [Tooltip("인스펙터 구분용 이름 (예: 점프, 2단 점프, 2단 점프 + 대시)")]
        public string Name;

        [Tooltip("이 단계로 연달아 생성할 발판 수")]
        [Min(1)] public int Count;

        [Tooltip("직전 발판과의 최소 가장자리 간격 (수평)")]
        [Min(0.0f)] public float MinGap;

        [Tooltip("직전 발판과의 최대 가장자리 간격 (수평). 좌우로 벌어진 거리도 포함한다.")]
        [Min(0.0f)] public float MaxGap;

        [Tooltip("직전 발판보다 높아질 수 있는 최대 높이")]
        [Min(0.0f)] public float MaxRise;

        [Tooltip("직전 발판보다 낮아질 수 있는 최대 높이")]
        [Min(0.0f)] public float MaxDrop;

        [Tooltip("직전 발판에서 좌우로 옮겨 갈 수 있는 최대 거리 (발판 중심 기준)")]
        [Min(0.0f)] public float MaxLateralShift;

        public SPlatformGapTier(
            string name,
            int count,
            float minGap,
            float maxGap,
            float maxRise,
            float maxDrop,
            float maxLateralShift)
        {
            Name = name;
            Count = count;
            MinGap = minGap;
            MaxGap = maxGap;
            MaxRise = maxRise;
            MaxDrop = maxDrop;
            MaxLateralShift = maxLateralShift;
        }
    }
}
