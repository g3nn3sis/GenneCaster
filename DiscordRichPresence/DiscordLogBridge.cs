using DiscordRPC.Logging;

namespace GenneCaster.DiscordRichPresence
{
    internal sealed class DiscordLogBridge : ILogger
    {
        public LogLevel Level { get; set; } = LogLevel.Warning;

        public void Trace(string message, params object[] args)
        {
            if (Level <= LogLevel.Trace) GenneCaster.Log.Debug(Format(message, args));
        }

        public void Info(string message, params object[] args)
        {
            if (Level <= LogLevel.Info) GenneCaster.Log.Debug(Format(message, args));
        }

        public void Warning(string message, params object[] args)
        {
            if (Level <= LogLevel.Warning) GenneCaster.Log.WarningOnce($"discord:{message}", Format(message, args));
        }

        public void Error(string message, params object[] args)
        {
            if (Level <= LogLevel.Error) GenneCaster.Log.WarningOnce($"discord:{message}", Format(message, args));
        }

        private static string Format(string message, object[] args) =>
            $"Discord RPC: {(args == null || args.Length == 0 ? message : string.Format(message, args))}";
    }
}
