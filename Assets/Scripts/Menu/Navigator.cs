using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Navigator : UIControllerBase
{
    [Tooltip("De naam van het GameObject dat bij de start als enige zichtbaar moet zijn.")]
    public string defaultScreenName;

    private readonly List<GameObject> screens = new List<GameObject>();
    private readonly List<UIDocumentLifecycle> screenLifecycles = new List<UIDocumentLifecycle>();
    private readonly List<(Button button, System.Action action)> registeredListeners = new List<(Button, System.Action)>();

    [SerializeField] private ObjectSelector objectSelector;

    // private event Action OnEscapePressed;

    private void Start()
    {
        InitializeDefaultScreen();
    }

    protected override void OnUIEnabled(VisualElement root)
    {
        DisableKeyboardNavigationOnElement(root);
        CacheScreens();
        
        var closeButton = root.Q<Button>("close");
        if (closeButton != null)
        {
            closeButton.style.display = DisplayStyle.None;
            System.Action closeAction = () => SetScreenVisibility(root, screens[0]);
            closeButton.clicked += closeAction;
            registeredListeners.Add((closeButton, closeAction));
        }

        // OnEscapePressed += () => SetScreenVisibility(root, screens[0]);

        BuildDynamicButtons(root);
        InitializeDefaultScreen();
    }

    // void Update()
    // {
    //     if (Input.GetKeyDown(KeyCode.Escape))
    //     {
    //         OnEscapePressed?.Invoke();
    //     }
    // }

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

    private void BuildDynamicButtons(VisualElement root)
    {
        var navigationGroupBox = root.Q<VisualElement>("NavigationBox");
        if (navigationGroupBox == null) return;

        navigationGroupBox.Clear();

        for (int i = 0; i < screens.Count; i++)
        {
            var screen = screens[i];
            if (screen == null) continue;

            if (screen.TryGetComponent<NavigationIcon>(out var navIcon))
            {
                Button btn = new Button();
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
                    btn.style.backgroundColor = Color.clear;
                    btn.style.borderTopWidth = 0;
                    btn.style.borderBottomWidth = 0;
                    btn.style.borderLeftWidth = 0;
                    btn.style.borderRightWidth = 0;                    
                }

                btn.tooltip = screen.name;

                System.Action onClickAction = () => {
                    SetScreenVisibility(root, screen);
                };

                btn.clicked += onClickAction;
                registeredListeners.Add((btn, onClickAction));

                navigationGroupBox.Add(btn);
            }
        }
    }

    private void SetScreenVisibility(VisualElement root, GameObject targetScreen)
    {
        bool isLastScreen = (targetScreen == screens[screens.Count - 1]);

        var navGroupBox = root.Q<VisualElement>("NavigationBox");
        var closeBtn = root.Q<Button>("close");
        var undoRedoBox = root.Q<VisualElement>("undo-redo-box");
        var leftSide = root.Q<VisualElement>("LeftSide");
        var rightSide = root.Q<VisualElement>("RightSide");

        if (navGroupBox != null) navGroupBox.style.display = isLastScreen ? DisplayStyle.None : DisplayStyle.Flex;
        if (closeBtn != null) closeBtn.style.display = isLastScreen ? DisplayStyle.Flex : DisplayStyle.None;
        if (undoRedoBox != null) undoRedoBox.style.display = isLastScreen ? DisplayStyle.None : DisplayStyle.Flex;
        if (leftSide != null) leftSide.style.display = isLastScreen ? DisplayStyle.None : DisplayStyle.Flex;
        if (rightSide != null) rightSide.style.display = isLastScreen ? DisplayStyle.None : DisplayStyle.Flex;

        int screenIndex = screens.IndexOf(targetScreen);
        if (objectSelector != null)
        {
            objectSelector.SetEnabled(screenIndex < 3);
        }

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

    private void InitializeDefaultScreen()
    {
        if (screens.Count == 0) return;

        GameObject defaultScreen = screens.Find(s => s != null && s.name == defaultScreenName);
        if (defaultScreen == null)
        {
            defaultScreen = screens[0];
        }

        if (TryGetComponent<UIDocument>(out var uiDoc) && uiDoc.rootVisualElement != null)
        {
            SetScreenVisibility(uiDoc.rootVisualElement, defaultScreen);
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
