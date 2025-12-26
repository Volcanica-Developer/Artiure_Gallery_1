using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for GalleryBuilder to add convenient buttons.
/// </summary>
[CustomEditor(typeof(GalleryBuilder))]
public class GalleryBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        
        GalleryBuilder builder = (GalleryBuilder)target;
        
        if (GUILayout.Button("Build Gallery Room"))
        {
            builder.BuildRoom();
        }
        
        if (GUILayout.Button("Clear Room"))
        {
            builder.ClearRoom();
        }
    }
}




