using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class Navigator : MonoBehaviour
{
    [Tooltip("De naam van het GameObject dat bij de start als enige actief moet zijn.")]
    public string defaultScreenName;

    [Tooltip("De naam van het GroupBox element in het UIDocument waarin de knoppen worden geplaatst.")]
    public string groupBoxName = "NavigationBox";

    private UIDocument uiDocument;
    private GroupBox navigationGroupBox;
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

        // Blokkeer navigatie-events op de Navigator UI zelf
        DisableKeyboardNavigationOnElement(root);

        navigationGroupBox = root.Q<GroupBox>(groupBoxName);
        if (navigationGroupBox == null)
        {
            Debug.LogError($"[Navigator] GroupBox met naam '{groupBoxName}' niet gevonden in het UIDocument.");
            return;
        }

        CacheScreens();
        BuildDynamicButtons();
    }

    private void OnDisable()
    {
        UnregisterListeners();
    }

    private void CacheScreens()
    {
        screens.Clear();
        
        Transform parentTransform = transform.parent != null ? transform.parent : transform.root;

        foreach (Transform child in parentTransform)
        {
            if (child != transform) 
            {
                screens.Add(child.gameObject);
            }
        }
    }

    private void BuildDynamicButtons()
    {
        UnregisterListeners();
        navigationGroupBox.Clear();

        foreach (var screen in screens)
        {
            if (screen == null) continue;

            if (screen.TryGetComponent<NavigationIcon>(out var navIcon))
            {
                Button btn = new Button();
                
                btn.style.width = 48;
                btn.style.height = 48;
                btn.style.marginLeft = 4;
                btn.style.marginRight = 4;
                btn.style.marginTop = 4;
                btn.style.marginBottom = 4;

                // Voorkom dat de navigatieknoppen focus pakken
                btn.focusable = false;

                if (navIcon.icon != null)
                {
                    btn.style.backgroundImage = new StyleBackground(navIcon.icon);
                }

                btn.tooltip = screen.name;

                System.Action onClickAction = () => OpenScreen(screen);
                btn.clicked += onClickAction;
                registeredListeners.Add((btn, onClickAction));

                navigationGroupBox.Add(btn);
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
                bool shouldBeActive = (screen == targetScreen);
                screen.SetActive(shouldBeActive);

                // Als het scherm geactiveerd wordt, pas de keyboard-instellingen toe op het UIDocument
                if (shouldBeActive)
                {
                    ApplyKeyboardSettingsToScreen(screen);
                }
            }
        }
    }

    private void ApplyKeyboardSettingsToScreen(GameObject screen)
    {
        if (screen.TryGetComponent<UIDocument>(out var targetUIDoc))
        {
            var root = targetUIDoc.rootVisualElement;
            if (root == null) return;

            // 1. Zorg dat we alleen keyboard navigation blokkeren op elementen buiten een TextField
            DisableKeyboardNavigationOnElement(root);

            // 2. Maak elementen non-focusable, MAAR sla TextField EN al zijn interne kinderen over
            root.Query<VisualElement>().ForEach(element =>
            {
                // Check of het element zelf een TextField is, OF in een TextField zit
                bool isInsideTextField = element is TextField || element.GetFirstAncestorOfType<TextField>() != null;

                if (!isInsideTextField)
                {
                    element.focusable = false;
                }
            });
        }
    }

    private void DisableKeyboardNavigationOnElement(VisualElement root)
    {
        root.RegisterCallback<NavigationMoveEvent>(evt => evt.StopPropagation(), TrickleDown.TrickleDown);
        root.RegisterCallback<NavigationSubmitEvent>(evt => evt.StopPropagation(), TrickleDown.TrickleDown);
        root.RegisterCallback<NavigationCancelEvent>(evt => evt.StopPropagation(), TrickleDown.TrickleDown);
    }
}
