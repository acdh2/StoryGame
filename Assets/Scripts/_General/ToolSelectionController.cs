using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ToolSelectorController : UIControllerBase
{
    [SerializeField] private ObjectSelector objectSelector;

    [Header("Tool Selector")]
    [SerializeField] private List<Texture2D> toolIcons = new List<Texture2D>();
    private int currentToolState = 0;
    private Button toolSelectorBtn;

    protected override void OnUIEnabled(VisualElement root)
    {
        toolSelectorBtn = root.Q<Button>("tool-selector");
        if (toolSelectorBtn != null)
        {
            toolSelectorBtn.clicked += OnToolSelectorClicked;
            UpdateToolButtonUI();
            ExecuteToolAction();
        }
    }

    protected override void OnUIDisabled()
    {
        if (toolSelectorBtn != null)
        {
            toolSelectorBtn.clicked -= OnToolSelectorClicked;
            toolSelectorBtn = null;
        }
    }

    private void OnToolSelectorClicked()
    {
        currentToolState = (currentToolState + 1) % 3;
        UpdateToolButtonUI();
        ExecuteToolAction();
    }

    private void UpdateToolButtonUI()
    {
        if (toolSelectorBtn == null) return;
        if (toolIcons != null && toolIcons.Count > currentToolState && toolIcons[currentToolState] != null)
        {
            toolSelectorBtn.style.backgroundImage = new StyleBackground(toolIcons[currentToolState]);
        }
    }

    private void ExecuteToolAction()
    {
        if (objectSelector != null)
        {
            switch (currentToolState)
            {
                case 0:
                    objectSelector.SetHandleType(0);
                    break;
                case 1:
                    objectSelector.SetHandleType(1);
                    break;
                case 2:
                    objectSelector.SetHandleType(2);
                    break;
            }
        }
    }
}