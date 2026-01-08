using UnityEngine;
using UnityEditor;
using System.IO;

public static class CreateMaterialsFromSelection
{
    // Change this path if needed (must be inside Assets/)
    private const string TargetFolder = "Assets/GeneratedMaterials";

    [MenuItem("Tools/Materials/Create Materials From Selected GameObjects")]
    public static void CreateMaterials()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            Debug.LogWarning("No GameObjects selected.");
            return;
        }

        // Ensure target folder exists
        if (!AssetDatabase.IsValidFolder(TargetFolder))
        {
            CreateFolderRecursively(TargetFolder);
        }

        foreach (GameObject go in selectedObjects)
        {
            if (go == null)
                continue;

            string materialName = go.name;
            string materialPath = Path.Combine(TargetFolder, materialName + ".mat");

            // Prevent overwriting existing materials
            if (File.Exists(materialPath))
            {
                Debug.LogWarning($"Material already exists: {materialPath}");
                continue;
            }

            // Create material (Standard shader by default)
            Material mat = new Material(Shader.Find("Standard"));
            mat.name = materialName;

            AssetDatabase.CreateAsset(mat, materialPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Materials created successfully.");
    }

    private static void CreateFolderRecursively(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string currentPath = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string nextPath = currentPath + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, parts[i]);
            }
            currentPath = nextPath;
        }
    }
}
