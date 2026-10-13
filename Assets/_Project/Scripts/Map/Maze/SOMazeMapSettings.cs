using UnityEngine;
using Map.Common;

namespace Map.Maze
{
    /// <summary>
    /// MazeMapGenerator 의 설정 에셋. 미로 크기와 타일 프리팹 5종을 담는다.
    /// 타일 프리팹은 "북쪽(+Z)이 열린 상태"를 기본 방향으로 만든다.
    /// </summary>
    [CreateAssetMenu(fileName = "SOMazeMapSettings", menuName = "Map/Maze Map Settings")]
    public class SOMazeMapSettings : SOMapSettings
    {
        #region Serialized Fields
        [Header("Maze")]
        [Tooltip("가로 칸 수")]
        [SerializeField, Range(2, 50)] private int _width = 8;

        [Tooltip("세로 칸 수")]
        [SerializeField, Range(2, 50)] private int _height = 8;

        [Tooltip("타일 한 칸의 크기. 타일 프리팹의 크기와 같아야 한다.")]
        [SerializeField, Min(0.1f)] private float _tileSize = 10.0f;

        [Header("Tiles (기본 방향: 북쪽이 열림)")]
        [Tooltip("막다른 길: 북쪽만 열림")]
        [SerializeField] private GameObject _deadEndPrefab;

        [Tooltip("직선: 북·남이 열림")]
        [SerializeField] private GameObject _straightPrefab;

        [Tooltip("모퉁이: 북·동이 열림")]
        [SerializeField] private GameObject _cornerPrefab;

        [Tooltip("세 갈래: 북·동·남이 열림")]
        [SerializeField] private GameObject _tJunctionPrefab;

        [Tooltip("네 갈래: 모두 열림")]
        [SerializeField] private GameObject _crossPrefab;
        #endregion

        #region Properties
        /// <summary>가로 칸 수.</summary>
        public int Width => _width;

        /// <summary>세로 칸 수.</summary>
        public int Height => _height;

        /// <summary>타일 한 칸의 크기.</summary>
        public float TileSize => _tileSize;

        /// <summary>막다른 길 타일.</summary>
        public GameObject DeadEndPrefab => _deadEndPrefab;

        /// <summary>직선 타일.</summary>
        public GameObject StraightPrefab => _straightPrefab;

        /// <summary>모퉁이 타일.</summary>
        public GameObject CornerPrefab => _cornerPrefab;

        /// <summary>세 갈래 타일.</summary>
        public GameObject TJunctionPrefab => _tJunctionPrefab;

        /// <summary>네 갈래 타일.</summary>
        public GameObject CrossPrefab => _crossPrefab;
        #endregion
    }
}
