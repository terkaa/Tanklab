#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.Threading;

using BestHTTP;
using BestHTTP.Connections;
using BestHTTP.Extensions;
using BestHTTP.JSON.LitJson;
using BestHTTP.PlatformSupport.Memory;

using static BestHTTP.HTTPManager;

using BestMQTT.Packets;

namespace BestMQTT.Transports
{
    public sealed class SecureTCPTransport : Transport, IHeartbeat
    {
        /// <summary>
        /// Queue for sending packets
        /// </summary>
        private ConcurrentQueue<BufferSegment> _buffers = new ConcurrentQueue<BufferSegment>();

        /// <summary>
        /// Event to signal the sending thread.
        /// </summary>
        private AutoResetEvent _bufferAvailableEvent = new AutoResetEvent(false);

        /// <summary>
        /// TCP connector of the BestHTTP package to deal with name resolition, TLS, etc.
        /// </summary>
        private TCPConnector tcpConnection;

        private volatile bool closed;
        private volatile int runningThreadCount;

        public SecureTCPTransport(MQTTClient client)
            :base(client)
        {
            this.tcpConnection = new TCPConnector();
        }

        internal override void BeginConnect(CancellationToken token = default)
        {
            base.BeginConnect(token);

            if (token.IsCancellationRequested)
                return;

            if (this.State != TransportStates.Initial)
                throw new Exception($"{nameof(SecureTCPTransport)} couldn't {nameof(BeginConnect)} as it's already in state {this.State}!");

            Logger.Information(nameof(SecureTCPTransport), $"{nameof(BeginConnect)}", this.Context);

            BestHTTP.PlatformSupport.Threading.ThreadedRunner.RunLongLiving(ReceivingThread);
        }

        internal override void Send(BufferSegment buffer)
        {
            if (this.State != TransportStates.Connected)
            {
                Logger.Warning(nameof(SecureTCPTransport), $"Send called while it's not in the Connected state! State: {this.State}", this.Context);
                return;
            }

            Logger.Information(nameof(SecureTCPTransport), $"{nameof(Send)}({buffer})", this.Context);
            this._buffers.Enqueue(buffer);
            this._bufferAvailableEvent.Set();
        }

        internal override void BeginDisconnect()
        {
            if (this.State >= TransportStates.Disconnecting)
                return;

            ChangeStateTo(TransportStates.Disconnecting, string.Empty);
            try
            {
                this.closed = true;
                this._bufferAvailableEvent.Set();
            }
            catch
            {
                ChangeStateTo(TransportStates.Disconnected, string.Empty);
            }
        }

