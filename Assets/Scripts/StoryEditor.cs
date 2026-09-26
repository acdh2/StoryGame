using UnityEngine;

public class StoryEditor : MonoBehaviour
{
    public StoryDataStore dataStore;

    [TextArea(10, 20)]
    public string CurrentLuaCode;

    private void OnEnable()
    {
        if (dataStore != null)
        {
            dataStore.OnDataChanged += UpdateLuaPreview;
        }
    }

    private void OnDisable()
    {
        if (dataStore != null)
        {
            dataStore.OnDataChanged -= UpdateLuaPreview;
            dataStore.Save();
        }
    }

    private void UpdateLuaPreview()
    {
        if (dataStore != null)
        {
            CurrentLuaCode = dataStore.ToLuaScript();
        }
    }

    public void SaveState()
    {
        dataStore?.Save();
    }

    public void LoadState()
    {
        dataStore?.Load();
    }
}