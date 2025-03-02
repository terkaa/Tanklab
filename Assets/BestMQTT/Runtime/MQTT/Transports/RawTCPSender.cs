using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;

using BestHTTP.Extensions;
using BestHTTP.PlatformSupport.Memory;

using static BestHTTP.HTTPManager;

namespace BestMQTT.Transports
{
    internal sealed class RawTCPSender
    {
        public Socket Socket { get; private set; }
        public int InitialWriteBufferSize { get; private set; }

        private RawTCPTransport _parent;
        private ConcurrentQueue<BufferSegment> _buffers = new ConcurrentQueue<BufferSegment>();
        private volatile int _isSendInProgress = 0;

        public RawTCPSender(RawTCPTransport parent, Socket socket, int initialWriteBufferSize)
        {
            this._parent = parent;
            this.Socket = socket;
            this.InitialWriteBufferSize = initialWriteBufferSize;
        }

        public void Send(BufferSegment buffer)
        {
            this._buffers.Enqueue(buffer);

            if (Interlocked.CompareExchange(ref this._isSendInProgress, 1, 0) == 0)
                this.BeginSend();
        }

        private void BeginSend()
        {
            var initialBuffer = BufferPool.Get(this.InitialWriteBufferSize, true);

            try
            {
                using (var ms = new BufferPoolMemoryStream(initialBuffer, 0, initialBuffer.Length, true, true, false, true))
                {
                    while (this._buffers.TryDequeue(out var buffer))
                    {
                        ms.Write(buffer.Data, buffer.Offset, buffer.Count);

                        BufferPool.Release(buffer);
                    }

                    var sendBuffer = ms.GetBuffer();

                    Logger.Information(nameof(RawTCPSender), $"{nameof(BeginSend)}: Sending {ms.Position:N0} bytes...", this._parent.Context);

                    this.Socket.BeginSend(sendBuffer, 0, (int)ms.Position, SocketFlags.None, OnSendFinished, sendBuffer);
                }
            }
            catch (Exception ex)
            {
                BufferPool.Release(initialBuffer);

                Logger.Exception(nameof(RawTCPSender), nameof(BeginSend), ex, this._parent.Context);
            }
        }

        private void OnSendFinished(IAsyncResult ar)
        {
            try
            {
                int sent = this.Socket.EndSend(ar, out var errorCode);

                Logger.Information(nameof(RawTCPSender), $"{nameof(OnSendFinished)}: Sent {sent:N0} bytes!", this._parent.Context);

                var sentBuffer = ar.AsyncState as byte[];
                BufferPool.Release(sentBuffer);

                if (this._buffers.Count > 0)
                    BeginSend();
                else
                    Interlocked.Exchange(ref this._isSendInProgress, 0);
            }
            catch (Exception ex)
            {
                Logger.Exception(nameof(RawTCPSender), nameof(OnSendFinished), ex, this._parent.Context);
            }
        }
    }
}
