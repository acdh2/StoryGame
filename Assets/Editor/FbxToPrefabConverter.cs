using UnityEditor;
using UnityEngine;
using System.IO;

public class FbxToPrefabConverter
{
    [MenuItem("Tools/Convert Selected FBX to Prefabs", true)]
    private static bool ValidateConvert()
    {
        foreach (var obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase) || 
                path.EndsWith(".obj", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    [MenuItem("Tools/Convert Selected FBX to Prefabs")]
    public static void Convert()
    {
        string prefabFolderPath = "Assets/Prefabs";

        if (!Directory.Exists(prefabFolderPath))
        {
            Directory.CreateDirectory(prefabFolderPath);
        }

        foreach (var obj in Selection.objects)
        {
            string assetPath = AssetDatabase.GetAssetPath(obj);
            GameObject fbxModel = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (fbxModel != null)
            {
                string prefabPath = Path.Combine(prefabFolderPath, fbxModel.name + ".prefab");
                prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);

                PrefabUtility.SaveAsPrefabAsset(fbxModel, prefabPath);
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("Geselecteerde objecten omgezet naar prefabs.");
    }
}
