using UnityEngine;

public static class MaterialPreviewGenerator
{
    public static Texture2D CreatePreview(Material material, int resolution = 128)
    {
        if (material == null) return null;

        // Maak een tijdelijke camera en lichtbron aan
        GameObject cameraObj = new GameObject("MaterialPreviewCamera");
        Camera cam = cameraObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0); // Transparante achtergrond
        cam.cullingMask = 1 << 31; // Gebruik een ongebruikte/tijdelijke layer

        GameObject lightObj = new GameObject("MaterialPreviewLight");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

        // Maak een preview sphere
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.layer = 31;
        sphere.GetComponent<Renderer>().sharedMaterial = material;

        // Positioneer camera
        sphere.transform.position = Vector3.zero;
        cam.transform.position = new Vector3(0, 0, -2.5f);
        cam.transform.LookAt(sphere.transform);

        // Render naar Render Texture
        RenderTexture rt = RenderTexture.GetTemporary(resolution, resolution, 16, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();

        // Lees pixels uit naar Texture2D
        RenderTexture.active = rt;
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        texture.Apply();

        // Opruimen
        RenderTexture.active = null;
        cam.targetTexture = null;
        RenderTexture.ReleaseTemporary(rt);
        Object.DestroyImmediate(sphere);
        Object.DestroyImmediate(lightObj);
        Object.DestroyImmediate(cameraObj);

        return texture;
    }
}