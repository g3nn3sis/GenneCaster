using GenneCaster.Classes.GameCamera;
using TMPro;
using UnityEngine;

namespace GenneCaster.Classes.Game
{
    internal class ObjectWrappers
    {
        public static TextMeshPro CreateText(GameObject parent, string name, Vector3 localPos, string text, TMP_FontAsset font, float fontSize)
        {
            var go = new GameObject($"_{name}");
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = localPos;
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.useGUILayout = true;
            tmp.outlineColor = Color.black;
            go.transform.rotation = Quaternion.LookRotation(CameraManager.camera?.Camera.transform.forward ?? Vector3.forward);
            tmp.font = font;
            UnityEngine.Object.Destroy(go, Time.deltaTime);

            return tmp;
        }

        public static SpriteRenderer CreateSprite(GameObject parent, string name, Sprite sprite, Vector3 localPos, Vector3 localScale, Vector3 transformFordward)
        {
            var go = new GameObject($"_{name}");
            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            go.transform.SetParent(parent.transform);
            ApplySprite(go, sprite);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.transform.rotation = Quaternion.LookRotation(transformFordward);
            UnityEngine.Object.Destroy(go, Time.deltaTime);

            return spriteRenderer;
        }
        static void ApplySprite(GameObject imageObj, Sprite sprite2)
        {
            var components = imageObj.GetComponents(typeof(SpriteRenderer));
            if (components == null) imageObj.AddComponent<SpriteRenderer>();
            if (imageObj != null)
            {
                var component = imageObj.GetComponent<SpriteRenderer>();
                if (component != null) component.sprite = sprite2;
            }
        }
    }
}
