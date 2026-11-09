using UnityEngine;

namespace GenneCaster.Base
{
    public abstract class ObjectComponent : MonoBehaviour
    {
        protected virtual void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Log.Debug($"{GetType().Name} initialized.");
        }

        protected virtual void OnDestroy() => Log.Debug($"{GetType().Name} destroyed.");

        protected virtual void OnApplicationQuit() { }
    }
}
