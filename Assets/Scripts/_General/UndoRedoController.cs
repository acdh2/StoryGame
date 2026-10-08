using Unity.VectorGraphics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class UndoRedoController : UIControllerBase, IKeyEventReceiver
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

    private void OnKeyDown(KeyDownEvent evt)
    {
        if (evt.actionKey && evt.keyCode == KeyCode.Z)
        {
            if (sceneManager != null) sceneManager.Undo();
            evt.StopPropagation();
        }
        if (evt.actionKey && evt.keyCode == KeyCode.Y)
        {
            if (sceneManager != null) sceneManager.Redo();
            evt.StopPropagation();
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

    public void OnKeyEvent(string identifier)
    {
        if (!gameObject.activeSelf) return;
        if (identifier == "Undo") if (sceneManager != null) sceneManager.Undo();
        if (identifier == "Redo") if (sceneManager != null) sceneManager.Redo();
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