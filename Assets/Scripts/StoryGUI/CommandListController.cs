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
    public SceneManager sceneManager;

    [Header("UI Templates")]
    public VisualTreeAsset itemTemplate;
    public VisualTreeAsset newItemTemplate;

    private ScrollView scrollView;
    private VisualElement container;
    private VisualElement footerContainer;

    private VisualElement draggedElement = null;
    private VisualElement placeholder = null;
    private bool isDragging = false;
    private bool isRebuilding = false;

    protected override void OnUIEnabled(VisualElement root)
    {
        DisableEditorCamera();

        scrollView = root.Q<ScrollView>("list-scroll-view");
        if (scrollView == null) return;

        container = scrollView.contentContainer;
        footerContainer = root.Q<VisualElement>("list-footer");

        if (dataStore != null)
        {
            dataStore.OnDataChanged += RebuildUI;
            RebuildUI();
        }

        OnUIReady?.Invoke();
    }

    protected override void OnUIDisabled()
    {
        EnableEditorCamera();

        if (dataStore != null)
        {
            dataStore.OnDataChanged -= RebuildUI;
        }

        scrollView = null;
        container = null;
        footerContainer = null;
        draggedElement = null;
        placeholder = null;
    }

    private Color GetColorForCommand(string commandType)
    {
        string type = commandType?.Trim().ToUpperInvariant() ?? "";
        switch (type)
        {
            case "SAY": return new Color(0.2f, 0.4f, 0.6f);
            case "CHOICE": return new Color(0.2f, 0.6f, 0.4f);
            case "TOUCHED": return new Color(0.6f, 0.6f, 0.2f);
            case "SHOW":
            case "HIDE": return new Color(0.2f, 0.5f, 0.5f);
            case "GOTO":
            case "CHAPTER": return new Color(0.6f, 0.4f, 0.2f);
            case "GIVE":
            case "TAKE": return new Color(0.5f, 0.2f, 0.6f);
            case "HAS":
            case "HAS_NOT": return new Color(0.6f, 0.2f, 0.4f);
            default: return new Color(0.3f, 0.3f, 0.3f);
        }
    }

    private bool NeedsObjectParameter(string commandType)
    {
        string type = commandType?.Trim().ToUpperInvariant() ?? "";
        return type == "TOUCHED" || type == "SHOW" || type == "HIDE" || type == "TELEPORT";
    }

    private bool NeedsLabelParameter(string commandType)
    {
        string type = commandType?.Trim().ToUpperInvariant() ?? "";
        return type == "GOTO";
    }

    private List<string> GetAvailableChapters()
    {
        var chapters = new List<string>();
        if (dataStore == null || dataStore.Commands == null) return chapters;

        foreach (var cmd in dataStore.Commands)
        {
            if (cmd != null && (cmd.CommandType ?? "").Trim().ToUpperInvariant() == "CHAPTER")
            {
                string arg = (cmd.Argument ?? "").Trim();
                if (!string.IsNullOrEmpty(arg) && !chapters.Contains(arg))
                {
                    chapters.Add(arg);
                }
            }
        }
        return chapters;
    }

    private void UpdateCommandView(VisualElement itemRoot, string command)
    {
        TextField inputField = itemRoot.Q<TextField>("command-input");
        DropdownField objectDropdown = itemRoot.Q<DropdownField>("item-dropdown");
        DropdownField labelDropdown = itemRoot.Q<DropdownField>("label-dropdown") ?? itemRoot.Q<DropdownField>("chapter-dropdown");

        itemRoot.style.backgroundColor = GetColorForCommand(command);

        string type = command?.Trim().ToUpperInvariant() ?? "";
        
        if (inputField != null) inputField.style.display = DisplayStyle.None;
        if (objectDropdown != null) objectDropdown.style.display = DisplayStyle.None;
        if (labelDropdown != null) labelDropdown.style.display = DisplayStyle.None;

        if (NeedsObjectParameter(command))
        {
            if (objectDropdown != null) objectDropdown.style.display = DisplayStyle.Flex;
        }
        else if (NeedsLabelParameter(command))
        {
            if (labelDropdown != null) labelDropdown.style.display = DisplayStyle.Flex;
        }
        else
        {
            if (inputField != null) inputField.style.display = DisplayStyle.Flex;
        }
    }

