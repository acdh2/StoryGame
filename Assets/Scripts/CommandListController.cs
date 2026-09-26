using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class CommandListController : MonoBehaviour
{
    [Header("UI Templates & Setup")]
    public VisualTreeAsset itemTemplate;
    public int initialItemCount = 24;

    private UIDocument uiDocument;
    private ScrollView scrollView;
    private VisualElement container;

    private VisualElement draggedElement = null;
    private VisualElement placeholder = null;
    private Vector2 dragStartPosition;
    private bool isDragging = false;

    private float lastClickTime = 0f;
    private const float DOUBLE_CLICK_THRESHOLD = 0.3f;

    private readonly List<string> defaultOptions = new List<string> { "say", "option", "label", "jump", "jumpif", "set", "print" };

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        VisualElement root = uiDocument.rootVisualElement;

        scrollView = root.Q<ScrollView>("list-scroll-view");
        if (scrollView == null) return;

        container = scrollView.contentContainer;
        container.style.paddingBottom = 300;

        scrollView.RegisterCallback<MouseDownEvent>(OnScrollViewMouseDown);
    }

    public void ClearList()
    {
        if (container != null)
        {
            container.Clear();
        }
    }

    public void PopulateInitialList()
    {
        ClearList();
        for (int i = 0; i < initialItemCount; i++)
        {
            AddItemToList();
        }
    }

    public VisualElement AddItemToList(string commandType = null, string argumentText = "")
    {
        if (itemTemplate == null)
        {
            Debug.LogError("[CommandListController] Geen itemTemplate toegewezen!");
            return null;
        }

        TemplateContainer itemInstance = itemTemplate.Instantiate();
        VisualElement itemRoot = itemInstance.Q<VisualElement>("command-item-root") ?? itemInstance;

        DropdownField dropdown = itemRoot.Q<DropdownField>("command-dropdown");
        if (dropdown != null)
        {
            dropdown.choices = defaultOptions;
            dropdown.value = string.IsNullOrEmpty(commandType) ? defaultOptions[0] : commandType;
        }

        TextField inputField = itemRoot.Q<TextField>("command-input");
        if (inputField != null)
        {
            inputField.value = argumentText;
            inputField.RegisterCallback<NavigationMoveEvent>(evt => evt.StopPropagation(), TrickleDown.TrickleDown);
            inputField.RegisterCallback<NavigationSubmitEvent>(evt => evt.StopPropagation(), TrickleDown.TrickleDown);
            inputField.RegisterCallback<NavigationCancelEvent>(evt => evt.StopPropagation(), TrickleDown.TrickleDown);
        }

        RegisterDragEvents(itemRoot);
        container.Add(itemRoot);

        return itemRoot;
    }

    public List<CommandData> GetCommandDataList()
    {
        List<CommandData> list = new List<CommandData>();
        if (container == null) return list;

        foreach (VisualElement child in container.Children())
        {
            DropdownField dropdown = child.Q<DropdownField>("command-dropdown");
            TextField inputField = child.Q<TextField>("command-input");

            if (dropdown != null && inputField != null)
            {
                // Alleen opslaan als er daadwerkelijk een gekozen type of invoer is
                list.Add(new CommandData
                {
                    CommandType = dropdown.value,
                    Argument = inputField.value
                });
            }
        }

        return list;
    }

    public void LoadFromDataList(List<CommandData> dataList)
    {
        ClearList();
        if (dataList == null || dataList.Count == 0)
        {
            PopulateInitialList();
            return;
        }

        foreach (var data in dataList)
        {
            AddItemToList(data.CommandType, data.Argument);
        }
    }

    private void OnScrollViewMouseDown(MouseDownEvent evt)
    {
        if (evt.button != 0) return;

        if (evt.target == scrollView || evt.target == container)
        {
            if (Time.time - lastClickTime < DOUBLE_CLICK_THRESHOLD)
            {
                AddItemToList();
                evt.StopPropagation();
            }
            lastClickTime = Time.time;
        }
    }

    private void RegisterDragEvents(VisualElement element)
    {
        element.RegisterCallback<MouseDownEvent>(evt =>
        {
            if (evt.button != 0) return;

            if (evt.target is VisualElement targetVE)
            {
                VisualElement dragHandle = element.Q<VisualElement>("drag-handle");
                if (dragHandle == null || (targetVE != dragHandle && !dragHandle.Contains(targetVE)))
                {
                    return;
                }
            }

            isDragging = true;
            draggedElement = element;
            dragStartPosition = evt.mousePosition;

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
                container.Remove(draggedElement);
                container.Remove(placeholder);
            }
            else
            {
                int oldIndex = container.IndexOf(draggedElement);
                int targetIndex = container.IndexOf(placeholder);

                if (oldIndex < targetIndex)
                {
                    targetIndex--;
                }

                container.Remove(placeholder);
                container.Remove(draggedElement);

                draggedElement.style.position = Position.Relative;
                draggedElement.style.top = StyleKeyword.Null;
                draggedElement.style.left = StyleKeyword.Null;
                draggedElement.style.width = StyleKeyword.Null;

                container.Insert(targetIndex, draggedElement);
            }

            draggedElement = null;
            placeholder = null;
            evt.StopPropagation();
        });
    }

    private void UpdateDraggedElementPosition(Vector2 mousePosition)
    {
        if (draggedElement == null) return;
        Vector2 localPos = container.WorldToLocal(mousePosition);
        draggedElement.style.left = localPos.x - (draggedElement.resolvedStyle.width / 2);
        draggedElement.style.top = localPos.y - (draggedElement.resolvedStyle.height / 2);
    }
}