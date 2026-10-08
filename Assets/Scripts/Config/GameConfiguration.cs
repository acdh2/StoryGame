using System.Collections.Generic;
using UnityEngine;

public class GameConfiguration : MonoBehaviour
{
    public event System.Action OnSettingChanged;
    public event System.Action OnInvalidateSettings;

    private bool isApplied = false;
    private Quaternion previousRotation = Quaternion.Euler(90, 45, 0);

    public List<AudioClip> musicTracks = new List<AudioClip>();
    public List<GameObject> playerModels = new List<GameObject>();

    private int _selectedMusicIndex = -1;
    public int selectedMusicIndex
    {
        get => _selectedMusicIndex;
        set
        {
            if (_selectedMusicIndex != value)
            {
                _selectedMusicIndex = value;
                OnSettingChanged?.Invoke();
            }
        }
    }

    private int _selectedPlayerModelIndex = 0;
    public int selectedPlayerModelIndex
    {
        get => _selectedPlayerModelIndex;
        set
        {
            if (_selectedPlayerModelIndex != value)
            {
                _selectedPlayerModelIndex = value;
                OnSettingChanged?.Invoke();
            }
        }
    }

    private bool _fogEnabled = false;
    public bool fogEnabled
    {
        get => _fogEnabled;
        set
        {
            if (_fogEnabled != value)
            {
                _fogEnabled = value;
                OnSettingChanged?.Invoke();
            }
        }
    }

    private Color _fogColor = Color.white;//new Color(0.71f, 0.75f, 0.80f);
    public Color fogColor
    {
        get => _fogColor;
        set
        {
            if (_fogColor != value)
            {
                _fogColor = value;
                OnSettingChanged?.Invoke();
            }
        }
    }

    private float _fogDensity = 0.5f;
    public float fogDensity
    {
        get => _fogDensity;
        set
        {
            if (!Mathf.Approximately(_fogDensity, value))
            {
                _fogDensity = value;
                OnSettingChanged?.Invoke();
            }
        }
    }

    private bool _dayTime = true;
    public bool dayTime
    {
        get => _dayTime;
        set
        {
            if (_dayTime != value)
            {
                _dayTime = value;
                OnSettingChanged?.Invoke();
            }
        }
    }

    private AudioSource audioSource;

    [System.Serializable]
    private class SaveData
    {
        public int selectedMusicIndex;
        public int selectedPlayerModelIndex;
        public bool fogEnabled;
        public float fogColorR;
        public float fogColorG;
        public float fogColorB;
        public float fogColorA;
        public float fogDensity;
        public float timeHours;
    }

    private SaveData defaultSettings;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.loop = true;

        defaultSettings = new SaveData
        {
            selectedMusicIndex = -1,
            selectedPlayerModelIndex = 0,
            fogEnabled = false,
            fogColorR = 1,
            fogColorG = 1,
            fogColorB = 1,
            fogColorA = 1,
            fogDensity = 0.75f,
            timeHours = 12
        };
    }

    public void Reset()
    {        
        if (defaultSettings == null) return;

        _selectedMusicIndex = defaultSettings.selectedMusicIndex;
        _selectedPlayerModelIndex = defaultSettings.selectedPlayerModelIndex;
        _fogEnabled = defaultSettings.fogEnabled;
        _fogColor = new Color(defaultSettings.fogColorR, defaultSettings.fogColorG, defaultSettings.fogColorB, defaultSettings.fogColorA);
        _fogDensity = defaultSettings.fogDensity;
        _dayTime = true;

        OnInvalidateSettings?.Invoke();
    }

    public void Apply()
    {
        if (isApplied) return;
        isApplied = true;

        if (fogEnabled) {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity * 0.75f;

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = fogColor;
            }
        }

        Light dirLight = RenderSettings.sun;
        if (dirLight != null)
        {
            float timeHours = dayTime?12:0;
            float rotationAngle = (timeHours / 24.0f) * 360.0f - 90.0f;
            previousRotation = dirLight.transform.rotation;
            dirLight.transform.rotation = Quaternion.Euler(rotationAngle, 45.0f, 0.0f);
        }

        if (musicTracks != null && selectedMusicIndex >= 0 && selectedMusicIndex < musicTracks.Count)
        {
            if (audioSource != null && musicTracks[selectedMusicIndex] != null)
            {
                audioSource.clip = musicTracks[selectedMusicIndex];
                audioSource.Play();
            }
        }
    }

    public void Unapply()
    {
        if (!isApplied) return;
        
        RenderSettings.fog = false;
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.Skybox;
        }
        Light dirLight = RenderSettings.sun;
        if (dirLight != null)
        {
            dirLight.transform.rotation = previousRotation;
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        isApplied = false;
    }

    public GameObject GetPlayerPrefab()
    {
        if (selectedPlayerModelIndex < 0) _selectedPlayerModelIndex = 0;
        if (selectedPlayerModelIndex >= playerModels.Count) _selectedPlayerModelIndex = playerModels.Count - 1;
        return playerModels[selectedPlayerModelIndex];
    }

    public string ExportToJson()
    {
        SaveData data = new SaveData
        {
            selectedMusicIndex = selectedMusicIndex,
            selectedPlayerModelIndex = selectedPlayerModelIndex,
            fogEnabled = fogEnabled,
            fogColorR = fogColor.r,
            fogColorG = fogColor.g,
            fogColorB = fogColor.b,
            fogColorA = fogColor.a,
            fogDensity = fogDensity,
            timeHours = dayTime?12:0
        };

        return JsonUtility.ToJson(data, true);
    }

    public void ImportFromJson(string json)
    {
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        if (data != null)
        {
            _selectedMusicIndex = data.selectedMusicIndex;
            _selectedPlayerModelIndex = data.selectedPlayerModelIndex;
            _fogEnabled = data.fogEnabled;
            _fogColor = new Color(data.fogColorR, data.fogColorG, data.fogColorB, data.fogColorA);
            _fogDensity = data.fogDensity;
            _dayTime = (data.timeHours > 6f);
            
            OnInvalidateSettings?.Invoke();
        }
    }
}