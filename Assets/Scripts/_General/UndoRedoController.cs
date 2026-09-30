using UnityEngine;
using UnityEngine.UIElements;

public class UndoRedoController : UIControllerBase
{
    [SerializeField] private SceneManager sceneManager;

    private Button undoButton;
    private Button redoButton;

    protected override void OnUIEnabled(VisualElement root)
    {
        undoButton = root.Q<Button>("undo");
        if (undoButton != null && sceneManager != null)
        {
            undoButton.clicked += sceneManager.Undo;
        }

        redoButton = root.Q<Button>("redo");
        if (redoButton != null && sceneManager != null)
        {
            redoButton.clicked += sceneManager.Redo;
        }
    }

    protected override void OnUIDisabled()
    {
        if (undoButton != null && sceneManager != null)
        {
            undoButton.clicked -= sceneManager.Undo;
        }

        if (redoButton != null && sceneManager != null)
        {
            redoButton.clicked -= sceneManager.Redo;
        }

        undoButton = null;
        redoButton = null;
    }

    private void Update()
    {
        if (sceneManager == null)
            return;

        if (undoButton != null)
        {
            bool canUndo = sceneManager.CanUndo;
            undoButton.style.opacity = canUndo ? 1f : 0.5f;
            undoButton.SetEnabled(canUndo);
        }

        if (redoButton != null)
        {
            bool canRedo = sceneManager.CanRedo;
            redoButton.style.opacity = canRedo ? 1f : 0.5f;
            redoButton.SetEnabled(canRedo);
        }
    }
}