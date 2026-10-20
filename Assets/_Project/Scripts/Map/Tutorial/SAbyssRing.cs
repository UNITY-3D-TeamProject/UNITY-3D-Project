using System;
using UnityEngine;

namespace Map.Tutorial
{
    /// <summary>
    /// 시스템 심연 3D 하늘의 고리 한 겹 설정. AbyssSkyRig 가 이 값으로 메시를 굽고 회전시킨다.
    /// 길이 단위는 m, 높이는 리그 중심 기준.
    /// </summary>
    [Serializable]
    public struct SAbyssRing
    {
        [Tooltip("고리 띠 가운데 반지름(m)")]
        public float Radius;

        [Tooltip("고리 띠 폭(m, 반지름 방향)")]
        public float Width;

        [Tooltip("리그 중심 기준 높이(m)")]
        public float Height;

        [Tooltip("기울기(도). x = X축, y = Z축")]
        public Vector2 Tilt;

        [Tooltip("모듈 블록 한 칸 길이(m, 둘레 방향)")]
        public float BlockLength;

        [Tooltip("블록이 빠지는 확률 (0~1)")]
        [Range(0.0f, 1.0f)] public float GapChance;

        [Tooltip("블록 두께 범위(m, 위아래)")]
        public Vector2 Thickness;

        [Tooltip("아래로 늘어지는 빛 기둥 다발 수")]
        public int PillarCount;

        [Tooltip("빛 기둥 최대 길이(m)")]
        public float PillarLength;

        [Tooltip("회전 속도(도/초). 음수면 반대로 돈다.")]
        public float DegreesPerSecond;
    }
}
