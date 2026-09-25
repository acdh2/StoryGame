using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class LevelObjectPalettePopulator : AssetPostprocessor
{
    private const string ROOT_FOLDER = "Assets/BuildingMaterials";
    private const string ICONS_FOLDER = "Assets/BuildingMaterials/_GeneratedIcons";

    private static void OnPostprocessAllAssets(
        string[] importedAssets, 
        string[] deletedAssets, 
        string[] movedAssets, 
        string[] movedFromAssetPaths)
    {
        bool touchedTargetFolder = false;

        foreach (string path in importedAssets)
        {
            // Voorkom een oneindige lus door wijzigingen in de gegenereerde icoontjes-map te negeren
            if (path.StartsWith(ICONS_FOLDER)) continue;
            if (path.StartsWith(ROOT_FOLDER)) { touchedTargetFolder = true; break; }
        }

        if (touchedTargetFolder)
        {
            // Gebruik delayCall om Unity de tijd te geven de prefab previews op de achtergrond op te bouwen
            EditorApplication.delayCall += PopulateAllPalettesInScene;
        }
    }

    [MenuItem("Tools/Refresh Level Object Palette")]
    public static void PopulateAllPalettesInScene()
    {
        var palettes = Object.FindObjectsByType<LevelObjectPalette>(FindObjectsSortMode.None);
        if (palettes.Length == 0) return;

        foreach (var palette in palettes)
        {
            UpdatePaletteData(palette);
        }
    }

    private static void UpdatePaletteData(LevelObjectPalette palette)
    {
        if (!Directory.Exists(ROOT_FOLDER)) return;

        // Zorg dat de map voor gegenereerde pictogrammen bestaat
        if (!Directory.Exists(ICONS_FOLDER))
        {
            Directory.CreateDirectory(ICONS_FOLDER);
            AssetDatabase.Refresh();
        }

        Undo.RecordObject(palette, "Auto Populate Level Object Palette");

        SerializedObject serializedPalette = new SerializedObject(palette);
        SerializedProperty categoriesProp = serializedPalette.FindProperty("categories");
        categoriesProp.ClearArray();

        string[] subDirectories = Directory.GetDirectories(ROOT_FOLDER);

        int categoryIndex = 0;
        foreach (string subDir in subDirectories)
        {
            // Sla onze gegenereerde icoontjesmap over als categorie
            if (subDir.Replace("\\", "/").StartsWith(ICONS_FOLDER)) continue;

            string categoryName = Path.GetFileName(subDir);
            Texture2D categoryIcon = FindCategoryIcon(ROOT_FOLDER, categoryName);

            categoriesProp.InsertArrayElementAtIndex(categoryIndex);
            SerializedProperty categoryElem = categoriesProp.GetArrayElementAtIndex(categoryIndex);

            categoryElem.FindPropertyRelative("categoryName").stringValue = categoryName;
            categoryElem.FindPropertyRelative("categoryIcon").objectReferenceValue = categoryIcon;

            SerializedProperty itemsProp = categoryElem.FindPropertyRelative("items");
            itemsProp.ClearArray();

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { subDir });

            int itemIndex = 0;
            foreach (string guid in prefabGuids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                if (prefab == null) continue;

                // Genereer of haal de opgeslagen PNG op voor deze prefab
                Texture2D thumbnailAsset = GetOrGeneratePersistentIcon(prefab, prefabPath);

                itemsProp.InsertArrayElementAtIndex(itemIndex);
                SerializedProperty itemElem = itemsProp.GetArrayElementAtIndex(itemIndex);

                itemElem.FindPropertyRelative("itemName").stringValue = prefab.name;
                itemElem.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                itemElem.FindPropertyRelative("thumbnail").objectReferenceValue = thumbnailAsset;

                itemIndex++;
            }

            categoryIndex++;
        }

        serializedPalette.ApplyModifiedProperties();
        EditorUtility.SetDirty(palette);
    }

