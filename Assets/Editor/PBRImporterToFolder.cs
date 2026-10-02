using UnityEngine;
using UnityEditor;
using System.IO;

public class PBRImporterToFolder : EditorWindow
{
    [MenuItem("Tools/Import PBR Materials to Imported_Materials")]
    public static void ShowWindow()
    {
        string rootPath = EditorUtility.OpenFolderPanel("Select Textures Root Folder", "Assets", "");
        if (string.IsNullOrEmpty(rootPath)) return;

        if (rootPath.StartsWith(Application.dataPath))
        {
            rootPath = "Assets" + rootPath.Substring(Application.dataPath.Length);
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "Selecteer een map binnen de Assets-map van het project.", "OK");
            return;
        }

        string targetDir = "Assets/Imported_Materials";
        if (!AssetDatabase.IsValidFolder(targetDir))
        {
            AssetDatabase.CreateFolder("Assets", "Imported_Materials");
        }

        string[] subDirs = AssetDatabase.GetSubFolders(rootPath);
        foreach (string dir in subDirs)
        {
            string folderName = Path.GetFileName(dir);
            string matPath = targetDir + "/" + folderName + ".mat";

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { dir });
            foreach (string guid in guids)
            {
                string texPath = AssetDatabase.GUIDToAssetPath(guid);
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                string texName = tex.name.ToLower();

                TextureImporter importer = AssetImporter.GetAtPath(texPath) as TextureImporter;

                if (importer != null)
                {
                    if (texName == "normal")
                    {
                        if (importer.textureType != TextureImporterType.NormalMap || importer.sRGBTexture)
                        {
                            importer.textureType = TextureImporterType.NormalMap;
                            importer.sRGBTexture = false;
                            importer.SaveAndReimport();
                        }
                    }
                    else if (texName == "roughness" || texName == "ao" || texName == "height")
                    {
                        if (importer.sRGBTexture)
                        {
                            importer.sRGBTexture = false;
                            importer.SaveAndReimport();
                        }
                    }
                }

                if (texName == "albedo")
                {
                    mat.SetTexture("_BaseMap", tex);
                    mat.SetTexture("_MainTex", tex);
                }
                else if (texName == "normal")
                {
                    mat.SetTexture("_BumpMap", tex);
                    mat.EnableKeyword("_NORMALMAP");
                }
                else if (texName == "height")
                {
                    mat.SetTexture("_ParallaxMap", tex);
                }
                else if (texName == "ao")
                {
                    mat.SetTexture("_OcclusionMap", tex);
                }
                else if (texName == "roughness")
                {
                    mat.SetTexture("_RoughnessMap", tex);
                }
            }
            EditorUtility.SetDirty(mat);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Materialen succesvol gegenereerd in Assets/Imported_Materials");
    }
}