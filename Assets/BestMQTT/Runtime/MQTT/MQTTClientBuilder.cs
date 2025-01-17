using System;

namespace BestMQTT
{
    /// <summary>
    /// Builder class to make MQTTClient creation easier.
    /// </summary>
    public sealed class MQTTClientBuilder
    {
        private ConnectionOptions _options = new ConnectionOptions();
        private OnConnectedDelegate _onConnected;
        private OnServerConnectAckMessageDelegate _onServerConnectAckMessage;
        private OnApplicationMessageDelegate _onApplicationMessage;
        private OnAuthenticationMessageDelegate _onAuthenticationMessage;
        private OnErrorDelegate _onError;
        private OnDisconnectDelegate _onDisconnect;
        private OnStateChangedDelegate _onStateChanged;

        public MQTTClientBuilder WithOptions(ConnectionOptions options)
        {
            this._options = options;
            return this;
        }

        public MQTTClientBuilder WithOptions(ConnectionOptionsBuilder builder)
        {
            return WithOptions(builder.Build());
        }

        public MQTTClientBuilder WithEventHandler(OnConnectedDelegate onConnected)
        {
            this._onConnected = onConnected;
            return this;
        }

        public MQTTClientBuilder WithEventHandler(OnServerConnectAckMessageDelegate onServerConnectAckMessage)
        {
            this._onServerConnectAckMessage = onServerConnectAckMessage;
            return this;
        }

        public MQTTClientBuilder WithEventHandler(OnApplicationMessageDelegate onApplicationMessage)
        {
            this._onApplicationMessage = onApplicationMessage;
            return this;
        }

        public MQTTClientBuilder WithEventHandler(OnAuthenticationMessageDelegate onAuthenticationMessage)
        {
            this._onAuthenticationMessage = onAuthenticationMessage;
            return this;
        }

        public MQTTClientBuilder WithEventHandler(OnErrorDelegate onError)
        {
            this._onError = onError;
            return this;
        }

        public MQTTClientBuilder WithEventHandler(OnDisconnectDelegate onDisconnect)
        {
            this._onDisconnect = onDisconnect;
            return this;
        }

        public MQTTClientBuilder WithEventHandler(OnStateChangedDelegate onStateChanged)
        {
            this._onStateChanged = onStateChanged;
            return this;
        }

        /// <summary>
        /// Creates an MQTTClient instance.
        /// </summary>
        public MQTTClient CreateClient()
        {
            var client = new MQTTClient(this._options);

            if (this._onConnected != null)
                client.OnConnected += this._onConnected;

            if (this._onServerConnectAckMessage != null)
                client.OnServerConnectAckMessage += this._onServerConnectAckMessage;

            if (this._onApplicationMessage != null)
                client.OnApplicationMessage += this._onApplicationMessage;

            if (this._onAuthenticationMessage != null)
                client.OnAuthenticationMessage += this._onAuthenticationMessage;

            if (this._onError != null)
                client.OnError += this._onError;

            if (this._onDisconnect != null)
                client.OnDisconnect += this._onDisconnect;

            if (this._onStateChanged != null)
                client.OnStateChanged += this._onStateChanged;

            return client;
        }
    }
}
