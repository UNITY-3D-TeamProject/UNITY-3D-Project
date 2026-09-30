using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Map.Maze;

namespace Map.Editor
{
    public class MazeEditorWindow : EditorWindow
    {
        private int _width = 10;
        private int _height = 10;
        private float _tileSize = 10f;
        
        private bool _randomizeStartEnd = true;
        private Vector2Int _startPos = new Vector2Int(0, 0);
        private Vector2Int _endPos = new Vector2Int(9, 9);

        private GameObject _deadEndPrefab;    // Open: North
        private GameObject _straightPrefab;   // Open: North, South
        private GameObject _cornerPrefab;     // Open: North, East
        private GameObject _tJunctionPrefab;  // Open: North, East, South
        private GameObject _crossPrefab;      // Open: All

        private Material _pathMaterial; // Optional material to highlight A* path

        private GameObject _mapRoot;
        private MazeGenerator _generator;

        [MenuItem("Project/Map/Maze Editor")]
        public static void ShowWindow()
        {
            GetWindow<MazeEditorWindow>("Maze Editor");
        }

        private void OnEnable()
        {
            _generator = new MazeGenerator();
        }

        private void OnGUI()
        {
            GUILayout.Label("Map Settings", EditorStyles.boldLabel);
            _width = EditorGUILayout.IntSlider("Width", _width, 2, 50);
            _height = EditorGUILayout.IntSlider("Height", _height, 2, 50);
            _tileSize = EditorGUILayout.FloatField("Tile Size", _tileSize);

            GUILayout.Space(10);
            GUILayout.Label("Start & End Settings", EditorStyles.boldLabel);
            _randomizeStartEnd = EditorGUILayout.Toggle("Randomize Start/End on Edges", _randomizeStartEnd);
            
            if (!_randomizeStartEnd)
            {
                _startPos.x = EditorGUILayout.IntSlider("Start X", _startPos.x, 0, _width - 1);
                _startPos.y = EditorGUILayout.IntSlider("Start Y", _startPos.y, 0, _height - 1);
                
                _endPos.x = EditorGUILayout.IntSlider("End X", _endPos.x, 0, _width - 1);
                _endPos.y = EditorGUILayout.IntSlider("End Y", _endPos.y, 0, _height - 1);
            }

            GUILayout.Space(10);
            GUILayout.Label("Prefabs (Require specific default orientations)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("DeadEnd: Open North\nStraight: Open North-South\nCorner: Open North-East\nTJunction: Open North-East-South\nCross: Open All", MessageType.Info);
            
            _deadEndPrefab = (GameObject)EditorGUILayout.ObjectField("Dead End (1)", _deadEndPrefab, typeof(GameObject), false);
            _straightPrefab = (GameObject)EditorGUILayout.ObjectField("Straight (2)", _straightPrefab, typeof(GameObject), false);
            _cornerPrefab = (GameObject)EditorGUILayout.ObjectField("Corner (2)", _cornerPrefab, typeof(GameObject), false);
            _tJunctionPrefab = (GameObject)EditorGUILayout.ObjectField("T-Junction (3)", _tJunctionPrefab, typeof(GameObject), false);
            _crossPrefab = (GameObject)EditorGUILayout.ObjectField("Cross (4)", _crossPrefab, typeof(GameObject), false);

            GUILayout.Space(10);
            _pathMaterial = (Material)EditorGUILayout.ObjectField("Path Highlight Material (Optional)", _pathMaterial, typeof(Material), false);

            GUILayout.Space(20);
            if (GUILayout.Button("Generate Maze", GUILayout.Height(40)))
            {
                Generate();
            }
        }

        private void Generate()
        {
            if (_deadEndPrefab == null || _straightPrefab == null || _cornerPrefab == null || _tJunctionPrefab == null || _crossPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign all prefabs before generating.", "OK");
                return;
            }

            if (_mapRoot != null)
            {
                DestroyImmediate(_mapRoot);
            }

            _mapRoot = new GameObject("GeneratedMaze");

            if (_randomizeStartEnd)
            {
                _startPos = GetRandomEdgePosition();
                do
                {
                    _endPos = GetRandomEdgePosition();
                } while (_startPos == _endPos);
            }

            var grid = _generator.GenerateMaze(_width, _height, _startPos);
            var path = _generator.FindPathAStar(grid, _startPos, _endPos);

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    InstantiateTile(grid[x, y], path);
                }
            }
            
