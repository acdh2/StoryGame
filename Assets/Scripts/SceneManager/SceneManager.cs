using System.Collections.Generic;
using UnityEngine;

public class SceneManager : MonoBehaviour
{
    [SerializeField] private LayerMask selectableLayer;
    [SerializeField] private ObjectSelector objectSelector;
    [SerializeField] private GameConfiguration gameConfiguration;
    [SerializeField] private StoryDataStore storyDataStore;
    [SerializeField] private string saveFileName = "current.json";
    [SerializeField] private int maxUndoSteps = 50;

    private SceneSaveSystem saveSystem;
    private readonly Stack<string> undoStack = new Stack<string>();
    private readonly Stack<string> redoStack = new Stack<string>();

    public bool HasChanged { get; private set; }

    public bool CanUndo => undoStack.Count > 1;
    public bool CanRedo => redoStack.Count > 0;

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
            RecordState();
            HasChanged = false;
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
            objectSelector.OnTransformComplete += InvalidateScene;
        }
        if (gameConfiguration != null)
        {
            gameConfiguration.OnSettingChanged += InvalidateScene;
        }

        if (storyDataStore != null)
        {
            storyDataStore.OnDataChanged += InvalidateScene;
        }
    }

    private void OnDisable()
    {
        if (objectSelector != null)
        {
            objectSelector.OnTransformComplete -= InvalidateScene;
        }

        if (gameConfiguration != null)
        {
            gameConfiguration.OnSettingChanged -= InvalidateScene;
        }

        if (storyDataStore != null)
        {
            storyDataStore.OnDataChanged -= InvalidateScene;
        }
    }

    private void InvalidateScene()
    {
        RecordState();
        SaveSceneInternal();
    }

    private void RecordState()
    {
        if (saveSystem == null) return;
        string json = saveSystem.SerializeScene();
        
        if (undoStack.Count > 0 && undoStack.Peek() == json) return;

        undoStack.Push(json);
        redoStack.Clear();
        HasChanged = true;

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

    public GameObject SpawnAndRegister(LevelObjectPalette.LevelItemData itemData, Vector3 spawnPosition)
    {
        if (itemData.prefab == null) return null;

        GameObject newObject = Instantiate(itemData.prefab, spawnPosition, Quaternion.identity);
        newObject.transform.parent = transform;
        newObject.name = "default";
        SetLayerRecursively(newObject, LayerMask.NameToLayer("SelectableObjects"));

        ObjectIdentifier identifier = newObject.GetComponent<ObjectIdentifier>();
        if (identifier == null)
        {
            identifier = newObject.AddComponent<ObjectIdentifier>();
        }
        identifier.SetPrefabId(itemData.itemName);
        identifier.SetMaterialId("");

        objectSelector?.SelectObject(newObject);

        RecordState();
        SaveSceneInternal();
        return newObject;
    }

    public void ApplyMaterialToTarget(GameObject targetObject, Material material)
    {        
        if (targetObject == null || material == null) return;

        ObjectIdentifier objectId = targetObject.GetComponentInChildren<ObjectIdentifier>();
        if (objectId != null)
        {
            objectId.SetMaterialId(material.name);
        }

        Renderer[] renderers = targetObject.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            foreach (Renderer r in renderers)
            {
                r.sharedMaterial = material;
            }
            RecordState();            
            SaveSceneInternal();
        }
    }

    public void RenameObject(GameObject targetObject, string newName)
    {
        if (targetObject == null || string.IsNullOrEmpty(newName)) return;
        if (targetObject.CompareTag("SpawnPoint")) return;

        targetObject.name = newName.ToLowerInvariant();
        RecordState();
        SaveSceneInternal();
    }

    public void DeleteObject(GameObject targetObject)
    {
        if (targetObject == null) return;

        if (objectSelector != null && objectSelector.SelectedObject == targetObject)
        {
            objectSelector.SelectObject(null);
        }

        Destroy(targetObject);
        RecordState();
        SaveSceneInternal();
    }

    private void SaveSceneInternal() 
    {
        if (saveSystem != null)
        {
            saveSystem.SaveSceneToFile(saveFileName);
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

    public void NewScene()
    {
        if (saveSystem == null) return;

        // 1. Zorg dat de huidige staat vaststaat op de stack
        RecordState();

        // 2. Wis de scene inhoud
        saveSystem.NewScene();

        // 3. Sla exact 1 schone lege staat op
        RecordState();
        SaveSceneInternal();
        HasChanged = false;
    }

    public void LoadSceneFromJson(string json)
    {
        if (saveSystem == null) return;

        saveSystem.LoadSceneFromJson(json);
        
        undoStack.Clear();
        redoStack.Clear();
        
        RecordState();
        HasChanged = false;
    }

    public string SerializeScene()
    {
        if (saveSystem != null) return saveSystem.SerializeScene();
        return "";
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
        saveSystem.LoadSceneFromJson(previousState);

        saveSystem.SaveSceneToFile(saveFileName);
        HasChanged = true;
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

        saveSystem.LoadSceneFromJson(nextState);

        saveSystem.SaveSceneToFile(saveFileName);
        HasChanged = true;
    }

    public List<string> GetUniqueObjectNames()
    {
        var names = new HashSet<string>();
        foreach (Transform child in transform)
        {
            if (child.CompareTag("SpawnPoint")) continue;
            names.Add(child.gameObject.name.ToLower());
        }
        return new List<string>(names);
    }    

    private readonly Stack<string> tempStateStack = new Stack<string>();

    public void PushSceneState()
    {
        if (saveSystem == null) return;
        string json = saveSystem.SerializeScene();
        tempStateStack.Push(json);
    }

    public void PopSceneState()
    {
        if (tempStateStack.Count == 0 || saveSystem == null) return;

        if (objectSelector != null)
        {
            objectSelector.SelectObject(null);
        }

        string state = tempStateStack.Pop();
        saveSystem.LoadSceneFromJson(state);
    }    
}