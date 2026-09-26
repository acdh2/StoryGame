using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocumentLifecycle))]
public abstract class UIControllerBase : MonoBehaviour
{
    protected UIDocumentLifecycle Lifecycle { get; private set; }
    protected VisualElement RootElement { get; private set; }

    protected virtual void Awake()
    {
        Lifecycle = GetComponent<UIDocumentLifecycle>();

        if (Lifecycle != null)
        {
            Lifecycle.OnUIEnabledEvent += HandleUIEnabled;
            Lifecycle.OnUIDisabledEvent += HandleUIDisabled;
        }
    }

    protected virtual void OnDestroy()
    {
        if (Lifecycle != null)
        {
            Lifecycle.OnUIEnabledEvent -= HandleUIEnabled;
            Lifecycle.OnUIDisabledEvent -= HandleUIDisabled;
        }
    }

    private void HandleUIEnabled(VisualElement root)
    {
        RootElement = root;
        OnUIEnabled(root);
    }

    private void HandleUIDisabled()
    {
        OnUIDisabled();
        RootElement = null;
    }

    /// <summary>
    /// Wordt aangeroepen zodra het UIDocument enabled is en de rootVisualElement beschikbaar is.
    /// </summary>
    protected virtual void OnUIEnabled(VisualElement root) { }

    /// <summary>
    /// Wordt aangeroepen zodra het UIDocument disabled wordt.
    /// </summary>
    protected virtual void OnUIDisabled() { }
}