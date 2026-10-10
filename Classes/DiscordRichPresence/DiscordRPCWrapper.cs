using System.Collections.Concurrent;
using DiscordRPC;
using DiscordRPC.Logging;

namespace GenneCaster.Classes.DiscordRichPresence
{
    public sealed class DiscordRpcWrapper : IDisposable
    {
        private DiscordRpcClient? _client;
        private readonly object _sync = new object();
        private CancellationTokenSource? _cts;
        private Task? _worker;
        private readonly ConcurrentQueue<Action> _presenceQueue = new();
        private bool _disposed;
        private bool _isConnecting;
        private int _reconnectAttempt;

        public string ApplicationId { get; private set; } = string.Empty;
        public string Username { get; private set; } = "Unknown";
        public string UserId { get; private set; } = "Unknown";
        public bool IsConnected => _client is { IsInitialized: true } && !_disposed;

        public event Action<string, string>? OnReady;
        public event Action<string>? OnError;
        public event Action? OnDisconnected;

        private readonly DateTime _sessionStart = DateTime.UtcNow;

        public bool Initialize(string applicationId, int pipe = 0)
        {
            lock (_sync)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(DiscordRpcWrapper));

                if (_worker != null)
                    return false;

                ApplicationId = applicationId;

                _cts = new CancellationTokenSource();
                _worker = Task.Run(() => WorkerLoop(pipe, _cts.Token));

                return true;
            }
        }

        private async Task WorkerLoop(int pipe, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_client == null || !_client.IsInitialized)
                    {
                        await TryConnect(pipe);
                    }

                    ProcessQueue();
                    await Task.Delay(500, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex.Message);
                    await HandleReconnectDelay(token);
                }
            }
        }

        private async Task TryConnect(int pipe)
        {
            if (_isConnecting || _disposed)
                return;

            _isConnecting = true;

            try
            {
                _client?.Dispose();

                _client = new DiscordRpcClient(ApplicationId, pipe)
                {
                    Logger = new ConsoleLogger
                    {
                        Level = LogLevel.Warning
                    }
                };

                _client.OnReady += (_, e) =>
                {
                    Username = e.User.Username;
                    UserId = e.User.ID.ToString();
                    _reconnectAttempt = 0;

                    OnReady?.Invoke(Username, UserId);
                };

                _client.OnError += (_, e) =>
                    OnError?.Invoke(e.Message);

                _client.OnConnectionFailed += (_, _) =>
                    OnDisconnected?.Invoke();

                _client.Initialize();
                _reconnectAttempt = 0;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
                await HandleReconnectDelay(CancellationToken.None);
            }
            finally
            {
                _isConnecting = false;
            }
        }

        private async Task HandleReconnectDelay(CancellationToken token)
        {
            _reconnectAttempt++;

            int delay = Math.Min(
                30000,
                1000 * (int)Math.Pow(2, _reconnectAttempt)
            );

            await Task.Delay(delay, token);
        }

        private void ProcessQueue()
        {
            if (!IsConnected || _client == null)
                return;

            while (_presenceQueue.TryDequeue(out var action))
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex.Message);
                }
            }
        }

        private readonly RichPresence _currentPresence = new()
        {
            Assets = new Assets(),
            Party = new Party(),
            Buttons = Array.Empty<Button>(),
            Timestamps = new Timestamps()
        };

        public void UpdatePresence(Action<RichPresence> updateAction)
        {
            _presenceQueue.Enqueue(() =>
            {
                if (!IsConnected || _client == null)
                    return;

                try
                {
                    updateAction(_currentPresence);

                    _currentPresence.Timestamps ??= new Timestamps();

                    if (_currentPresence.Timestamps.Start == null)
                        _currentPresence.Timestamps.Start = _sessionStart;

                    _client.SetPresence(_currentPresence);
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex.Message);
                }
            });
        }

        public void UpdatePresence(
            string? details = null,
            string? state = null,
            string? largeImageKey = null,
            string? largeImageText = null,
            string? smallImageKey = null,
            string? smallImageText = null,
            string? partyId = null,
            int? partyMax = null,
            int? partySize = null,
            Button[]? buttons = null)
        {
            UpdatePresence(p =>
            {
                if (details != null)
                    p.Details = details;

                if (state != null)
                    p.State = state;

                p.Assets ??= new Assets();
                p.Party ??= new Party();

                if (largeImageKey != null)
                    p.Assets.LargeImageKey = largeImageKey;

                if (largeImageText != null)
                    p.Assets.LargeImageText = largeImageText;

                if (smallImageKey != null)
                    p.Assets.SmallImageKey = smallImageKey;

                if (smallImageText != null)
                    p.Assets.SmallImageText = smallImageText;

                if (partyId != null)
                    p.Party.ID = partyId;

                if (partyMax != null)
                    p.Party.Max = partyMax.Value;

                if (partySize != null)
                    p.Party.Size = partySize.Value;

                if (buttons != null)
                    p.Buttons = buttons;
            });
        }

        public void ClearPresence()
        {
            if (!IsConnected || _client == null)
                return;

            try
            {
                _client.ClearPresence();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
            }
        }

        public void Shutdown()
        {
            lock (_sync)
            {
                if (_disposed)
                    return;

                try
                {
                    _cts?.Cancel();
                    _client?.ClearPresence();
                    _client?.Dispose();
                    _client = null;

                    Username = "Unknown";
                    UserId = "Unknown";
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex.Message);
                }
            }
        }

        public void Dispose()
        {
            Shutdown();

            _disposed = true;

            try
            {
                _cts?.Dispose();
            }
            catch
            {
            }

            GC.SuppressFinalize(this);
        }
    }
}