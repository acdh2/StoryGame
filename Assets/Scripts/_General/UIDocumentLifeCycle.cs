using System;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UIDocumentLifecycle : MonoBehaviour
{
    public event Action<VisualElement> OnUIEnabledEvent;
    public event Action OnUIDisabledEvent;

    private UIDocument uiDocument;
    private bool isVisible = true;

    public bool IsVisible
    {
        get => isVisible;
        set => SetVisible(value);
    }

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void Start()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        ApplyVisibility(isVisible);
    }

    public void SetVisible(bool visible)
    {
        if (isVisible == visible) return;
        isVisible = visible;
        ApplyVisibility(isVisible);
    }

    private void ApplyVisibility(bool visible)
    {
        if (uiDocument == null || uiDocument.rootVisualElement == null) return;

        if (visible)
        {
            uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
            OnUIEnabledEvent?.Invoke(uiDocument.rootVisualElement);
        }
        else
        {
            OnUIDisabledEvent?.Invoke();
            uiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }

}