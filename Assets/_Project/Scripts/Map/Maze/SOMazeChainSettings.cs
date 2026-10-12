using System.Collections.Generic;
using UnityEngine;
using Map.Common;

namespace Map.Maze
{
    /// <summary>
    /// MazeChainGenerator 의 설정 에셋. 미로 크기, 미로별 겉모습, 바닥·다리 프리팹을 담는다.
    /// </summary>
    [CreateAssetMenu(fileName = "SOMazeChainSettings", menuName = "Map/Maze Chain Settings")]
    public class SOMazeChainSettings : SOMapSettings
    {
        #region Serialized Fields
        [Header("Maze")]
        [Tooltip("미로 한 변의 칸 수")]
        [SerializeField, Range(2, 50)] private int _size = 15;

        [Tooltip("한 칸의 크기. 벽·출구·다리 프리팹의 크기와 맞아야 한다.")]
        [SerializeField, Min(0.1f)] private float _tileSize = 5.0f;

        [Tooltip("벽 두께. 바닥이 바깥 벽 밑까지 깔리도록 이만큼 더 넓게 만든다.")]
        [SerializeField, Min(0.0f)] private float _wallThickness = 0.5f;

        [Header("Chain")]
        [Tooltip("미로별 겉모습. 여기 넣은 개수만큼 미로가 이어진다.")]
        [SerializeField] private SMazeTheme[] _themes;

        [Tooltip("미로 사이 다리의 길이(칸 수). 다리 프리팹 하나가 한 칸이다.")]
        [SerializeField, Min(1)] private int _bridgeTileCount = 4;

        [Header("Prefabs")]
        [Tooltip("바닥 프리팹. 1 x 1 x 1 크기의 블록이며 미로 크기에 맞춰 늘린다.")]
        [SerializeField] private GameObject _floorPrefab;

        [Tooltip("다리 한 칸 프리팹. 길이 방향이 Z축, 윗면이 y = 0")]
        [SerializeField] private GameObject _bridgeSegmentPrefab;

        [Tooltip("입구 칸 표시 프리팹 (선택, 테스트용)")]
        [SerializeField] private GameObject _startMarkerPrefab;
        #endregion

        #region Properties
        /// <summary>미로 한 변의 칸 수.</summary>
        public int Size => _size;

        /// <summary>한 칸의 크기.</summary>
        public float TileSize => _tileSize;

        /// <summary>벽 두께.</summary>
        public float WallThickness => _wallThickness;

        /// <summary>미로별 겉모습. 개수만큼 미로가 이어진다.</summary>
        public IReadOnlyList<SMazeTheme> Themes => _themes;

        /// <summary>미로 사이 다리의 길이(칸 수).</summary>
        public int BridgeTileCount => _bridgeTileCount;

        /// <summary>바닥 프리팹.</summary>
        public GameObject FloorPrefab => _floorPrefab;

        /// <summary>다리 한 칸 프리팹.</summary>
        public GameObject BridgeSegmentPrefab => _bridgeSegmentPrefab;

        /// <summary>입구 칸 표시 프리팹.</summary>
        public GameObject StartMarkerPrefab => _startMarkerPrefab;
        #endregion
    }
}
