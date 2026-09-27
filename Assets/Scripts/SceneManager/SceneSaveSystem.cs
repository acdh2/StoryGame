using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(StoryDataStore))]
public class SceneSaveSystem : MonoBehaviour
{
    [SerializeField] private LevelObjectPalette levelObjectPalette;
    [SerializeField] private MaterialPalette materialPalette;

    private StoryDataStore storyDataStore;

    public event Action OnSceneLoaded;

    [System.Serializable]
    public class SaveData
    {
        public List<ObjectSaveData> objects = new List<ObjectSaveData>();
        public string storyJson;
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
    }

    public string SerializeScene()
    {
        SaveData data = new SaveData();

        foreach (Transform child in transform)
        {
            Renderer renderer = child.GetComponent<Renderer>();
            string matName = renderer != null && renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "";

            ObjectIdentifier identifier = child.GetComponent<ObjectIdentifier>();
            string id = identifier != null ? identifier.PrefabId : child.name;

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

        return JsonUtility.ToJson(data, true);
    }

    public void LoadSceneFromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        SaveData data = JsonUtility.FromJson<SaveData>(json);
        if (data == null) return;

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
                    if (identifier == null)
                    {
                        identifier = spawned.AddComponent<ObjectIdentifier>();
                    }
                    identifier.SetPrefabId(itemData.itemName);

                    SetLayerRecursively(spawned, LayerMask.NameToLayer("SelectableObjects"));

                    if (!string.IsNullOrEmpty(objData.materialName))
                    {
                        Material mat = FindMaterialByName(objData.materialName);
                        if (mat != null)
                        {
                            Renderer targetRenderer = spawned.GetComponent<Renderer>();
                            if (targetRenderer != null)
                            {
                                targetRenderer.sharedMaterial = mat;
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

        OnSceneLoaded?.Invoke();
    }

    public void NewScene()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        if (storyDataStore != null)
        {
            storyDataStore.SetCommands(new List<CommandData>());
        }

        OnSceneLoaded?.Invoke();
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