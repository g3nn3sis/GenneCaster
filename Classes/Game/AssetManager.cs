using GenneCaster.Classes.GameCamera;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using static GenneCaster.Base.ObjectPatcher;
namespace GenneCaster.Classes.Game
{
    internal class AssetManager : ObjectComponent
    {
        public static TMP_FontAsset[]? discoveredFonts;
        public static Texture2D? smallSteamTex;
        public static Texture2D? smallMetaTex;
        public static Sprite? smallMetaSprite;
        public static Sprite? smallSteamSprite;
        public AssetManager()
        {
            discoveredFonts = discoverFonts();
            loadIcons();
        }
        internal TMP_FontAsset[] discoverFonts()
        {
            if (discoveredFonts != null) return discoveredFonts;
            TMP_FontAsset[] loadedFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (loadedFonts.Length <= 0) return new TMP_FontAsset[0];

            List<TMP_FontAsset> clonedFonts = new List<TMP_FontAsset>();
            foreach (var font in loadedFonts)
            {
                TMP_FontAsset clone = UnityEngine.Object.Instantiate(font);
                DontDestroyOnLoad(clone);
                clone.name = $"_{font.name}";
                clonedFonts.Add(clone);
                Console.WriteLine($"Discovered font : {clone.name}, {clone.sourceFontFile}");
            }
            return clonedFonts.ToArray();
        }
        void loadIcons()
        {
            base.StartCoroutine(DownloadIcon("https://r2.gennesis.cc/c01b5c7cab9fc937.png", delegate (Texture2D tex)
            {
                smallSteamTex = tex;
                smallSteamSprite = Sprite.Create(
                smallSteamTex,
                new Rect(0f, 0f, smallSteamTex.width, smallSteamTex.height),
                new Vector2(0.5f, 0.5f)
                );
            }));
            base.StartCoroutine(DownloadIcon("https://r2.gennesis.cc/4819c378ef1b6814.png", delegate (Texture2D tex)
            {
                smallMetaTex = tex;
                smallMetaSprite = Sprite.Create(
                smallMetaTex,
                new Rect(0f, 0f, smallMetaTex.width, smallMetaTex.height),
                new Vector2(0.5f, 0.5f)
                );
            }));
        }
        IEnumerator DownloadIcon(string url, Action<Texture2D> callback)
        {
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
            {
                yield return uwr.SendWebRequest();
                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failure in downloading texture from {url}");
                    callback?.Invoke(Texture2D.grayTexture);
                }
                else
                {
                    var texture = DownloadHandlerTexture.GetContent(uwr);
                    callback?.Invoke(texture);
                }
            }
        }
        private IEnumerator LoadBundle(string url, string prefabName, Action<GameObject> onLoaded)
        {
            UnityWebRequest uwr = UnityWebRequestAssetBundle.GetAssetBundle(url);
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                yield break;
            }
            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(uwr);
            if (bundle == null)
            {
                yield break;
            }
            GameObject prefab = bundle.LoadAsset<GameObject>(prefabName);
            if (prefab == null)
            {
                yield break;
            }
            GameObject instance = Instantiate(prefab);
            onLoaded(instance);
        }
        private void SetStaticField(string fieldName, object value)
        {
            var field = typeof(AssetManager).GetField(fieldName);
            if (field != null) field.SetValue(null, value);
        }
        private T GetStaticField<T>(string fieldName) where T : class
        {
            var field = typeof(T).GetField(fieldName);
            return field != null ? field.GetValue(null) as T : null;
        }
    }
}