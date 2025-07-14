using UnityEditor;
using UnityEngine;

public class MultipleMazeSpawner : EditorWindow
{
    private const int MaxPrefabs = 10;

    private GameObject[] prefabs = new GameObject[MaxPrefabs];
    private int[] counts = new int[MaxPrefabs];

    private const string SpawnedPrefix = "MazeInstance_";

    [MenuItem("Tools/Multiple Prefab Spawner")]
    public static void ShowWindow()
    {
        GetWindow<MultipleMazeSpawner>("Prefab Spawner");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Prefab Spawner Tool", EditorStyles.boldLabel);

        for (int i = 0; i < MaxPrefabs; i++)
        {
            EditorGUILayout.BeginHorizontal();
            prefabs[i] = (GameObject)EditorGUILayout.ObjectField($"Prefab {i + 1}", prefabs[i], typeof(GameObject), false);
            counts[i] = EditorGUILayout.IntField("Quantity", counts[i]);
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Spawn all Prefabs"))
        {
            SpawnPrefabs();
        }

        if (GUILayout.Button("Delete all mazes"))
        {
            DeleteAllSpawnedPrefabs();
        }

        //aggiungi tasto per resettare tutti e anche i oggetti prefabbricati
        if (GUILayout.Button("Reset All"))
        {
            for (int i = 0; i < MaxPrefabs; i++)
            {
                prefabs[i] = null;
                counts[i] = 0;
            }
        }
    }

    private void SpawnPrefabs()
    {
        float xOffset = 0f;
        int counter = 0;

        for (int i = 0; i < MaxPrefabs; i++)
        {
            if (prefabs[i] == null || counts[i] <= 0) continue;

            for (int j = 0; j < counts[i]; j++)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[i]);
                if (instance != null)
                {
                    Undo.RegisterCreatedObjectUndo(instance, "Spawn Prefab");
                    instance.transform.position = new Vector3(xOffset, 0, 0);
                    instance.name = $"{SpawnedPrefix}{counter++}";


                    xOffset += 30f;
                }
            }
        }
    }

    private void DeleteAllSpawnedPrefabs()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.StartsWith(SpawnedPrefix))
            {
                Undo.DestroyObjectImmediate(obj);
            }
        }
    }
}
