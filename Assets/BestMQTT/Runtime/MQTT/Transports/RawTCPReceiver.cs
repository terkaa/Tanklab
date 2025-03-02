using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using BestHTTP.PlatformSupport.Memory;

using BestMQTT.Packets;

using static BestHTTP.HTTPManager;

namespace BestMQTT.Transports
{
    internal class RawTCPReceiver
    {
        public Socket Socket { get; private set; }
        public int ReadBufferSize { get; private set; }

        private RawTCPTransport _parent;

        public RawTCPReceiver(RawTCPTransport parent, Socket socket, int readBufferSize, ConcurrentQueue<Packet> parsePacketsInto)
        {
            this._parent = parent;
            this.Socket = socket;
            this.ReadBufferSize = readBufferSize;
        }

        public void StartReceive()
        {
            var buffer = BufferPool.Get(this.ReadBufferSize, true);

            try
            {
                Logger.Information(nameof(RawTCPReceiver), $"{nameof(StartReceive)}: Preparing for data! Buffer size: {buffer.Length:N0}", this._parent.Context);

                this.Socket.BeginReceive(buffer,
                                         0,
                                         buffer.Length,
                                         SocketFlags.None,
                                         OnReceiveFinished,
                                         buffer);
            }
            catch (Exception ex)
            {
                BufferPool.Release(buffer);

                Logger.Exception(nameof(RawTCPReceiver), nameof(StartReceive), ex, this._parent.Context);
            }
        }

        private void OnReceiveFinished(IAsyncResult ar)
        {
            byte[] buffer = ar.AsyncState as byte[];
            try
            {
                int readCount = this.Socket.EndReceive(ar);

                Logger.Information(nameof(RawTCPReceiver), $"{nameof(OnReceiveFinished)}: Received {readCount:N0} bytes!", this._parent.Context);

                if (readCount > 0)
                {
                    this._parent.ReceiveStream.Write(new BufferSegment(buffer, 0, readCount));

                    try
                    {
                        this._parent.TryParseIncomingPackets();
                    }
                    catch (MQTTException ex)
                    {
                        this._parent.EnqueueTransportEvent(new TransportEvent(TransportEventTypes.MQTTException, ex, nameof(this._parent.TryParseIncomingPackets)));
                    }
                    catch (Exception ex)
                    {
                        Logger.Exception(nameof(RawTCPReceiver), $"{nameof(OnReceiveFinished)}.TryParseIncomingPackets", ex, this._parent.Context);
                    }

                    this.StartReceive();
                }
                else
                {
                    this._parent.EnqueueTransportEvent(new TransportEvent(TransportEventTypes.StateChange, TransportStates.Disconnected));

                    BufferPool.Release(buffer);

                    // error
                    this.Socket.Close();
                }
            }
            catch (Exception ex)
            {
                BufferPool.Release(buffer);

                if (this._parent.State < TransportStates.Disconnecting)
                    Logger.Exception(nameof(RawTCPReceiver), nameof(OnReceiveFinished), ex, this._parent.Context);
            }
        }
    }
}
