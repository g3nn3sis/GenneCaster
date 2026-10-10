using BepInEx;
using GorillaLocomotion;
using HarmonyLib;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

namespace GenneCaster.Base
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    internal class BaseLoader : BaseUnityPlugin
    {
        void Start()
        {
            try
            {
                HarmonyPatches.ApplyHarmonyPatches();
            }
            catch (Exception e) { UnityEngine.Debug.LogException(e); }
        }
        void OnDestroy()
        {
            try
            {
                HarmonyPatches.RemoveHarmonyPatches();
            }
            catch (Exception e) { UnityEngine.Debug.LogException(e); }
        }
        void OnApplicationQuit()
        {
            try
            {
                HarmonyPatches.RemoveHarmonyPatches();
            }
            catch (Exception e) { UnityEngine.Debug.LogException(e); }
        }
    }
    [HarmonyPatch(typeof(GTPlayer), "LateUpdate")]
    public class ObjectPatcher
    {
        internal static GameObject? LoaderObject;
        internal static GameObject? ObjectSubSection;
        public static GameObject? GetLoaderObject() => LoaderObject;
        public static GameObject? GetObjectSubSection() => ObjectSubSection;
        public static GameObject? SetParentObjectSubSection(GameObject obj)
        {
            if (!LoaderObject || !ObjectSubSection) return obj;
            obj.transform.SetParent(ObjectSubSection.transform);
            return obj;
        }
        public abstract class ObjectComponent : MonoBehaviour
        {
            protected virtual void Awake() => Console.WriteLine($"[{PluginInfo.Name}] {GetType().FullName} finished initializing.");
            protected virtual void OnDestroy() => Console.WriteLine($"[{PluginInfo.Name}] {GetType().FullName} destroyed for unknown reasons.");
            protected void DestroyClass() { if (this) Destroy(this); }
            protected async Task AsyncDestroyClass(Action<UnityAction> callback) { if (this) Destroy(this); }
            protected virtual void OnApplicationQuit() => Console.WriteLine($"[{PluginInfo.Name}] {GetType().FullName} removed, application quitting.");
            public ObjectComponent() { DontDestroyOnLoad(gameObject); Console.WriteLine($"[{PluginInfo.Name}] {GetType().FullName} started initializing."); }
        }
        static void Postfix(GTPlayer __instance)
        {
            if (LoaderObject != null) return;
            if (!__instance) return;
            __instance.inOverlay = true;
            try
            {
                var patchComponents = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsSubclassOf(typeof(ObjectComponent)) && !t.IsAbstract)
                .ToArray();
                LoaderObject = new GameObject($"_{PluginInfo.Name}", patchComponents);
                ObjectSubSection = new GameObject($"_{PluginInfo.Name}-ObjectHolder");
                ObjectSubSection.transform.SetParent(LoaderObject.transform);
            }
            catch (Exception e) { UnityEngine.Debug.LogException(e); }
        }

    }
}
