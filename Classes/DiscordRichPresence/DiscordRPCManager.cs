using static GenneCaster.Base.ObjectPatcher;

namespace GenneCaster.Classes.DiscordRichPresence
{
    public class DiscordRPCManager : ObjectComponent
    {
        public static DiscordRpcWrapper? _rpc;
        internal const string discordBotId = "1361832683411607725";
        public static bool ready;
        internal void Start()
        {
            _rpc = new DiscordRpcWrapper();
            _rpc.Initialize(discordBotId);
            _rpc.OnReady += (_, _) =>
            {
                DiscordRPCStateManager.SetLoadingPresence();
                ready = true;
            };
            _rpc.OnError += msg =>
            {
                UnityEngine.Debug.Log($"[DiscordRPC] {msg}");
            };
        }
        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            _rpc?.Dispose();
            _rpc = null;
        }
    }
}
