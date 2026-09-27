#if UNITY_WEBGL

using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Assets.SimpleFileBrowserForWebGL
{
    /// <summary>
    /// Based on https://pixeleuphoria.com/blog/index.php/2020/04/27/unity-webgl-download-content/
    /// </summary>
    public class WebFileBrowser : MonoBehaviour
    {
        #if UNITY_EDITOR

        public static void Download(string fileName, byte[] bytes)
        {
            Debug.LogWarning("This is a stub for Editor. Native code works only inside WebGL builds.");
        }

        public static void Upload(Action<string, string, byte[]> callback, string fileExtension = "*")
        {
            Debug.LogWarning("This is a stub for Editor. Native code works only inside WebGL builds.");
        }

        #else

        [DllImport("__Internal")] private static extern void WebFileBrowserDownload(string fileName, string base64Data);
        [DllImport("__Internal")] private static extern string WebFileBrowserUpload(string extensionFilter, string receiverName, string methodName);

        private static WebFileBrowser _instance;
        private static Action<string, string, byte[]> _callback; // name, MIME, bytes.

        public void Awake()
        {
            _instance = this;
        }

        public static void Download(string fileName, byte[] bytes)
        {
            WebFileBrowserDownload(fileName, Convert.ToBase64String(bytes));
        }

        /// <summary>
        /// Extension examples: "*", ".txt", "image/*".
        /// </summary>
        public static void Upload(Action<string, string, byte[]> callback, string fileExtension = "*")
        {
            if (_instance == null)
            {
                _instance = new GameObject(typeof(WebFileBrowser).Name).AddComponent<WebFileBrowser>();
            }

            _callback = callback;
            WebFileBrowserUpload(fileExtension, _instance.name, "OnUpload");
        }

        public void OnUpload(string result)
        {
            var match = Regex.Match(result, "{\"name\":\"(.*?)\",\"result\":\"data:(.*?);base64,(.*)\"}");

            if (match.Success)
            {
                var fileName = match.Groups[1].Value;
                var mime = match.Groups[2].Value;
                var base64 = match.Groups[3].Value;
                var bytes = Convert.FromBase64String(base64);

                _callback?.Invoke(fileName, mime, bytes);
            }
        }

        #endif
    }
}

#endif