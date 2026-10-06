using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using SFB;
#if UNITY_WEBGL && !UNITY_EDITOR
    using Assets.SimpleFileBrowserForWebGL;
#endif

public class FileManager : UIControllerBase
{
    [SerializeField] private SceneSaveSystem saveSystem;
    [SerializeField] private Transform sceneRoot;
    [SerializeField] private EditorCameraController editorCameraController;

    private Button newBtn;
    private Button loadBtn;
    private Button saveBtn;

    protected override void OnUIEnabled(VisualElement root)
    {
        newBtn = root.Q<Button>("btn-new");
        if (newBtn != null)
        {
            newBtn.clicked += OnNewClicked;
        }

        loadBtn = root.Q<Button>("btn-load");
        if (loadBtn != null)
        {
            loadBtn.clicked += OnLoadClicked;
        }

        saveBtn = root.Q<Button>("btn-save");
        if (saveBtn != null)
        {
            saveBtn.clicked += OnSaveClicked;
        }
    }

    protected override void OnUIDisabled()
    {
        
        if (newBtn != null)
        {
            newBtn.clicked -= OnNewClicked;
            newBtn = null;
        }

        if (loadBtn != null)
        {
            loadBtn.clicked -= OnLoadClicked;
            loadBtn = null;
        }

        if (saveBtn != null)
        {
            saveBtn.clicked -= OnSaveClicked;
            saveBtn = null;
        }
    }

    private void OnNewClicked()
    {
        if (saveSystem != null)
        {
            saveSystem.NewScene();
            ResetView();
        }
    }

    private void OnLoadClicked()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebFileBrowser.Upload((fileName, mime, bytes) => {
            if (bytes != null)
            {
                string json = System.Text.Encoding.UTF8.GetString(bytes);
                if (saveSystem != null && sceneRoot != null)
                {
                    saveSystem.LoadSceneFromJson(json);
                    ResetView();
                }
            }
        }, ".json");
#else
        var extensions = new[] {
            new ExtensionFilter("JSON Scene", "json"),
            new ExtensionFilter("All Files", "*"),
        };

        var paths = StandaloneFileBrowser.OpenFilePanel("Open Scene", "", extensions, false);
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            string json = File.ReadAllText(paths[0]);
            if (saveSystem != null && sceneRoot != null)
            {
                saveSystem.LoadSceneFromJson(json);
                ResetView();
            }
        }
#endif
    }

    private void OnSaveClicked()
    {
        if (saveSystem == null || sceneRoot == null) return;

        string json = saveSystem.SerializeScene();

#if UNITY_WEBGL && !UNITY_EDITOR
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
        WebFileBrowser.Download("game.json", bytes);
#else
        var extensions = new[] {
            new ExtensionFilter("JSON Scene", "json"),
        };

        var path = StandaloneFileBrowser.SaveFilePanel("Save Game", "", "game", extensions);
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, json);
        }
#endif
    }

    private void ResetView()
    {
        if (editorCameraController != null)
        {
            editorCameraController.ResetView();
        }
    }
}