        private void ReceivingThread()
        {
            try
            {
                Interlocked.Increment(ref this.runningThreadCount);

                if (base.ConnectCancellationToken.IsCancellationRequested)
                {
                    this.transportEvents.Enqueue(new TransportEvent(TransportEventTypes.StateChange, TransportStates.DisconnectedWithError, "IsCancellationRequested"));
                    return;
                }

                var options = this.Parent.Options;
                Logger.Information(nameof(SecureTCPTransport), $"{nameof(ReceivingThread)} is up and running! runningThreadCount: {this.runningThreadCount}, options: {JsonMapper.ToJson(options)}", this.Context);

                // we cheat here a little: use wss or ws for secure or unsecure connections instead of https/http to avoid ALPN negotiation
                var request = new HTTPRequest(new Uri($"{(options.UseTLS ? "https" : "http")}://{options.Host}:{options.Port}/"));
                request.Context.Add("Transport", this.Context);

#if !BESTHTTP_DISABLE_PROXY
                if (request.Proxy is HTTPProxy)
                    request.Proxy = null;
#endif

                this.tcpConnection.Connect(request);

                if (base.ConnectCancellationToken.IsCancellationRequested)
                {
                    this.transportEvents.Enqueue(new TransportEvent(TransportEventTypes.StateChange, TransportStates.DisconnectedWithError, "IsCancellationRequested"));
                    return;
                }

                Logger.Information(nameof(SecureTCPTransport), $"{nameof(ReceivingThread)} - Connected", this.Context);

                this.transportEvents.Enqueue(new TransportEvent(TransportEventTypes.StateChange, TransportStates.Connected));

                BestHTTP.PlatformSupport.Threading.ThreadedRunner.RunLongLiving(SendThread);

                while (!closed)
                {
                    var buffer = BufferPool.Get(16 * 1024, true);
                    int count = this.tcpConnection.Stream.Read(buffer, 0, buffer.Length);
                    if (count == 0)
                    {
                        //this.closed = true;
                        //this._bufferAvailableEvent.Set();
                        //this.transportEvents.Enqueue(new TransportEvent(TransportEventTypes.StateChange, TransportStates.DisconnectedWithError, "TCP closed"));
                        //return;
                        throw new Exception("TCP connection closed unexpectedly!");
                    }
                    this.ReceiveStream.Write(new BufferSegment(buffer, 0, count));

                    Logger.Information(nameof(SecureTCPTransport), $"{nameof(ReceivingThread)} - Received ({count})", this.Context);

                    try
                    {
                        TryParseIncomingPackets();
                    }
                    catch (MQTTException ex)
                    {
                        this.transportEvents.Enqueue(new TransportEvent(TransportEventTypes.MQTTException, ex, nameof(TryParseIncomingPackets)));
                    }
                    catch (Exception ex)
                    {
                        Logger.Exception(nameof(SecureTCPTransport), $"{nameof(ReceivingThread)}.TryParseIncomingPackets", ex, this.Context);
                    }
                }
            }
            catch(Exception ex)
            {
                if (Logger.Level == BestHTTP.Logger.Loglevels.All && this.State < TransportStates.Disconnecting)
                    Logger.Exception(nameof(SecureTCPTransport), nameof(ReceivingThread), ex, this.Context);

                this.closed = true;
                this._bufferAvailableEvent.Set();
                this.transportEvents.Enqueue(new TransportEvent(TransportEventTypes.StateChange, TransportStates.DisconnectedWithError, ex.Message));
            }
            finally
            {
                Interlocked.Decrement(ref this.runningThreadCount);
                this.transportEvents.Enqueue(new TransportEvent(TransportEventTypes.StateChange, TransportStates.Disconnected));
            }
        }

        private void SendThread()
        {
            try
            {
                Interlocked.Increment(ref this.runningThreadCount);
                Logger.Information(nameof(SecureTCPTransport), $"{nameof(SendThread)} is up and running! runningThreadCount: {this.runningThreadCount}", this.Context);

                while (!closed)
                {
                    this._bufferAvailableEvent.WaitOne();
                    while (this._buffers.TryDequeue(out BufferSegment buff))
                    {
                        this.tcpConnection.Stream.Write(buff.Data, buff.Offset, buff.Count);
                        BufferPool.Release(buff);
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Exception(nameof(SecureTCPTransport), nameof(ReceivingThread), ex, this.Context);
            }
            finally
            {
                Interlocked.Decrement(ref this.runningThreadCount);
                this.transportEvents.Enqueue(new TransportEvent(TransportEventTypes.StateChange, TransportStates.Disconnected));
            }
        }

        // Call it every time we received a Disconnected event. This means that CleanupAfterDisconnect going to be called 2-3 times.
        protected override void CleanupAfterDisconnect()
        {
            // Run cleanup logic only when both threads are closed
            if (this.runningThreadCount == 0 && this._bufferAvailableEvent != null)
            {
                base.CleanupAfterDisconnect();

                this._bufferAvailableEvent?.Dispose();
                this._bufferAvailableEvent = null;

                this.tcpConnection?.Close();
                this.tcpConnection = null;

                Logger.Information(nameof(SecureTCPTransport), $"{nameof(CleanupAfterDisconnect)} finished!", this.Context);
            }
        }
    }
}

#endif
