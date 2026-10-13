using UnityEditor;
using UnityEngine;

public class MeshReadWriteEnabler : EditorWindow
{
    [MenuItem("Tools/Enable ReadWrite for Meshes", true)]
    private static bool ValidateEnableReadWrite()
    {
        return Selection.activeObject != null && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));
    }

    [MenuItem("Tools/Enable ReadWrite for Meshes")]
    public static void EnableReadWrite()
    {
        string folderPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { folderPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
        }
        Debug.Log("Read/Write ingeschakeld voor alle meshes in " + folderPath);
    }
}