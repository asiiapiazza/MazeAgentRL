using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MazeGeneratorTraining))]
public class MazeGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MazeGeneratorTraining script = (MazeGeneratorTraining)target;
        if (GUILayout.Button("Generate maze"))
        {
            script.GenerateMazeInEditor();

            // Salva prefab dopo generazione
            string prefabFolder = "Assets/Prefab/GeneratedMazes";
            if (!AssetDatabase.IsValidFolder(prefabFolder))
                AssetDatabase.CreateFolder("Assets/Prefab", "GeneratedMazes");

            string prefabPath = prefabFolder + "/MazePrefab_" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(script.mazeParent, prefabPath);

            Debug.Log("Prefab saved in " + prefabPath);
        }
    }
}
