using System;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UIDocumentLifecycle : MonoBehaviour
{
    public event Action<VisualElement> OnUIEnabledEvent;
    public event Action OnUIDisabledEvent;

    private UIDocument uiDocument;
    private bool isInitialized = false;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument != null)
        {
            uiDocument.rootVisualElement.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            uiDocument.rootVisualElement.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            if (uiDocument.rootVisualElement.panel != null && !isInitialized)
            {
                isInitialized = true;
                OnUIEnabledEvent?.Invoke(uiDocument.rootVisualElement);
            }
        }
    }

    private void OnDisable()
    {
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            uiDocument.rootVisualElement.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            uiDocument.rootVisualElement.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        if (isInitialized)
        {
            isInitialized = false;
            OnUIDisabledEvent?.Invoke();
        }
    }

    private void OnAttachToPanel(AttachToPanelEvent evt)
    {
        if (!isInitialized && uiDocument.rootVisualElement != null)
        {
            isInitialized = true;
            OnUIEnabledEvent?.Invoke(uiDocument.rootVisualElement);
        }
    }

    private void OnDetachFromPanel(DetachFromPanelEvent evt)
    {
        if (isInitialized)
        {
            isInitialized = false;
            OnUIDisabledEvent?.Invoke();
        }
    }
}