            Selection.activeGameObject = _mapRoot;
            Debug.Log("Maze Generated Successfully.");
        }

        private Vector2Int GetRandomEdgePosition()
        {
            int edge = Random.Range(0, 4);
            switch (edge)
            {
                case 0: return new Vector2Int(Random.Range(0, _width), 0); // Bottom
                case 1: return new Vector2Int(Random.Range(0, _width), _height - 1); // Top
                case 2: return new Vector2Int(0, Random.Range(0, _height)); // Left
                case 3: return new Vector2Int(_width - 1, Random.Range(0, _height)); // Right
            }
            return Vector2Int.zero;
        }

        private void InstantiateTile(MazeGenerator.SCell cell, List<Vector2Int> path)
        {
            GameObject prefabToSpawn = null;
            float rotationY = 0f;

            int openCount = 0;
            if (cell.OpenNorth) openCount++;
            if (cell.OpenEast) openCount++;
            if (cell.OpenSouth) openCount++;
            if (cell.OpenWest) openCount++;

            switch (openCount)
            {
                case 1:
                    prefabToSpawn = _deadEndPrefab;
                    if (cell.OpenNorth) rotationY = 0f;
                    else if (cell.OpenEast) rotationY = 90f;
                    else if (cell.OpenSouth) rotationY = 180f;
                    else if (cell.OpenWest) rotationY = 270f;
                    break;
                case 2:
                    if (cell.OpenNorth && cell.OpenSouth)
                    {
                        prefabToSpawn = _straightPrefab;
                        rotationY = 0f;
                    }
                    else if (cell.OpenEast && cell.OpenWest)
                    {
                        prefabToSpawn = _straightPrefab;
                        rotationY = 90f;
                    }
                    else
                    {
                        prefabToSpawn = _cornerPrefab;
                        if (cell.OpenNorth && cell.OpenEast) rotationY = 0f;
                        else if (cell.OpenEast && cell.OpenSouth) rotationY = 90f;
                        else if (cell.OpenSouth && cell.OpenWest) rotationY = 180f;
                        else if (cell.OpenWest && cell.OpenNorth) rotationY = 270f;
                    }
                    break;
                case 3:
                    prefabToSpawn = _tJunctionPrefab;
                    if (cell.OpenNorth && cell.OpenEast && cell.OpenSouth) rotationY = 0f;
                    else if (cell.OpenEast && cell.OpenSouth && cell.OpenWest) rotationY = 90f;
                    else if (cell.OpenSouth && cell.OpenWest && cell.OpenNorth) rotationY = 180f;
                    else if (cell.OpenWest && cell.OpenNorth && cell.OpenEast) rotationY = 270f;
                    break;
                case 4:
                    prefabToSpawn = _crossPrefab;
                    rotationY = 0f;
                    break;
            }

            if (prefabToSpawn != null)
            {
                Vector3 position = new Vector3(cell.X * _tileSize, 0, cell.Y * _tileSize);
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabToSpawn, _mapRoot.transform);
                instance.transform.position = position;
                instance.transform.rotation = Quaternion.Euler(0, rotationY, 0);
                instance.name = $"Tile_{cell.X}_{cell.Y}";

                // Highlight path
                if (_pathMaterial != null && path != null && path.Contains(new Vector2Int(cell.X, cell.Y)))
                {
                    // Create a marker or override material
                    GameObject pathMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    pathMarker.transform.position = position + Vector3.up * 1.5f;
                    pathMarker.transform.SetParent(instance.transform);
                    pathMarker.transform.localScale = Vector3.one * (_tileSize * 0.2f);
                    if (pathMarker.TryGetComponent<Renderer>(out var rend))
                    {
                        rend.sharedMaterial = _pathMaterial;
                    }
                    pathMarker.name = "PathMarker";
                }
            }
        }
    }
}
