using GenneCaster.Base;

namespace GenneCaster.DiscordRichPresence
{
    internal sealed class DiscordRPCManager : ObjectComponent
    {
        internal static DiscordRpcWrapper? _rpc;
        internal const string discordBotId = "1361832683411607725";
        internal static bool ready;
        internal void Start()
        {
            _rpc = new DiscordRpcWrapper();
            _rpc.OnReady += (_, _) =>
            {
                Log.Forget("discord-error");
                Log.Info("Connected to Discord.");
                if (CasterConfig.DiscordPresence.Value) DiscordRPCStateManager.SetLoadingPresence();
                ready = true;
            };
            _rpc.OnError += msg => Log.WarningOnce("discord-error", $"Discord Rich Presence: {msg}");
            _rpc.Initialize(discordBotId);
        }

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            _rpc?.Dispose();
            _rpc = null;
        }
    }
}
