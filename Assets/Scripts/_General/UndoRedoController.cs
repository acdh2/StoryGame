using UnityEngine;
using UnityEngine.UIElements;

public class UndoRedoController : UIControllerBase
{
    [SerializeField] private SceneManager sceneManager;

    protected override void Awake()
    {
        base.Awake();
        isInitialised = true;
    }

    protected override void OnUIEnabled(VisualElement root)
    {
        var undoButton = root.Q<Button>("undo");
        if (undoButton != null && sceneManager != null)
        {
            undoButton.clicked += sceneManager.Undo;
        }

        var redoButton = root.Q<Button>("redo");
        if (redoButton != null && sceneManager != null)
        {
            redoButton.clicked += sceneManager.Redo;
        }
    }

    protected override void OnUIDisabled()
    {
        if (RootElement != null)
        {
            var undoButton = RootElement.Q<Button>("undo");
            if (undoButton != null && sceneManager != null)
            {
                undoButton.clicked -= sceneManager.Undo;
            }

            var redoButton = RootElement.Q<Button>("redo");
            if (redoButton != null && sceneManager != null)
            {
                redoButton.clicked -= sceneManager.Redo;
            }
        }
    }
}