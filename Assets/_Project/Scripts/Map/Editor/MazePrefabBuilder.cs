using UnityEditor;
using UnityEngine;
using System.IO;

namespace Map.Editor
{
    public static class MazePrefabBuilder
    {
        private const string PREFAB_DIR = "Assets/_Project/Prefabs/Map/Maze";
        private const float TILE_SIZE = 10f;
        private const float WALL_HEIGHT = 2f;
        private const float WALL_THICKNESS = 1f;

        [MenuItem("Project/Map/Generate Maze Prefabs")]
        public static void GenerateDefaultPrefabs()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
                AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
                
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/Map"))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Map");

            if (!AssetDatabase.IsValidFolder(PREFAB_DIR))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs/Map", "Maze");

            CreatePrefab("DeadEnd", false, true, true, true);         // Open: North
            CreatePrefab("Straight", false, true, false, true);       // Open: North, South
            CreatePrefab("Corner", false, false, true, true);         // Open: North, East
            CreatePrefab("TJunction", false, false, false, true);     // Open: North, East, South
            CreatePrefab("Cross", false, false, false, false);        // Open: All

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"[Map] 5 Default Prefabs have been generated at: {PREFAB_DIR}");
        }

        private static void CreatePrefab(string name, bool hasNorth, bool hasEast, bool hasSouth, bool hasWest)
        {
            GameObject root = new GameObject(name);
            
            // Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(root.transform);
            floor.transform.localPosition = new Vector3(0, -0.5f, 0);
            floor.transform.localScale = new Vector3(TILE_SIZE, 1, TILE_SIZE);

            // Walls
            if (hasNorth) CreateWall(root, "Wall_North", new Vector3(0, WALL_HEIGHT / 2f, TILE_SIZE / 2f - WALL_THICKNESS / 2f), new Vector3(TILE_SIZE, WALL_HEIGHT, WALL_THICKNESS));
            if (hasSouth) CreateWall(root, "Wall_South", new Vector3(0, WALL_HEIGHT / 2f, -TILE_SIZE / 2f + WALL_THICKNESS / 2f), new Vector3(TILE_SIZE, WALL_HEIGHT, WALL_THICKNESS));
            if (hasEast)  CreateWall(root, "Wall_East",  new Vector3(TILE_SIZE / 2f - WALL_THICKNESS / 2f, WALL_HEIGHT / 2f, 0), new Vector3(WALL_THICKNESS, WALL_HEIGHT, TILE_SIZE));
            if (hasWest)  CreateWall(root, "Wall_West",  new Vector3(-TILE_SIZE / 2f + WALL_THICKNESS / 2f, WALL_HEIGHT / 2f, 0), new Vector3(WALL_THICKNESS, WALL_HEIGHT, TILE_SIZE));

            // Corner pillars for Cross (optional, but looks better)
            if (!hasNorth && !hasEast && !hasSouth && !hasWest)
            {
                float offset = TILE_SIZE / 2f - WALL_THICKNESS / 2f;
                CreateWall(root, "Pillar_NE", new Vector3(offset, WALL_HEIGHT / 2f, offset), new Vector3(WALL_THICKNESS, WALL_HEIGHT, WALL_THICKNESS));
                CreateWall(root, "Pillar_NW", new Vector3(-offset, WALL_HEIGHT / 2f, offset), new Vector3(WALL_THICKNESS, WALL_HEIGHT, WALL_THICKNESS));
                CreateWall(root, "Pillar_SE", new Vector3(offset, WALL_HEIGHT / 2f, -offset), new Vector3(WALL_THICKNESS, WALL_HEIGHT, WALL_THICKNESS));
                CreateWall(root, "Pillar_SW", new Vector3(-offset, WALL_HEIGHT / 2f, -offset), new Vector3(WALL_THICKNESS, WALL_HEIGHT, WALL_THICKNESS));
            }

            string path = $"{PREFAB_DIR}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            GameObject.DestroyImmediate(root);
        }

        private static void CreateWall(GameObject parent, string name, Vector3 localPos, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent.transform);
            wall.transform.localPosition = localPos;
            wall.transform.localScale = scale;
        }
    }
}
