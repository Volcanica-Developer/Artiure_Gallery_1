#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ChildSpawner))]
public class ChildSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector (count, childPrefab, clearExistingChildren)
        DrawDefaultInspector();

        ChildSpawner spawner = (ChildSpawner)target;

        EditorGUILayout.Space();
        if (GUILayout.Button("Create Children"))
        {
            spawner.CreateChildren();
        }
    }
}
#endif
