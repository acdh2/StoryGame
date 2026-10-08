using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(StoryDataStore))]
public class SceneSaveSystem : MonoBehaviour
{
    const string DEFAULT_MATERIAL_TAG = "&&&DEFAULT&&&";
    
    [SerializeField] private LevelObjectPalette levelObjectPalette;
    [SerializeField] private MaterialPalette materialPalette;
    [SerializeField] private GameConfiguration gameConfiguration;
    [SerializeField] private GameObject floorObject;

    private Material defaultFloorMaterial;
    private StoryDataStore storyDataStore;

    [System.Serializable]
    public class SaveData
    {
        public List<ObjectSaveData> objects = new List<ObjectSaveData>();
        public string floorMaterialName;
        public string storyJson;
        public string configJson;
    }

    [System.Serializable]
    public class ObjectSaveData
    {
        public string prefabId;
        public string customName;
        public string materialName;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    private void Awake()
    {
        storyDataStore = GetComponent<StoryDataStore>();

        if (floorObject != null)
        {
            Renderer floorRenderer = floorObject.GetComponent<Renderer>();
            if (floorRenderer != null)
            {
                defaultFloorMaterial = floorRenderer.sharedMaterial;
            }
        }
    }

    public string SerializeScene()
    {
        SaveData data = new SaveData();

        if (floorObject != null)
        {
            Renderer floorRenderer = floorObject.GetComponent<Renderer>();
            if (floorRenderer != null)
            {
                if (floorRenderer.sharedMaterial == defaultFloorMaterial)
                {
                    data.floorMaterialName = DEFAULT_MATERIAL_TAG;
                }
                else
                {
                    data.floorMaterialName = floorRenderer.sharedMaterial != null ? floorRenderer.sharedMaterial.name : "";
                }
            }
        }

        foreach (Transform child in transform)
        {
            if (child.CompareTag("SpawnPoint")) continue;

            // Zoek de renderer in de child-hiërarchie (LODs)
            Renderer renderer = child.GetComponentInChildren<Renderer>(true);
            string matName = renderer != null && renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "";

            ObjectIdentifier identifier = child.GetComponent<ObjectIdentifier>();
            if (identifier == null) identifier = child.GetComponentInChildren<ObjectIdentifier>();

            string id = identifier != null && !string.IsNullOrEmpty(identifier.PrefabId) ? identifier.PrefabId : child.name;

            // Als er een expliciete materialId is opgeslagen op het ObjectIdentifier component, gebruik die
            if (identifier != null && !string.IsNullOrEmpty(identifier.MaterialId))
            {
                matName = identifier.MaterialId;
            }

            ObjectSaveData objData = new ObjectSaveData
            {
                prefabId = id,
                customName = child.name,
                materialName = matName,
                position = child.position,
                rotation = child.rotation,
                scale = child.localScale
            };

            data.objects.Add(objData);
        }

        if (storyDataStore != null)
        {
            data.storyJson = storyDataStore.SerializeToJson();
        }

        if (gameConfiguration != null)
        {
            data.configJson = gameConfiguration.ExportToJson();
        }

        return JsonUtility.ToJson(data, true);
    }

    public void LoadSceneFromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        ClearChildren();

        SaveData data = JsonUtility.FromJson<SaveData>(json);
        if (data == null) return;

        if (floorObject != null && !string.IsNullOrEmpty(data.floorMaterialName))
        {
            Material mat = (data.floorMaterialName == DEFAULT_MATERIAL_TAG) 
                ? defaultFloorMaterial 
                : FindMaterialByName(data.floorMaterialName);

            if (mat != null)
            {
                Renderer floorRenderer = floorObject.GetComponent<Renderer>();
                if (floorRenderer != null)
                {
                    floorRenderer.sharedMaterial = mat;
                }
            }
        }

        if (data.objects != null)
        {
            foreach (var objData in data.objects)
            {
                string lookupKey = !string.IsNullOrEmpty(objData.prefabId) ? objData.prefabId : objData.customName;
                LevelObjectPalette.LevelItemData itemData = FindItemDataByName(lookupKey);

                if (itemData.prefab != null)
                {
                    GameObject spawned = Instantiate(itemData.prefab, objData.position, objData.rotation);
                    spawned.transform.parent = transform;
                    spawned.transform.localScale = objData.scale;
                    spawned.name = !string.IsNullOrEmpty(objData.customName) ? objData.customName : itemData.itemName;

                    ObjectIdentifier identifier = spawned.GetComponent<ObjectIdentifier>();
                    if (identifier == null) identifier = spawned.AddComponent<ObjectIdentifier>();
                    
                    identifier.SetPrefabId(itemData.itemName);
                    identifier.SetMaterialId(objData.materialName);

                    SetLayerRecursively(spawned, LayerMask.NameToLayer("SelectableObjects"));

                    if (!string.IsNullOrEmpty(objData.materialName))
                    {
                        Material mat = FindMaterialByName(objData.materialName);
                        if (mat != null)
                        {
                            // Pas toe op ALLE renderers in de hiërarchie (voor LODs)
                            Renderer[] renderers = spawned.GetComponentsInChildren<Renderer>(true);
                            foreach (Renderer r in renderers)
                            {
                                r.sharedMaterial = mat;
                            }
                        }
                    }
                }
            }
        }

        if (storyDataStore != null && !string.IsNullOrEmpty(data.storyJson))
        {
            storyDataStore.DeserializeFromJson(data.storyJson);
        }

        if (gameConfiguration != null && !string.IsNullOrEmpty(data.configJson))
        {
            gameConfiguration.ImportFromJson(data.configJson);
        }
    }

    public void NewScene()
    {
        ClearChildren();

        if (floorObject != null && defaultFloorMaterial != null)
        {
            Renderer floorRenderer = floorObject.GetComponent<Renderer>();
            if (floorRenderer != null)
            {
                floorRenderer.sharedMaterial = defaultFloorMaterial;
            }
        }

        if (storyDataStore != null)
        {
            storyDataStore.SetCommands(new List<CommandData>());
            storyDataStore.AddCommand("text", "Welcome to the story!");
        }

        if (gameConfiguration != null)
        {
            gameConfiguration.Reset();
        }
    }

    private void ClearChildren()
    {
        // Gebruik DestroyImmediate in editor / runtime opruimen om vervuiling in dezelfde frame te voorkomen
        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in transform)
        {
            children.Add(child.gameObject);
        }
        
        foreach (GameObject child in children)
        {
            DestroyImmediate(child);
        }
    }

    public void SaveSceneToFile(string fileName = "scene.json")
    {
        string json = SerializeScene();
        string path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllText(path, json);
    }

    public void LoadSceneFromFile(string fileName = "scene.json")
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        if (!File.Exists(path)) return;

        string json = File.ReadAllText(path);
        LoadSceneFromJson(json);
    }

    private LevelObjectPalette.LevelItemData FindItemDataByName(string itemName)
    {
        if (levelObjectPalette == null) return default;
        return levelObjectPalette.FindItemDataByName(itemName);
    }

    private Material FindMaterialByName(string materialName)
    {
        if (materialPalette == null) return null;
        return materialPalette.FindMaterialByName(materialName);
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}