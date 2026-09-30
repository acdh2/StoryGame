using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Navigator : UIControllerBase
{
    [Tooltip("De naam van het GameObject dat bij de start als enige zichtbaar moet zijn.")]
    public string defaultScreenName;

    [Tooltip("De naam van het GroupBox element in het UIDocument waarin de knoppen worden geplaatst.")]
    public string groupBoxName = "NavigationBox";

    private VisualElement navigationGroupBox;
    private Button closeButton;
    private readonly List<GameObject> screens = new List<GameObject>();
    private readonly List<UIDocumentLifecycle> screenLifecycles = new List<UIDocumentLifecycle>();
    private readonly List<(Button button, System.Action action)> registeredListeners = new List<(Button, System.Action)>();

    private VisualElement undoRedoBox;

    protected override void Awake()
    {
        base.Awake();
        isInitialised = true;
    }

    private void Start()
    {
        InitializeDefaultScreen();
    }

    void ReturnToFile()
    {
        OpenScreen(screens[0]);
        navigationGroupBox.style.display = DisplayStyle.Flex;
        closeButton.style.display = DisplayStyle.None;        
    }

    protected override void OnUIEnabled(VisualElement root)
    {
        DisableKeyboardNavigationOnElement(root);

        navigationGroupBox = root.Q<VisualElement>(groupBoxName);
        if (navigationGroupBox == null)
        {
            Debug.LogError($"[Navigator] GroupBox met naam '{groupBoxName}' niet gevonden in het UIDocument.");
            return;
        }

        undoRedoBox = root.Q<VisualElement>("undo-redo-box");

        closeButton = root.Q<Button>("close");
        closeButton.style.display = DisplayStyle.None;
        closeButton.clicked += () => ReturnToFile();

        CacheScreens();
        BuildDynamicButtons();
        InitializeDefaultScreen();
    }

    protected override void OnUIDisabled()
    {
        UnregisterListeners();
    }

    private void CacheScreens()
    {
        screens.Clear();
        screenLifecycles.Clear();
        
        Transform parentTransform = transform.parent != null ? transform.parent : transform.root;

        foreach (Transform child in parentTransform)
        {
            if (child != transform) 
            {
                screens.Add(child.gameObject);
                screenLifecycles.Add(child.GetComponent<UIDocumentLifecycle>());
            }
        }
    }

    private void BuildDynamicButtons()
    {
        UnregisterListeners();
        navigationGroupBox.Clear();

        for (int i = 0; i < screens.Count; i++)
        {
            var screen = screens[i];
            bool isFirst = (i == 0);
            bool isLast = (i == screens.Count - 1);
            if (screen == null) continue;

            if (screen.TryGetComponent<NavigationIcon>(out var navIcon))
            {
                Button btn = new Button();
                
                btn.style.backgroundColor = Color.clear;
                btn.style.borderTopColor = Color.clear;
                btn.style.borderBottomColor = Color.clear;
                btn.style.borderLeftColor = Color.clear;
                btn.style.borderRightColor = Color.clear;
                
                btn.style.width = 48;
                btn.style.height = 54;
                btn.style.marginLeft = 4;
                btn.style.marginRight = 4;
                btn.style.marginTop = 4;
                btn.style.marginBottom = 4;
                

                btn.focusable = false;

                if (navIcon.icon != null)
                {
                    btn.style.backgroundImage = new StyleBackground(navIcon.icon);
                }

                btn.tooltip = screen.name;
                System.Action onClickAction;

                if (isLast) {
                    onClickAction = () => {
                        navigationGroupBox.style.display = DisplayStyle.None;
                        closeButton.style.display = DisplayStyle.Flex;
                        undoRedoBox.style.display = DisplayStyle.None;
                        OpenScreen(screen);
                    };
                } else if (isFirst) {
                    onClickAction = () => {
                        undoRedoBox.style.display = DisplayStyle.None;
                        OpenScreen(screen);
                    };
                } else {
                    onClickAction = () => {
                        undoRedoBox.style.display = DisplayStyle.Flex;
                        OpenScreen(screen);
                    };
                }
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
        for (int i = 0; i < screens.Count; i++)
        {
            var screen = screens[i];
            var lifecycle = screenLifecycles[i];

            if (screen != null && lifecycle != null)
            {
                bool shouldBeVisible = (screen == targetScreen);
                lifecycle.IsVisible = shouldBeVisible;

                if (shouldBeVisible)
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

            DisableKeyboardNavigationOnElement(root);

            root.Query<VisualElement>().ForEach(element =>
            {
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