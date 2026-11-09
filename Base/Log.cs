using BepInEx.Logging;
using GenneCaster.Base;

namespace GenneCaster
{
    internal static class Log
    {
        private static ManualLogSource? source;
        private static readonly HashSet<string> reported = new HashSet<string>();

        internal static void Init(ManualLogSource logger) => source = logger;

        internal static void Debug(string message) => Write(LogLevel.Debug, message);
        internal static void Info(string message) => Write(LogLevel.Info, message);
        internal static void Warning(string message) => Write(LogLevel.Warning, message);
        internal static void Error(string message) => Write(LogLevel.Error, message);
        internal static void Exception(Exception e, string context) => Write(LogLevel.Error, $"{context}: {e}");

        internal static void WarningOnce(string key, string message)
        {
            if (Remember(key)) Write(LogLevel.Warning, message);
        }

        internal static void ExceptionOnce(string key, Exception e, string context)
        {
            if (Remember(key)) Exception(e, context);
        }

        internal static void Forget(string key)
        {
            lock (reported) reported.Remove(key);
        }

        private static bool Remember(string key)
        {
            lock (reported) return reported.Add(key);
        }

        private static void Write(LogLevel level, string message)
        {
            if (source != null)
            {
                source.Log(level, message);
                return;
            }
            string line = $"[{PluginInfo.Name}] {message}";
            if (level == LogLevel.Error) UnityEngine.Debug.LogError(line);
            else if (level == LogLevel.Warning) UnityEngine.Debug.LogWarning(line);
            else UnityEngine.Debug.Log(line);
        }
    }
}
