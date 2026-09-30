using System.Collections.Generic;
using UnityEngine;

public class SceneManager : MonoBehaviour
{
    [SerializeField] private LayerMask selectableLayer;
    [SerializeField] private ObjectSelector objectSelector;
    [SerializeField] private StoryDataStore storyDataStore;
    [SerializeField] private string saveFileName = "current.json";
    [SerializeField] private int maxUndoSteps = 50;

    private SceneSaveSystem saveSystem;
    private readonly Stack<string> undoStack = new Stack<string>();
    private readonly Stack<string> redoStack = new Stack<string>();
    private bool isPerformingUndoRedo = false;

    private void Awake()
    {
        saveSystem = GetComponent<SceneSaveSystem>();
        if (storyDataStore == null)
        {
            storyDataStore = GetComponent<StoryDataStore>();
        }
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
        if (saveSystem != null)
        {
            saveSystem.SaveSceneToFile(saveFileName);
        }
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

        if (storyDataStore != null)
        {
            storyDataStore.OnDataChanged += HandleDataChanged;
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

        if (storyDataStore != null)
        {
            storyDataStore.OnDataChanged -= HandleDataChanged;
        }
    }

    private void HandleTransformComplete(GameObject targetObject)
    {
        SaveCurrentScene();
    }

    private void HandleSceneLoaded()
    {
        if (!isPerformingUndoRedo)
        {
            RecordState();
        }
    }

    private void HandleDataChanged()
    {
        if (!isPerformingUndoRedo)
        {
            SaveCurrentScene();
        }
    }

    public void RecordState()
    {
        if (saveSystem == null) return;
        string json = saveSystem.SerializeScene();
        
        if (undoStack.Count > 0 && undoStack.Peek() == json) return;

        undoStack.Push(json);
        redoStack.Clear();

        if (undoStack.Count > maxUndoSteps)
        {
            var temp = new List<string>(undoStack);
            temp.RemoveAt(temp.Count - 1);
            undoStack.Clear();
            for (int i = temp.Count - 1; i >= 0; i--)
            {
                undoStack.Push(temp[i]);
            }
        }
    }

    public GameObject SpawnAndRegister(LevelObjectPalette.LevelItemData itemData, Vector3 spawnPosition, ObjectSelector selector)
    {
        if (itemData.prefab == null) return null;

        GameObject newObject = Instantiate(itemData.prefab, spawnPosition, Quaternion.identity);
        newObject.transform.parent = transform;
        newObject.name = "default";//itemData.itemName;
        SetLayerRecursively(newObject, LayerMask.NameToLayer("SelectableObjects"));

        ObjectIdentifier identifier = newObject.GetComponent<ObjectIdentifier>();
        if (identifier == null)
        {
            identifier = newObject.AddComponent<ObjectIdentifier>();
        }
        identifier.SetPrefabId(itemData.itemName);

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

    public void RenameObject(GameObject targetObject, string newName)
    {
        if (targetObject == null || string.IsNullOrEmpty(newName)) return;

        targetObject.name = newName;
        SaveCurrentScene();
    }

    public void DeleteObject(GameObject targetObject)
    {
        if (targetObject == null) return;

        if (objectSelector != null && objectSelector.SelectedObject == targetObject)
        {
            objectSelector.SelectObject(null);
        }

        Destroy(targetObject);
        SaveCurrentScene();
    }

    public void SaveCurrentScene()
    {
        if (isPerformingUndoRedo) return;
        RecordState();

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

    public void Undo()
    {
        if (undoStack.Count <= 1 || saveSystem == null) return;

        if (objectSelector != null)
        {
            objectSelector.SelectObject(null);
        }

        string currentState = undoStack.Pop();
        redoStack.Push(currentState);

        string previousState = undoStack.Peek();
        isPerformingUndoRedo = true;
        saveSystem.LoadSceneFromJson(previousState);
        isPerformingUndoRedo = false;

        saveSystem.SaveSceneToFile(saveFileName);
    }
    
    public void Redo()
    {
        if (redoStack.Count == 0 || saveSystem == null) return;

        if (objectSelector != null)
        {
            objectSelector.SelectObject(null);
        }

        string nextState = redoStack.Pop();
        undoStack.Push(nextState);

        isPerformingUndoRedo = true;
        saveSystem.LoadSceneFromJson(nextState);
        isPerformingUndoRedo = false;

        saveSystem.SaveSceneToFile(saveFileName);
    }
}