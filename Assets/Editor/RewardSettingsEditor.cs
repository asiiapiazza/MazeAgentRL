#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CubeTraining))]
public class RewardSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        CubeTraining agent = (CubeTraining)target;


        EditorGUILayout.LabelField("📦 Agent Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        agent.MaxStep = EditorGUILayout.IntField("Max Steps for training", agent.MaxStep);
        agent.moveSpeed = EditorGUILayout.FloatField("Move Speed", agent.moveSpeed);
        agent.canAgentJump = EditorGUILayout.Toggle("Allow agent to jump", agent.canAgentJump);
        agent.timeScale = EditorGUILayout.IntField("Time Scale", agent.timeScale);
        agent.numberOfObstacles = EditorGUILayout.IntField("Number of Obstacles", agent.numberOfObstacles);

        EditorGUI.indentLevel--;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🧱 Environment References", EditorStyles.boldLabel);

        EditorGUI.indentLevel++;

        agent.floor = (GameObject)EditorGUILayout.ObjectField("Floor Parent", agent.floor, typeof(GameObject), true);
        agent.wall = (GameObject)EditorGUILayout.ObjectField("Wall Parent", agent.wall, typeof(GameObject), true);
        agent.obstaclePrefab = (GameObject)EditorGUILayout.ObjectField("Obstacle Prefab", agent.obstaclePrefab, typeof(GameObject), false);

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🏆 Reward Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        var settings = agent.rewardSettings;
        settings.fallingInVoid = EditorGUILayout.FloatField("Falling in Void", settings.fallingInVoid);
        settings.inAirNotOnGround = EditorGUILayout.FloatField("In Air Not on Ground", settings.inAirNotOnGround);
        settings.oneJump = EditorGUILayout.FloatField("One Jump", settings.oneJump);
        settings.notStraightMovement = EditorGUILayout.FloatField("Not Straight Movement", settings.notStraightMovement);
        settings.eachStep = EditorGUILayout.FloatField("Each Step", settings.eachStep);
        settings.exploreNotVisitedCell = EditorGUILayout.FloatField("Explore Not Visited", settings.exploreNotVisitedCell);
        settings.exploreVisitedCell = EditorGUILayout.FloatField("Explore Visited Cell", settings.exploreVisitedCell);
        settings.hitExit = EditorGUILayout.FloatField("Hit Exit", settings.hitExit);
        settings.hitWall = EditorGUILayout.FloatField("Hit Wall", settings.hitWall);
        settings.jumpedOverObstacle = EditorGUILayout.FloatField("Jumped Over Obstacle", settings.jumpedOverObstacle);
        settings.hitObstacle = EditorGUILayout.FloatField("Hit Obstacle", settings.hitObstacle);

        EditorGUI.indentLevel--;


        if (GUILayout.Button("🔄 Reset Rewards to Defaults"))
        {
            Undo.RecordObject(agent, "Reset Reward Settings");
            settings.ResetToDefaults();
            EditorUtility.SetDirty(agent);
        }

        EditorGUILayout.HelpBox("Change these values to optimize the agent's behavior in the maze. Use 'Reset' to return to the default values.", MessageType.Info);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(agent);
        }


    }
}
#endif
