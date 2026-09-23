using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class Navigator : MonoBehaviour
{
    [Tooltip("De naam van het GameObject dat bij de start als enige actief moet zijn.")]
    public string defaultScreenName;

    private UIDocument uiDocument;
    private readonly List<GameObject> screens = new List<GameObject>();
    private readonly List<(Button button, System.Action action)> registeredListeners = new List<(Button, System.Action)>();

    private void Awake()
    {
        CacheScreens();
    }

    private void Start()
    {
        InitializeDefaultScreen();
    }

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        VisualElement root = uiDocument.rootVisualElement;

        CacheScreens();
        UnregisterListeners();

        foreach (var screen in screens)
        {
            if (screen == null) continue;

            string screenName = screen.name;
            Button btn = root.Q<Button>(screenName);

            if (btn != null)
            {
                System.Action onClickAction = () => OpenScreen(screen);
                btn.clicked += onClickAction;
                registeredListeners.Add((btn, onClickAction));
            }
            else
            {
                Debug.LogWarning($"[Navigator] Geen knop gevonden met de naam '{screenName}' in het UIDocument.");
            }
        }
    }

    private void OnDisable()
    {
        UnregisterListeners();
    }

    private void CacheScreens()
    {
        screens.Clear();
        
        foreach (Transform child in transform.root)
        {
            if (child != transform) {
                screens.Add(child.gameObject);
            }
        }
    }

    private void InitializeDefaultScreen()
    {
        if (screens.Count == 0) return;

        GameObject defaultScreen = screens.Find(s => s != null && s.name == defaultScreenName);

        if (defaultScreen != null)
        {
            OpenScreen(defaultScreen);
        }
        else
        {
            if (!string.IsNullOrEmpty(defaultScreenName))
            {
                Debug.LogWarning($"[Navigator] Default scherm '{defaultScreenName}' niet gevonden. Eerste scherm wordt geladen.");
            }
            OpenScreen(screens[0]);
        }
    }

    private void UnregisterListeners()
    {
        foreach (var (button, action) in registeredListeners)
        {
            if (button != null && action != null)
            {
                button.clicked -= action;
            }
        }
        registeredListeners.Clear();
    }

    public void OpenScreen(GameObject targetScreen)
    {
        foreach (var screen in screens)
        {
            if (screen != null)
            {
                screen.SetActive(screen == targetScreen);
            }
        }
    }
}