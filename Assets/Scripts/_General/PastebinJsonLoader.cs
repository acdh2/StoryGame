using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Runtime.InteropServices;

public class GistJsonLoader : MonoBehaviour
{
    #if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string GetGistId();
    #endif

    [SerializeField] private SceneManager sceneManager;

    private void Start()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
            string gistId = GetGistId();

            Debug.Log("Gist ID: " + gistId);

            if (!string.IsNullOrEmpty(gistId))
            {
                StartCoroutine(DownloadJsonFromGist(gistId));
            }
        #endif
    }

    private IEnumerator DownloadJsonFromGist(string gistId)
    {
        // GitHub Gist raw URL structuur
        string rawUrl = $"https://gist.githubusercontent.com/raw/{gistId}";

        using (UnityWebRequest request = UnityWebRequest.Get(rawUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Debug.Log("Ontvangen JSON: " + json);

                sceneManager?.LoadSceneFromJson(json);
                
                Navigator navigator = FindAnyObjectByType<Navigator>();
                if (navigator != null)
                {
                    navigator.StartGame();
                }
            }
            else
            {
                Debug.LogError($"Fout bij ophalen Gist: {request.error}");
            }
        }
    }
}