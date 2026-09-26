using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class StoryEditor : MonoBehaviour
{
    [Header("Default Lua Code")]
    [TextArea(10, 20)]
    public string defaultLuaScript = @"";

    // De opgeslagen actuele Lua code (beschikbaar ook als het GameObject inactief is)
    public string CurrentLuaCode { get; private set; }

    private UIDocument uiDocument;
    private TextField codeTextField;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        CurrentLuaCode = defaultLuaScript;
    }

    private void OnEnable()
    {
        BindUI();
    }

    private void OnDisable()
    {
        UnbindUI();
    }

    private void BindUI()
    {
        if (uiDocument == null || uiDocument.rootVisualElement == null) return;

        codeTextField = uiDocument.rootVisualElement.Q<TextField>("code");

        if (codeTextField != null)
        {
            // Vul het tekstveld met de huidige opgeslagen code
            codeTextField.value = CurrentLuaCode;

            // Luister naar wijzigingen in het tekstveld
            codeTextField.RegisterValueChangedCallback(OnCodeChanged);
        }
        else
        {
            Debug.LogWarning("[StoryEditor] TextField met de naam 'code' niet gevonden in het UIDocument.");
        }
    }

    private void UnbindUI()
    {
        if (codeTextField != null)
        {
            codeTextField.UnregisterValueChangedCallback(OnCodeChanged);
            codeTextField = null;
        }
    }

    private void OnCodeChanged(ChangeEvent<string> evt)
    {
        // Sla de gewijzigde tekst direct intern op
        CurrentLuaCode = evt.newValue;
    }
}