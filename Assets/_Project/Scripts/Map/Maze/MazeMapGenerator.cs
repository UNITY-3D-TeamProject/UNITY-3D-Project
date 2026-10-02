using System.Collections.Generic;
using UnityEngine;
using Map.Common;

namespace Map.Maze
{
    /// <summary>
    /// 타일 프리팹을 이어 붙여 미로를 런타임에 생성한다.
    /// 시작 칸은 가장자리에서 무작위로 고르고, 도착 칸은 시작 칸에서 가장 먼 칸으로 정한다.
    /// 타일 프리팹은 "북쪽(+Z)이 열린 상태"를 기본 방향으로 만든다.
    /// </summary>
    public class MazeMapGenerator : MapGeneratorBase
    {
        #region Constants
        private const float START_HEIGHT_OFFSET = 0.1f;
        #endregion

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

        #region Protected Methods
        /// <inheritdoc />
        protected override void Build(
            Transform root,
            System.Random random,
            List<Vector3> placementPoints,
            out Vector3 startPosition,
            out Vector3 goalPosition)
        {
            startPosition = transform.position;
            goalPosition = transform.position;

            bool hasAllPrefabs = (_deadEndPrefab != null) && (_straightPrefab != null) && (_cornerPrefab != null) &&
                                 (_tJunctionPrefab != null) && (_crossPrefab != null);
            if (!hasAllPrefabs)
            {
                Debug.LogError($"[{name}] 미로 타일 프리팹 5종이 모두 연결되어야 합니다.", this);
                return;
            }

            var generator = new MazeGenerator();
            Vector2Int startCell = PickEdgeCell(random);
            MazeGenerator.SCell[,] grid = generator.GenerateMaze(_width, _height, startCell, random);
            Vector2Int goalCell = FindFarthestCell(generator.ComputeDistances(grid, startCell));

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    CreateTile(grid[x, y], root);

                    // 시작·도착 칸에는 장애물이나 아이템을 놓지 않는다
                    var cell = new Vector2Int(x, y);
                    if ((cell != startCell) && (cell != goalCell))
                    {
                        placementPoints.Add(GetCellCenter(root, cell));
                    }
                }
            }

            startPosition = GetCellCenter(root, startCell) + (Vector3.up * START_HEIGHT_OFFSET);
            goalPosition = GetCellCenter(root, goalCell);
        }
        #endregion

        #region Private Methods
        private Vector2Int PickEdgeCell(System.Random random)
        {
            switch (random.Next(4))
            {
                case 0:  return new Vector2Int(random.Next(_width), 0);             // 아래
                case 1:  return new Vector2Int(random.Next(_width), _height - 1);   // 위
                case 2:  return new Vector2Int(0, random.Next(_height));            // 왼쪽
                default: return new Vector2Int(_width - 1, random.Next(_height));   // 오른쪽
            }
        }

        private Vector2Int FindFarthestCell(int[,] distances)
        {
            Vector2Int farthest = Vector2Int.zero;
            int maxDistance = -1;

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (distances[x, y] > maxDistance)
                    {
                        maxDistance = distances[x, y];
                        farthest = new Vector2Int(x, y);
                    }
                }
            }

            return farthest;
        }

        private Vector3 GetCellCenter(Transform root, Vector2Int cell)
        {
            return root.TransformPoint(new Vector3(cell.x * _tileSize, 0.0f, cell.y * _tileSize));
        }

        private void CreateTile(MazeGenerator.SCell cell, Transform root)
        {
            SelectTile(cell, out GameObject prefab, out float rotationY);
            if (prefab == null) return;

            GameObject tile = Instantiate(prefab, root);
            tile.name = $"Tile_{cell.X}_{cell.Y}";
            tile.transform.localPosition = new Vector3(cell.X * _tileSize, 0.0f, cell.Y * _tileSize);
            tile.transform.localRotation = Quaternion.Euler(0.0f, rotationY, 0.0f);
        }

        /// <summary>
        /// 칸의 열린 방향에 맞는 타일 프리팹과, 기본 방향에서 돌려야 할 각도를 고른다.
        /// </summary>
        private void SelectTile(MazeGenerator.SCell cell, out GameObject prefab, out float rotationY)
        {
            prefab = null;
            rotationY = 0.0f;

            int openCount = 0;
            if (cell.OpenNorth) openCount++;
            if (cell.OpenEast) openCount++;
            if (cell.OpenSouth) openCount++;
            if (cell.OpenWest) openCount++;

            switch (openCount)
            {
                case 1:
                    prefab = _deadEndPrefab;
                    if (cell.OpenEast) rotationY = 90.0f;
                    else if (cell.OpenSouth) rotationY = 180.0f;
                    else if (cell.OpenWest) rotationY = 270.0f;
                    break;

                case 2:
                    if (cell.OpenNorth && cell.OpenSouth)
                    {
                        prefab = _straightPrefab;
                    }
                    else if (cell.OpenEast && cell.OpenWest)
                    {
                        prefab = _straightPrefab;
                        rotationY = 90.0f;
                    }
                    else
                    {
                        prefab = _cornerPrefab;
                        if (cell.OpenEast && cell.OpenSouth) rotationY = 90.0f;
                        else if (cell.OpenSouth && cell.OpenWest) rotationY = 180.0f;
                        else if (cell.OpenWest && cell.OpenNorth) rotationY = 270.0f;
                    }
                    break;

                case 3:
                    prefab = _tJunctionPrefab;
                    if (!cell.OpenNorth) rotationY = 90.0f;        // 동·남·서
                    else if (!cell.OpenEast) rotationY = 180.0f;   // 남·서·북
                    else if (!cell.OpenSouth) rotationY = 270.0f;  // 서·북·동
                    break;

                case 4:
                    prefab = _crossPrefab;
                    break;
            }
        }
        #endregion
    }
}
