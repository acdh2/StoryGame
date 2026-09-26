using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CommandListController : UIControllerBase
{
    public event Action OnUIReady;

    public EditorCameraController editorCameraController;
    public ObjectSelector objectSelector;
    public StoryDataStore dataStore;

    [Header("UI Templates")]
    public VisualTreeAsset itemTemplate;

    private ScrollView scrollView;
    private VisualElement container;
    private readonly List<string> defaultOptions = new List<string> { "say", "option", "label", "jump", "jumpif", "set", "print" };

    private VisualElement draggedElement = null;
    private VisualElement placeholder = null;
    private bool isDragging = false;

    private float lastClickTime = 0f;
    private const float DOUBLE_CLICK_THRESHOLD = 0.3f;
    private EventCallback<MouseDownEvent> scrollViewMouseDownCallback;

    protected override void OnUIEnabled(VisualElement root)
    {
        DisableEditorCamera();

        scrollView = root.Q<ScrollView>("list-scroll-view");
        if (scrollView == null) return;

        container = scrollView.contentContainer;
        container.style.paddingBottom = 300;

        scrollViewMouseDownCallback = evt =>
        {
            if (evt.button != 0) return;
            if (evt.target == scrollView || evt.target == container)
            {
                if (Time.time - lastClickTime < DOUBLE_CLICK_THRESHOLD)
                {
                    dataStore?.AddCommand();
                    evt.StopPropagation();
                }
                lastClickTime = Time.time;
            }
        };
        scrollView.RegisterCallback(scrollViewMouseDownCallback);

        if (dataStore != null)
        {
            dataStore.OnDataChanged += RebuildUI;
            dataStore.Load();
        }

        OnUIReady?.Invoke();
    }

    protected override void OnUIDisabled()
    {
        EnableEditorCamera();

        if (scrollView != null && scrollViewMouseDownCallback != null)
        {
            scrollView.UnregisterCallback(scrollViewMouseDownCallback);
            scrollViewMouseDownCallback = null;
        }

        if (dataStore != null)
        {
            dataStore.OnDataChanged -= RebuildUI;
            dataStore.Save();
        }

        scrollView = null;
        container = null;
        draggedElement = null;
        placeholder = null;
    }

    private void RebuildUI()
    {
        if (container == null || dataStore == null) return;

        container.Clear();

        for (int i = 0; i < dataStore.Commands.Count; i++)
        {
            var data = dataStore.Commands[i];
            int index = i;

            TemplateContainer itemInstance = itemTemplate.Instantiate();
            VisualElement itemRoot = itemInstance.Q<VisualElement>("command-item-root") ?? itemInstance;

            DropdownField dropdown = itemRoot.Q<DropdownField>("command-dropdown");
            if (dropdown != null)
            {
                dropdown.choices = defaultOptions;
                dropdown.value = string.IsNullOrEmpty(data.CommandType) ? defaultOptions[0] : data.CommandType;
                dropdown.RegisterValueChangedCallback(evt =>
                {
                    dataStore.UpdateCommand(index, evt.newValue, data.Argument);
                });
            }

            TextField inputField = itemRoot.Q<TextField>("command-input");
            if (inputField != null)
            {
                inputField.value = data.Argument;
                inputField.RegisterValueChangedCallback(evt =>
                {
                    dataStore.UpdateCommand(index, dropdown != null ? dropdown.value : "say", evt.newValue);
                });
                inputField.RegisterCallback<NavigationMoveEvent>(evt => evt.StopPropagation(), TrickleDown.TrickleDown);
                inputField.RegisterCallback<NavigationSubmitEvent>(evt => evt.StopPropagation(), TrickleDown.TrickleDown);
                inputField.RegisterCallback<NavigationCancelEvent>(evt => evt.StopPropagation(), TrickleDown.TrickleDown);
            }

            RegisterDragEvents(itemRoot, index);
            container.Add(itemRoot);
        }
    }

    private void RegisterDragEvents(VisualElement element, int originalIndex)
    {
        element.RegisterCallback<MouseDownEvent>(evt =>
        {
            if (evt.button != 0) return;

            if (evt.target is VisualElement targetVE)
            {
                VisualElement dragHandle = element.Q<VisualElement>("drag-handle");
                if (dragHandle != null && targetVE != dragHandle && !dragHandle.Contains(targetVE))
                {
                    return;
                }
            }

            isDragging = true;
            draggedElement = element;

            placeholder = new VisualElement();
            placeholder.style.height = element.resolvedStyle.height;
            placeholder.style.width = element.resolvedStyle.width;
            placeholder.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.4f);

            int index = container.IndexOf(element);
            container.Insert(index, placeholder);

            draggedElement.style.position = Position.Absolute;
            draggedElement.style.width = placeholder.resolvedStyle.width;
            draggedElement.CaptureMouse();

            UpdateDraggedElementPosition(evt.mousePosition);
            evt.StopPropagation();
        });

        element.RegisterCallback<MouseMoveEvent>(evt =>
        {
            if (!isDragging || draggedElement != element) return;

            UpdateDraggedElementPosition(evt.mousePosition);

            float draggedY = draggedElement.worldBound.center.y;
            int placeholderIndex = container.IndexOf(placeholder);

            foreach (var child in container.Children())
            {
                if (child == placeholder || child == draggedElement) continue;

                int childIndex = container.IndexOf(child);

                if (draggedY < child.worldBound.center.y && placeholderIndex > childIndex)
                {
                    container.Insert(childIndex, placeholder);
                    break;
                }
                else if (draggedY > child.worldBound.center.y && placeholderIndex < childIndex)
                {
                    container.Insert(childIndex, placeholder);
                    break;
                }
            }

            evt.StopPropagation();
        });

        element.RegisterCallback<MouseUpEvent>(evt =>
        {
            if (!isDragging || draggedElement != element) return;

            draggedElement.ReleaseMouse();
            isDragging = false;

            Rect containerBounds = scrollView.worldBound;
            if (!containerBounds.Contains(evt.mousePosition))
            {
                dataStore.RemoveCommand(originalIndex);
            }
            else
            {
                int targetIndex = container.IndexOf(placeholder);
                if (originalIndex < targetIndex)
                {
                    targetIndex--;
                }

                dataStore.MoveCommand(originalIndex, targetIndex);
            }

            draggedElement = null;
            placeholder = null;
            evt.StopPropagation();
        });
    }

    private void UpdateDraggedElementPosition(Vector2 mousePosition)
    {
        if (draggedElement == null || container == null) return;
        Vector2 localPos = container.WorldToLocal(mousePosition);
        draggedElement.style.left = localPos.x - (draggedElement.resolvedStyle.width / 2);
        draggedElement.style.top = localPos.y - (draggedElement.resolvedStyle.height / 2);
    }

    private void DisableEditorCamera() 
    {
        if (editorCameraController != null) editorCameraController.enabled = false;
        if (objectSelector != null)
        {
            objectSelector.SelectObject(null);
            objectSelector.enabled = false;
        }
    }

    private void EnableEditorCamera()
    {
        if (editorCameraController != null) editorCameraController.enabled = true;
        if (objectSelector != null) objectSelector.enabled = true;
    }
}