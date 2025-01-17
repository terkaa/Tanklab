using System;

namespace BestMQTT
{
    /// <summary>
    /// Supported transports that can be used to connect with.
    /// </summary>
    public enum SupportedTransports
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        TCP = 0,
#endif
        WebSocket = 1
    }

    public enum SupportedProtocolVersions
    {
        MQTT_3_1_1,
        MQTT_5_0
    }

    /// <summary>
    /// Connection related options to pass to the MQTTClient.
    /// </summary>
    public sealed class ConnectionOptions
    {
        /// <summary>
        /// Host of the target server.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Port of the service listening on.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Whether to use a secure protocol (TLS over TCP or wss://).
        /// </summary>
        public bool UseTLS { get; set; }

        /// <summary>
        /// Selected transport to connect with.
        /// </summary>
        public SupportedTransports Transport { get; set; }

        /// <summary>
        /// Optional path for websocket, its default is "/mqtt".
        /// </summary>
        public string Path { get; set; } = "/mqtt";

        /// <summary>
        /// The protocol version that the plugin has to use to connect with to the server.
        /// </summary>
        public SupportedProtocolVersions ProtocolVersion { get; set; } = SupportedProtocolVersions.MQTT_5_0;
    }

    public sealed class ConnectionOptionsBuilder
    {
        private ConnectionOptions Options { get; set; } = new ConnectionOptions();

#if !UNITY_WEBGL || UNITY_EDITOR
        /// <summary>
        /// Add options for a TCP connection.
        /// </summary>
        public ConnectionOptionsBuilder WithTCP(string host, int port)
        {
            this.Options.Host = host;
            this.Options.Port = port;
            this.Options.Transport = SupportedTransports.TCP;

            return this;
        }
#endif

        /// <summary>
        /// Add options for a WebSocket connection.
        /// </summary>
        public ConnectionOptionsBuilder WithWebSocket(string host, int port)
        {
            this.Options.Host = host;
            this.Options.Port = port;
            this.Options.Transport = SupportedTransports.WebSocket;

            return this;
        }

        /// <summary>
        /// When used MQTTClient going to use TLS to secure the communication.
        /// </summary>
        public ConnectionOptionsBuilder WithTLS()
        {
            this.Options.UseTLS = true;

            return this;
        }

        /// <summary>
        /// Used by the WebSocket transport to connect to the given path.
        /// </summary>
        public ConnectionOptionsBuilder WithPath(string path)
        {
            this.Options.Path = path;
            return this;
        }

        /// <summary>
        /// The protocol version that the plugin has to use to connect with to the server.
        /// </summary>
        public ConnectionOptionsBuilder WithProtocolVersion(SupportedProtocolVersions version)
        {
            this.Options.ProtocolVersion = version;
            return this;
        }

        /// <summary>
        /// Creates an MQTTClient object with the already set options.
        /// </summary>
        public MQTTClient CreateClient() => new MQTTClient(this.Build());

        /// <summary>
        /// Creates the final ConnectionOptions instance.
        /// </summary>
        public ConnectionOptions Build() => this.Options;
    }
}
