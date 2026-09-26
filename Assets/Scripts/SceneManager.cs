using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SceneManager : MonoBehaviour
{
    [SerializeField] private LayerMask selectableLayer;
    [SerializeField] private ObjectSelector objectSelector;
    [SerializeField] private LevelObjectPalette levelObjectPalette;
    [SerializeField] private MaterialPalette materialPalette;

    [System.Serializable]
    public class SaveData
    {
        public List<ObjectSaveData> objects = new List<ObjectSaveData>();
    }

    [System.Serializable]
    public class ObjectSaveData
    {
        public string itemName;
        public string materialName;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    private void Start()
    {
        LoadScene();
    }

    private void OnEnable()
    {
        if (objectSelector != null)
        {
            objectSelector.OnTransformComplete += HandleTransformComplete;
        }
    }

    private void OnDisable()
    {
        if (objectSelector != null)
        {
            objectSelector.OnTransformComplete -= HandleTransformComplete;
        }
    }

    private void HandleTransformComplete(GameObject targetObject)
    {
        Debug.Log($"Transformatie voltooid voor: {targetObject.name}");
        SaveScene();
    }

    public GameObject SpawnAndRegister(LevelObjectPalette.LevelItemData itemData, Vector3 spawnPosition, ObjectSelector selector)
    {
        if (itemData.prefab == null) return null;

        GameObject newObject = Instantiate(itemData.prefab, spawnPosition, Quaternion.identity);
        newObject.transform.parent = transform;
        newObject.name = itemData.itemName;
        SetLayerRecursively(newObject, LayerMask.NameToLayer("SelectableObjects"));

        selector?.SelectObject(newObject);

        SaveScene();
        return newObject;
    }

    public void ApplyMaterialToTarget(GameObject targetObject, Material material)
    {
        if (targetObject == null || material == null) return;

        Renderer targetRenderer = targetObject.GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            targetRenderer.sharedMaterial = material;
            SaveScene();
        }
    }

    public void SaveScene(string fileName = "scene.json")
    {
        SaveData data = new SaveData();

        foreach (Transform child in transform)
        {
            Renderer renderer = child.GetComponent<Renderer>();
            string matName = renderer != null && renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "";

            ObjectSaveData objData = new ObjectSaveData
            {
                itemName = child.name,
                materialName = matName,
                position = child.position,
                rotation = child.rotation,
                scale = child.localScale
            };

            data.objects.Add(objData);
        }

        string json = JsonUtility.ToJson(data, true);
        string path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllText(path, json);
    }

    public void LoadScene(string fileName = "scene.json")
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        if (!File.Exists(path)) return;

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        string json = File.ReadAllText(path);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        foreach (var objData in data.objects)
        {
            LevelObjectPalette.LevelItemData itemData = FindItemDataByName(objData.itemName);
            if (itemData.prefab != null)
            {
                GameObject spawned = Instantiate(itemData.prefab, objData.position, objData.rotation);
                spawned.transform.parent = transform;
                spawned.transform.localScale = objData.scale;
                spawned.name = objData.itemName;
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

    private LevelObjectPalette.LevelItemData FindItemDataByName(string itemName)
    {
        // Reflection of public fields via serialized data isn't directly exposed unless configured, 
        // assuming LevelObjectPalette has a method or public access, or you use a lookup dictionary.
        // For standard Unity fields, you can expose a public method on LevelObjectPalette to find items.
        return default;
    }

    private Material FindMaterialByName(string materialName)
    {
        return null;
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