private static Texture2D GetOrGeneratePersistentIcon(GameObject prefab, string prefabPath)
{
    string iconFileName = $"{prefab.name}_icon.png";
    string iconPath = Path.Combine(ICONS_FOLDER, iconFileName).Replace("\\", "/");

    Texture2D existingIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
    if (existingIcon != null) return existingIcon;

    if (prefab == null) return null;

    int width = 128;
    int height = 128;

    // 1. Maak een tijdelijke preview-scene/objecten aan
    GameObject previewInstance = Object.Instantiate(prefab);
    previewInstance.hideFlags = HideFlags.HideAndDontSave;

    // Zorg dat het object op een isolatielaag staat om interferentie te voorkomen
    int previewLayer = 31; // Gebruik een ongebruikte layer
    SetLayerRecursively(previewInstance, previewLayer);

    // Bereken de Bounding Box om de camera goed te positioneren
    Bounds bounds = GetBounds(previewInstance);
    if (bounds.size == Vector3.zero) bounds = new Bounds(previewInstance.transform.position, Vector3.one);

    // 2. Maak een tijdelijke Camera aan
    GameObject camObj = new GameObject("IconCamera");
    camObj.hideFlags = HideFlags.HideAndDontSave;
    Camera cam = camObj.AddComponent<Camera>();

    cam.cullingMask = 1 << previewLayer;
    cam.clearFlags = CameraClearFlags.SolidColor;
    cam.backgroundColor = new Color(0, 0, 0, 0); // Volledig transparante achtergrond
    cam.nearClipPlane = 0.01f;
    cam.farClipPlane = 100f;

    // Positioneer de camera schuin boven het object (iso-perspectief)
    float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
    float distance = maxExtent / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
    
    Vector3 cameraDir = new Vector3(1f, 1f, -1f).normalized;
    cam.transform.position = bounds.center + cameraDir * (distance * 1.5f);
    cam.transform.LookAt(bounds.center);

    // 3. Maak tijdelijke belichting aan
    GameObject lightObj = new GameObject("IconLight");
    lightObj.hideFlags = HideFlags.HideAndDontSave;
    Light light = lightObj.AddComponent<Light>();
    light.type = LightType.Directional;
    light.intensity = 1.2f;
    lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

    // 4. Render naar RenderTexture
    RenderTexture rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
    cam.targetTexture = rt;
    cam.Render();

    RenderTexture previousActive = RenderTexture.active;
    RenderTexture.active = rt;

    Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
    result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
    result.Apply();

    RenderTexture.active = previousActive;
    cam.targetTexture = null;

    // Opruimen van tijdelijke objecten
    RenderTexture.ReleaseTemporary(rt);
    Object.DestroyImmediate(previewInstance);
    Object.DestroyImmediate(camObj);
    Object.DestroyImmediate(lightObj);

    // 5. Opslaan als PNG
    byte[] bytes = result.EncodeToPNG();
    Object.DestroyImmediate(result);

    File.WriteAllBytes(iconPath, bytes);
    AssetDatabase.ImportAsset(iconPath, ImportAssetOptions.ForceUpdate);

    // Configureer de import-instellingen direct als Sprite/Texture met Alpha
    TextureImporter importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
    if (importer != null)
    {
        importer.alphaIsTransparency = true;
        importer.textureType = TextureImporterType.Sprite;
        importer.SaveAndReimport();
    }

    return AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
}

private static Bounds GetBounds(GameObject obj)
{
    Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
    if (renderers.Length == 0) return new Bounds(obj.transform.position, Vector3.zero);

    Bounds bounds = renderers[0].bounds;
    for (int i = 1; i < renderers.Length; i++)
    {
        bounds.Encapsulate(renderers[i].bounds);
    }
    return bounds;
}

private static void SetLayerRecursively(GameObject obj, int layer)
{
    obj.layer = layer;
    foreach (Transform child in obj.transform)
    {
        SetLayerRecursively(child.gameObject, layer);
    }
}

    private static Texture2D GetOrGeneratePersistentIcon2(GameObject prefab, string prefabPath)
    {
        string iconFileName = $"{prefab.name}_icon.png";
        string iconPath = Path.Combine(ICONS_FOLDER, iconFileName).Replace("\\", "/");

        // 1. Controleer of de icoon-asset op schijf al bestaat
        Texture2D existingIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
        if (existingIcon != null)
        {
            return existingIcon;
        }

        // 2. Probeer de preview op te vragen
        Texture2D preview = AssetPreview.GetAssetPreview(prefab);
        if (preview == null)
        {
            preview = AssetPreview.GetMiniThumbnail(prefab);
        }

        if (preview == null) return null;

        // 3. Zet de preview om naar een opslaanbare Texture2D en sla op als PNG
        RenderTexture renderTexture = RenderTexture.GetTemporary(preview.width, preview.height);
        Graphics.Blit(preview, renderTexture);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;

        Texture2D readableTexture = new Texture2D(preview.width, preview.height, TextureFormat.RGBA32, false);
        readableTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        readableTexture.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);

        byte[] bytes = readableTexture.EncodeToPNG();
        Object.DestroyImmediate(readableTexture);

        File.WriteAllBytes(iconPath, bytes);
        AssetDatabase.ImportAsset(iconPath, ImportAssetOptions.ForceUpdate);

        return AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
    }

    private static Texture2D FindCategoryIcon(string rootFolder, string categoryName)
    {
        string[] extensions = new string[] { ".png", ".jpg", ".jpeg", ".asset" };
        foreach (string ext in extensions)
        {
            string iconPath = Path.Combine(rootFolder, categoryName + ext).Replace("\\", "/");
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            if (icon != null) return icon;
        }
        return null;
    }
}