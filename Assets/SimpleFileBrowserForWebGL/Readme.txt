Setup steps:
1) Move [Assets/SimpleFileBrowserForWebGL/WebGLTemplates] folder to [Assets] folder
2) Open [Player Setting/Player/Resolution and Presentation] and select [SimpleFileBrowserForWebGL] template

Alternatively, if you're using your custom WebGL template, just create a global variable [gameInstance] and initialize it inside [createUnityInstance] callback (refer to [SimpleFileBrowserForWebGL] template for code example).

If my WebGL template is outdated, copy the Default template from Unity installation folder, create a global variable [gameInstance] and initialize it inside [createUnityInstance] callback (refer to [SimpleFileBrowserForWebGL] template for code example).

Usage:
- WebFileBrowser.Download(string fileName, byte[] bytes)
- WebFileBrowser.Upload(Action<string, string, byte[]> callback, string fileExtension = "*")

Useful tips:
- Use [Encoding.Default.GetString] and [Encoding.Default.GetBytes] to convert strings to bytes and vise versa
- Use [System.Convert.ToBase64String] and [System.Convert.FromBase64String] to deal with Base64 strings