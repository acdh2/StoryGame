using UnityEngine;

public class InputManager : MonoBehaviour
{
    private static InputManager _instance;
    private static bool isQuitting = false;

    public static InputManager Instance
    {
        get
        {
            if (isQuitting) return null;

            if (_instance == null)
            {
                _instance = FindFirstObjectByType<InputManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("InputManager");
                    _instance = go.AddComponent<InputManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    public InputSystem_Actions Actions { get; private set; }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (Actions == null)
        {
            Actions = new InputSystem_Actions();
        }
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    public void EnableMap(string mapName)
    {
        Actions?.asset.FindActionMap(mapName)?.Enable();
    }

    public void DisableMap(string mapName)
    {
        Actions?.asset.FindActionMap(mapName)?.Disable();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            Actions?.Dispose();
            _instance = null;
        }
    }
}