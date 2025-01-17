using System;
using System.Threading;

using BestHTTP;
using BestHTTP.JSON.LitJson;
using BestHTTP.PlatformSupport.Memory;
using BestHTTP.WebSocket;

using static BestHTTP.HTTPManager;

using BestMQTT.Packets;

namespace BestMQTT.Transports
{
    /// <summary>
    /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901285
    /// </summary>
    internal sealed class WebSocketTransport : Transport
    {
        private WebSocket webSocket;

        public WebSocketTransport(MQTTClient parent)
            : base(parent)
        {}

        internal override void BeginConnect(CancellationToken token = default)
        {
            base.BeginConnect(token);

            if (token.IsCancellationRequested)
                return;

            if (this.State != TransportStates.Initial)
                throw new Exception($"{nameof(WebSocketTransport)} couldn't {nameof(BeginConnect)} as it's already in state {this.State}!");

            var options = this.Parent.Options;

            Logger.Information(nameof(WebSocketTransport), $"{nameof(BeginConnect)} with options: {JsonMapper.ToJson(options)}", this.Context);

            string url = $"{(options.UseTLS ? "wss" : "ws")}://{options.Host}";

            if (options.Port > 0)
                url += $":{options.Port}{(options.Path.StartsWith("/") ? string.Empty : "/")}{options.Path ?? string.Empty}";

            this.webSocket = new WebSocket(new Uri(url), string.Empty, "mqtt");
            this.webSocket.OnOpen += WebSocket_OnOpen;
            this.webSocket.OnError += WebSocket_OnError;
            this.webSocket.OnClosed += WebSocket_OnClosed;
            this.webSocket.OnMessage += WebSocket_OnMessage;
            this.webSocket.OnBinary += WebSocket_OnBinary;
            this.webSocket.Open();
        }

        private void WebSocket_OnOpen(WebSocket webSocket) => this.transportEvents.Enqueue(new TransportEvent(TransportEventTypes.StateChange, TransportStates.Connected)); //ChangeStateTo(TransportStates.Connected, string.Empty);
        private void WebSocket_OnError(WebSocket webSocket, string reason) => this.transportEvents.Enqueue(new TransportEvent(TransportEventTypes.StateChange, TransportStates.DisconnectedWithError, reason));
        private void WebSocket_OnClosed(WebSocket webSocket, ushort code, string message) => this.transportEvents.Enqueue(new TransportEvent(TransportEventTypes.StateChange, TransportStates.Disconnected, message));
        private void WebSocket_OnMessage(WebSocket webSocket, string message) => ChangeStateTo(TransportStates.Disconnected, "Textual message received! message: " + message);

        private void WebSocket_OnBinary(WebSocket webSocket, byte[] data)
        {
            this.ReceiveStream.Write(new BufferSegment(data, 0, data.Length));

            try
            {
                TryParseIncomingPackets();
            }
            catch (MQTTException ex)
            {
                this.Parent.MQTTError(nameof(TryParseIncomingPackets), ex);
            }
            catch (Exception ex)
            {
                Logger.Exception(nameof(WebSocketTransport), $"{nameof(WebSocket_OnBinary)}.TryParseIncomingPackets", ex, this.Context);
            }
        }

        internal override void Send(BufferSegment buffer)
        {
            if (this.State != TransportStates.Connected)
            {
                Logger.Warning(nameof(WebSocketTransport), $"Send called while it's not in the Connected state! State: {this.State}", this.Context);
                return;
            }

            Logger.Information(nameof(WebSocketTransport), $"{nameof(Send)}({buffer})", this.Context);

            this.webSocket.Send(buffer.Data, (ulong)buffer.Offset, (ulong)buffer.Count);

            BufferPool.Release(buffer);
        }

        internal override void BeginDisconnect()
        {
            if (this.State >= TransportStates.Disconnecting)
                return;

            Logger.Information(nameof(WebSocketTransport), $"{nameof(BeginDisconnect)}", this.Context);

            this.ChangeStateTo(TransportStates.Disconnecting, string.Empty);
            this.webSocket.Close();
        }
    }
}
