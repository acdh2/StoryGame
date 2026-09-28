using System.Collections.Generic;
using UnityEngine;

public class GameConfiguration : MonoBehaviour
{
    private bool isApplied = false;
    private Quaternion previousRotation = Quaternion.Euler(90, 45, 0);

    public List<AudioClip> musicTracks = new List<AudioClip>();
    public List<GameObject> playerModels = new List<GameObject>();

    //Properties start here
    public int selectedMusicIndex = 0; //bij -1 is er geen muziek
    public int selectedPlayerModelIndex = 0; //moet van 0 t/m count-1
    public bool fogEnabled = true;
    public Color fogColor = new Color(0.71f, 0.75f, 0.80f);
    public float fogDensity = 0.25f;
    public float timeHours = 6.0f;
    //Properties end here

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

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.loop = true;
    }

    public void Apply()
    {
        if (isApplied) return;
        isApplied = true;

        if (fogEnabled) {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;

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
        if (selectedPlayerModelIndex < 0) selectedPlayerModelIndex = 0;
        if (selectedPlayerModelIndex >= playerModels.Count) selectedPlayerModelIndex = playerModels.Count - 1;
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
            timeHours = timeHours
        };

        return JsonUtility.ToJson(data, true);
    }

    public void ImportFromJson(string json)
    {
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        if (data != null)
        {
            selectedMusicIndex = data.selectedMusicIndex;
            selectedPlayerModelIndex = data.selectedPlayerModelIndex;
            fogEnabled = data.fogEnabled;
            fogColor = new Color(data.fogColorR, data.fogColorG, data.fogColorB, data.fogColorA);
            fogDensity = data.fogDensity;
            timeHours = data.timeHours;
        }
    }
}