private void RebuildUI()
{
    if (container == null || dataStore == null || isRebuilding) return;

    isRebuilding = true;

    container.Clear();
    if (footerContainer != null)
    {
        footerContainer.Clear();
    }

    List<string> commandOptions = dataStore.AvailableCommandTypes != null ? new List<string>(dataStore.AvailableCommandTypes) : new List<string> { "say" };
    List<string> chapters = GetAvailableChapters();
    List<string> objectNames = sceneManager != null ? sceneManager.GetUniqueObjectNames() : new List<string>();

    for (int i = 0; i < dataStore.Commands.Count; i++)
    {
        var data = dataStore.Commands[i];
        int index = i;

        TemplateContainer itemInstance = itemTemplate.Instantiate();
        VisualElement itemRoot = itemInstance.Q<VisualElement>("command-item-root") ?? itemInstance;

        DropdownField typeDropdown = itemRoot.Q<DropdownField>("command-dropdown");
        TextField inputField = itemRoot.Q<TextField>("command-input");
        DropdownField objectDropdown = itemRoot.Q<DropdownField>("item-dropdown");
        DropdownField labelDropdown = itemRoot.Q<DropdownField>("label-dropdown") ?? itemRoot.Q<DropdownField>("chapter-dropdown");

        string currentType = string.IsNullOrEmpty(data.CommandType) ? commandOptions[0] : data.CommandType;

        if (typeDropdown != null)
        {
            typeDropdown.choices = commandOptions;
            typeDropdown.value = currentType;
            
            typeDropdown.RegisterValueChangedCallback(evt =>
            {
                if (isRebuilding) return;
                string newType = evt.newValue;
                string targetArg = data.Argument;
                string upperType = newType?.Trim().ToUpperInvariant() ?? "";

                if (upperType == "GOTO")
                {
                    targetArg = chapters.Count > 0 ? chapters[0] : "";
                }
                else if (NeedsObjectParameter(newType))
                {
                    targetArg = objectNames.Count > 0 ? objectNames[0] : "";
                }
                else if (upperType == "KILL")
                {
                    targetArg = "";
                }

                dataStore.UpdateCommand(index, newType, targetArg);
                UpdateCommandView(itemRoot, newType);
            });
        }

        if (NeedsObjectParameter(currentType))
        {
            if (objectDropdown != null)
            {
                objectDropdown.choices = objectNames;
                if (objectNames.Count > 0)
                {
                    if (!string.IsNullOrEmpty(data.Argument) && objectNames.Contains(data.Argument))
                    {
                        objectDropdown.value = data.Argument;
                    }
                    else
                    {
                        objectDropdown.value = objectNames[0];
                        dataStore.UpdateCommand(index, currentType, objectNames[0], true);
                    }
                }
                else
                {
                    objectDropdown.value = "";
                }

                objectDropdown.RegisterValueChangedCallback(evt =>
                {
                    if (isRebuilding) return;
                    dataStore.UpdateCommand(index, typeDropdown != null ? typeDropdown.value : currentType, evt.newValue, true);
                });
            }
        }
        else if (NeedsLabelParameter(currentType))
        {
            if (labelDropdown != null)
            {
                labelDropdown.choices = chapters;
                if (chapters.Count > 0)
                {
                    if (!string.IsNullOrEmpty(data.Argument) && chapters.Contains(data.Argument))
                    {
                        labelDropdown.value = data.Argument;
                    }
                    else
                    {
                        labelDropdown.value = chapters[0];
                        dataStore.UpdateCommand(index, currentType, chapters[0], true);
                    }
                }
                else
                {
                    labelDropdown.value = "";
                }

                labelDropdown.RegisterValueChangedCallback(evt =>
                {
                    if (isRebuilding) return;
                    dataStore.UpdateCommand(index, typeDropdown != null ? typeDropdown.value : currentType, evt.newValue, true);
                });
            }
        }
        else
        {
            if (inputField != null)
            {
                inputField.value = data.Argument;
                inputField.RegisterValueChangedCallback(evt =>
                {
                    if (isRebuilding) return;
                    dataStore.UpdateCommand(index, typeDropdown != null ? typeDropdown.value : currentType, evt.newValue, false);
                });
                inputField.RegisterCallback<FocusOutEvent>(evt =>
                {
                    if (isRebuilding) return;
                    dataStore.UpdateCommand(index, typeDropdown != null ? typeDropdown.value : currentType, inputField.value, true);
                });                
                
                inputField.RegisterCallback<NavigationMoveEvent>(evt => evt.StopPropagation(), TrickleDown.TrickleDown);
                inputField.RegisterCallback<NavigationSubmitEvent>(evt => evt.StopPropagation(), TrickleDown.TrickleDown);
                inputField.RegisterCallback<NavigationCancelEvent>(evt => evt.StopPropagation(), TrickleDown.TrickleDown);
            }
        }

        RegisterDragEvents(itemRoot, index);
        UpdateCommandView(itemRoot, currentType);
        container.Add(itemRoot);
    }

    if (newItemTemplate != null)
    {
        VisualElement addItemRoot = newItemTemplate.Instantiate();
        addItemRoot.RegisterCallback<ClickEvent>(evt =>
        {
            dataStore.AddCommand();
        });

        if (footerContainer != null)
        {
            footerContainer.Add(addItemRoot);
        }
    }

    isRebuilding = false;
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
