namespace IOTLinkAPI.Configs
{
#pragma warning disable 1591
    public class MqttConfig
    {
        public CredentialConfig Credentials { get; set; }

        public TCPConfig TCP { get; set; }

        public WebSocketConfig WebSocket { get; set; }

        public MsgConfig Messages { get; set; }

        public LWTConfig LWT { get; set; }

        public DiscoveryConfig Discovery { get; set; }

        public string ClientId { get; set; }

        public string Prefix { get; set; }

        public string GlobalPrefix { get; set; }

        public bool CleanSession { get; set; }

        public int KeepAlivePeriod { get; set; }

        public int AutoReconnectDelay { get; set; }

        public int MaxPendingMessages { get; set; }

        public class CredentialConfig
        {
            public string Username { get; set; }

            public string Password { get; set; }

            public static CredentialConfig FromConfiguration(Configuration config)
            {
                if (config == null)
                    return new CredentialConfig();

                CredentialConfig cfg = new CredentialConfig
                {
                    Username = config.GetValue("username", null),
                    Password = config.GetValue("password", null)
                };

                return cfg;
            }
        }

        public class TCPConfig
        {
            public string Hostname { get; set; }

            public int Port { get; set; }

            public bool Secure { get; set; }

            public bool Enabled { get; set; }

            public TlsConfig TlsConfig { get; set; }

            public static TCPConfig FromConfiguration(Configuration config)
            {
                if (config == null)
                    return new TCPConfig();

                TCPConfig cfg = new TCPConfig
                {
                    Hostname = config.GetValue("hostname", "localhost"),
                    Port = config.GetValue("port", 1883),
                    Secure = config.GetValue("secure", false),
                    Enabled = config.GetValue("enabled", true),
                    TlsConfig = TlsConfig.FromConfiguration(config.GetValue("tlsConfig"))
                };

                return cfg;
            }
        }

        public class WebSocketConfig
        {
            public string URI { get; set; }

            public bool Secure { get; set; }

            public bool Enabled { get; set; }

            public TlsConfig TlsConfig { get; set; }

            public static WebSocketConfig FromConfiguration(Configuration config)
            {
                if (config == null)
                    return new WebSocketConfig();

                WebSocketConfig cfg = new WebSocketConfig
                {
                    URI = config.GetValue("uri", null),
                    Secure = config.GetValue("secure", false),
                    Enabled = config.GetValue("enabled", false),
                    TlsConfig = TlsConfig.FromConfiguration(config.GetValue("tlsConfig"))
                };

                return cfg;
            }
        }

        public class TlsConfig
        {
            public bool AllowUntrustedCertificates { get; set; }

            public bool IgnoreCertificateChainErrors { get; set; }

            public bool IgnoreCertificateRevocationErrors { get; set; }

            public string CACertificate { get; set; }

            public string ClientCertificate { get; set; }

            public string ClientCertificatePassword { get; set; }

            public static TlsConfig FromConfiguration(Configuration config)
            {
                if (config == null)
                    return new TlsConfig();

                TlsConfig cfg = new TlsConfig
                {
                    AllowUntrustedCertificates = config.GetValue("allowUntrustedCertificates", false),
                    IgnoreCertificateChainErrors = config.GetValue("ignoreCertificateChainErrors", false),
                    IgnoreCertificateRevocationErrors = config.GetValue("ignoreCertificateRevocationErrors", false),
                    CACertificate = config.GetValue("caCertificate", null),
                    ClientCertificate = config.GetValue("clientCertificate", null),
                    ClientCertificatePassword = config.GetValue("clientCertificatePassword", null),
                };

                return cfg;
            }
        }

        public class LWTConfig : MsgConfig
        {
            public string ConnectMessage { get; set; }

            public string DisconnectMessage { get; set; }

            public bool Enabled { get; set; }

            public static new LWTConfig FromConfiguration(Configuration config)
            {
                if (config == null)
                    return new LWTConfig();

                LWTConfig cfg = new LWTConfig
                {
                    ConnectMessage = config.GetValue("connectMsg", "ON"),
                    DisconnectMessage = config.GetValue("disconnectMsg", "OFF"),
                    QoS = config.GetValue("qos", 1),
                    Retain = config.GetValue("retain", false),
                    Enabled = config.GetValue("enabled", true)
                };

                return cfg;
            }
        }

        public class MsgConfig
        {
            public int QoS { get; set; }

            public bool Retain { get; set; }

            public static MsgConfig FromConfiguration(Configuration config)
            {
                if (config == null)
                    return new MsgConfig();

                MsgConfig cfg = new MsgConfig
                {
                    QoS = config.GetValue("qos", 1),
                    Retain = config.GetValue("retain", false)
                };

                return cfg;
            }
        }

        public class DiscoveryConfig
        {
            public string TopicPrefix { get; set; }

            public bool Enabled { get; set; }

            public bool DomainPrefix { get; set; }

            public static DiscoveryConfig FromConfiguration(Configuration config)
            {
                if (config == null)
                    return new DiscoveryConfig();

                DiscoveryConfig cfg = new DiscoveryConfig
                {
                    TopicPrefix = config.GetValue("topicPrefix", "homeassistant"),
                    DomainPrefix = config.GetValue("domainPrefix", false),
                    Enabled = config.GetValue("enabled", true)
                };

                return cfg;
            }
        }

        public static MqttConfig FromConfiguration(Configuration config)
        {
            if (config == null)
                return new MqttConfig();

            MqttConfig mqtt = new MqttConfig
            {
                Credentials = CredentialConfig.FromConfiguration(config.GetValue("credentials")),
                TCP = TCPConfig.FromConfiguration(config.GetValue("tcp")),
                WebSocket = WebSocketConfig.FromConfiguration(config.GetValue("websocket")),
                Messages = MsgConfig.FromConfiguration(config.GetValue("messages")),
                LWT = LWTConfig.FromConfiguration(config.GetValue("lwt")),
                Discovery = DiscoveryConfig.FromConfiguration(config.GetValue("discovery")),

                ClientId = config.GetValue("clientId", null),
                CleanSession = config.GetValue("cleanSession", true),
                Prefix = config.GetValue("prefix", "IOTLink"),
                GlobalPrefix = config.GetValue("globalPrefix", "IOTLink/all"),
                AutoReconnectDelay = config.GetValue("autoReconnectDelay", 10),
                KeepAlivePeriod = config.GetValue("keepAlivePeriod", 60),
                MaxPendingMessages = config.GetValue("maxPendingMessages", 10),
            };

            return mqtt;
        }
    }
}
