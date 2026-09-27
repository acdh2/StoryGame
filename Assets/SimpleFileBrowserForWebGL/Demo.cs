using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.SimpleFileBrowserForWebGL
{
    public class Demo : MonoBehaviour
    {
        public Text Text;
        public Image Image;

        #if UNITY_WEBGL

        public void Download()
        {
            WebFileBrowser.Download("Hello.txt", Encoding.Default.GetBytes("Hello!"));
        }

        public void UploadText()
        {
            WebFileBrowser.Upload((fileName, mime, bytes) => Text.text = Encoding.Default.GetString(bytes), ".txt");
        }

        public void UploadImage()
        {
            WebFileBrowser.Upload(OnUploadImage, "image/*"); // You can use MIME types.
        }

        private void OnUploadImage(string fileName, string mime, byte[] bytes)
        {
            var texture = new Texture2D(2, 2);

            texture.LoadImage(bytes);
            Text.text = $"{fileName} ({mime})";
            Image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one / 2);
        }

        #endif
    }
}