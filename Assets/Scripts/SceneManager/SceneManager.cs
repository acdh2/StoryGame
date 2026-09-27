using UnityEngine;

[RequireComponent(typeof(SceneSaveSystem))]
public class SceneManager : MonoBehaviour
{
    [SerializeField] private LayerMask selectableLayer;
    [SerializeField] private ObjectSelector objectSelector;
    [SerializeField] private string saveFileName = "scene.json";
    private SceneSaveSystem saveSystem;

    private void Awake()
    {
        saveSystem = GetComponent<SceneSaveSystem>();
    }

    private void Start()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (saveSystem != null)
        {
            saveSystem.LoadSceneFromFile(saveFileName);
        }
#endif
    }
    

    private void OnApplicationQuit()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        SaveCurrentScene();
#endif
    }

    private void OnEnable()
    {
        if (objectSelector != null)
        {
            objectSelector.OnTransformComplete += HandleTransformComplete;
        }

        if (saveSystem != null)
        {
            saveSystem.OnSceneLoaded += HandleSceneLoaded;
        }
    }

    private void OnDisable()
    {
        if (objectSelector != null)
        {
            objectSelector.OnTransformComplete -= HandleTransformComplete;
        }

        if (saveSystem != null)
        {
            saveSystem.OnSceneLoaded -= HandleSceneLoaded;
        }
    }

    private void HandleTransformComplete(GameObject targetObject)
    {
        Debug.Log($"Transformatie voltooid voor: {targetObject.name}");
        SaveCurrentScene();
    }

    private void HandleSceneLoaded()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        SaveCurrentScene();
#endif
    }

    public GameObject SpawnAndRegister(LevelObjectPalette.LevelItemData itemData, Vector3 spawnPosition, ObjectSelector selector)
    {
        if (itemData.prefab == null) return null;

        GameObject newObject = Instantiate(itemData.prefab, spawnPosition, Quaternion.identity);
        newObject.transform.parent = transform;
        newObject.name = itemData.itemName;
        SetLayerRecursively(newObject, LayerMask.NameToLayer("SelectableObjects"));

        selector?.SelectObject(newObject);

        SaveCurrentScene();
        return newObject;
    }

    public void ApplyMaterialToTarget(GameObject targetObject, Material material)
    {
        if (targetObject == null || material == null) return;

        Renderer targetRenderer = targetObject.GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            targetRenderer.sharedMaterial = material;
            SaveCurrentScene();
        }
    }

    public void SaveCurrentScene()
    {
        if (saveSystem != null)
        {
            saveSystem.SaveSceneToFile(saveFileName);
        }
    }

    public void LoadCurrentScene()
    {
        if (saveSystem != null)
        {
            saveSystem.LoadSceneFromFile(saveFileName);
        }
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