using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DropProtocol.Editor
{
/// <summary>
///     Builds the grey-box tile prefabs and the tile set from <see cref="TileRecipes" />, so the set can be
///     regenerated whenever the edge rules or the tile size change. Floor, edge walls and gate stubs are
///     derived from the sockets; only cover and anchors are authored per tile.
/// </summary>
public static class TileFactory
{
    public const int TileSize = 20;
    private const string LevelFolder = "Assets/_Project/Prefabs/Level";
    private const string TileFolder = "Assets/_Project/Prefabs/Map/Tiles";
    private const string TileSetPath = "Assets/_Project/Settings/Missions/TileSet.asset";
    private const string FloorPiece = "FloorTile_4m";
    private const string WallPiece = "Wall_Station";
    private const int FloorModule = 4;
    private const int WallModule = 2;
    private const float WallInset = 0.3f;
    // An open edge keeps its middle 12 m clear; the 4 m stubs at both ends frame the gate.
    private const int GateHalfWidth = 6;
    private static readonly string[] SideNames = { "North", "East", "South", "West" };

    [MenuItem("DropProtocol/Map/Rebuild Tiles")]
    public static void RebuildTiles()
    {
        EnsureFolder("Assets/_Project/Prefabs", "Map");
        EnsureFolder("Assets/_Project/Prefabs/Map", "Tiles");
        var tiles = new List<MapTile>();
        foreach (TileRecipe recipe in TileRecipes.All)
        {
            tiles.Add(Build(recipe));
        }

        var set = AssetDatabase.LoadAssetAtPath<TileSet>(TileSetPath);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<TileSet>();
            AssetDatabase.CreateAsset(set, TileSetPath);
        }

        var serialized = new SerializedObject(set);
        SerializedProperty list = serialized.FindProperty("m_tiles");
        list.arraySize = tiles.Count;
        for (int i = 0; i < tiles.Count; i++)
        {
            list.GetArrayElementAtIndex(i).objectReferenceValue = tiles[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        Debug.Log($"Rebuilt {tiles.Count} tiles into {TileFolder} and {TileSetPath}");
    }

    private static MapTile Build(TileRecipe recipe)
    {
        var root = new GameObject(recipe.Name);
        try
        {
            var floorCollider = root.AddComponent<BoxCollider>();
            floorCollider.size = new Vector3(TileSize, 0.2f, TileSize);
            floorCollider.center = new Vector3(0f, -0.1f, 0f);

            GameObject floor = Child(root, "Floor");
            int half = TileSize / 2;
            for (int x = -half + FloorModule / 2; x < half; x += FloorModule)
            {
                for (int z = -half + FloorModule / 2; z < half; z += FloorModule)
                {
                    Place(FloorPiece, floor, new Vector3(x, 0f, z), 0);
                }
            }

            var edges = new GameObject[MapRules.SideCount];
            for (int side = 0; side < edges.Length; side++)
            {
                edges[side] = BuildEdge(root, side, recipe.Sockets[side]);
            }

            GameObject cover = Child(root, "Cover");
            foreach (CoverItem item in recipe.Cover)
            {
                Place(item.Prefab, cover, new Vector3(item.X, 0f, item.Z), item.QuarterTurns);
            }

            var tile = root.AddComponent<MapTile>();
            tile.Configure(recipe.Sockets, recipe.Sites, recipe.EnemySpawns, edges, TileSize);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{TileFolder}/{recipe.Name}.prefab");
            return prefab.GetComponent<MapTile>();
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static GameObject BuildEdge(GameObject root, int side, MapSocket socket)
    {
        GameObject edge = Child(root, "Edge_" + SideNames[side]);
        float offset = TileSize * 0.5f - WallInset;
        int quarterTurns = side == MapRules.East || side == MapRules.West ? 1 : 0;
        for (int t = -TileSize / 2 + WallModule / 2; t < TileSize / 2; t += WallModule)
        {
            if (socket == MapSocket.Open && Mathf.Abs(t) <= GateHalfWidth)
            {
                continue;
            }

            Vector3 local;
            switch (side)
            {
                case MapRules.North: local = new Vector3(t, 0f, offset); break;
                case MapRules.South: local = new Vector3(t, 0f, -offset); break;
                case MapRules.East: local = new Vector3(offset, 0f, t); break;
                default: local = new Vector3(-offset, 0f, t); break;
            }

            Place(WallPiece, edge, local, quarterTurns);
        }

        return edge;
    }

    private static void Place(string prefabName, GameObject parent, Vector3 localPosition, int quarterTurns)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>($"{LevelFolder}/{prefabName}.prefab");
        if (source == null)
        {
            throw new System.InvalidOperationException($"Level prefab {prefabName} not found.");
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, parent.transform);
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = MapRules.Rotation(quarterTurns);
    }

    private static GameObject Child(GameObject parent, string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    private static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
}
