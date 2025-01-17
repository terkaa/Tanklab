using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using static BestHTTP.HTTPManager;

using BestMQTT.Databases;

namespace BestMQTT
{
    sealed class SessionDef
    {
        public string ClientId;

        /// <summary>
        /// Base64 encoded client id used for directory naming.
        /// </summary>
        public string Encoded;

        public override string ToString() => this.ClientId;
    }

    sealed class SessionStoreDef
    {
        public string Host;
        public string LastUsedSessionId;
        public List<SessionDef> Sessions;

        public SessionDef FindLast() => this.Sessions?.FirstOrDefault(s => s.ClientId == this.LastUsedSessionId);

        public override string ToString() => this.LastUsedSessionId;
    }

    /// <summary>
    /// Helper class to manage sessions.
    /// </summary>
    public static class SessionHelper
    {
        /// <summary>
        /// Returns with the root directory of the sessions
        /// </summary>
        internal static string GetRootSessionsDirectory(string host) => Path.Combine(new string[] { GetRootCacheFolder(), "MQTT", "Sessions", ToFileName(host) });
        private static string GetSessionStoreJSonPath(string host) => Path.Combine(SessionHelper.GetRootSessionsDirectory(host), "SessionStore.json");
        private static string ToFileName(string name) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(name)).Replace('/', '-');

        /// <summary>
        /// Creates and returns with a Null session.
        /// </summary>
        public static Session CreateNullSession(string host) => new Session(host, new SessionDef { ClientId = null, Encoded = null });

        /// <summary>
        /// Returns with all the current sessions.
        /// </summary>
        public static IEnumerable<Session> GetSessions(string host) => LoadStore(host).Sessions?.Select(def => new Session(host, def)).ToArray() ?? new Session[0];

        /// <summary>
        /// Returns true if there's at least one stored session for the given host.
        /// </summary>
        public static bool HasAny(string host) => LoadStore(host).Sessions?.Count > 0;

        /// <summary>
        /// Loads the session with the matching clientId, or creates a new one with this id.
        /// </summary>
        public static Session Get(string host, string clientId = null)
        {
            var sessionStoreDef = LoadStore(host);
            if (sessionStoreDef.Sessions == null)
                sessionStoreDef.Sessions = new List<SessionDef>();

            SessionDef sessionDef = null;

            if (!string.IsNullOrEmpty(clientId))
            {
                sessionDef = sessionStoreDef.Sessions?.FirstOrDefault(sd => sd.ClientId == clientId);
                if (sessionDef == null) {
                    sessionDef = Create(clientId);
                    sessionStoreDef.Sessions.Add(sessionDef);
                }

                if (sessionStoreDef.LastUsedSessionId != clientId)
                {
                    sessionStoreDef.LastUsedSessionId = clientId;
                    Save(host, sessionStoreDef);
                }

                return new Session(host, sessionDef);
            }

            if (!string.IsNullOrEmpty(sessionStoreDef.LastUsedSessionId))
            {
                sessionDef = sessionStoreDef.Sessions?.FirstOrDefault(sd => sd.ClientId == sessionStoreDef.LastUsedSessionId);
                if (sessionDef != null)
                    return new Session(host, sessionDef);
            }

            sessionDef = Create(null);

            sessionStoreDef.Sessions.Add(sessionDef);
            sessionStoreDef.LastUsedSessionId = sessionDef.ClientId;

            Save(host, sessionStoreDef);

            return new Session(host, sessionDef);
        }

        private static SessionDef Create(string clientId)
        {
            var sessionDef = new SessionDef();
            sessionDef.ClientId = string.IsNullOrEmpty(clientId) ? ("BestMQTT_" + Guid.NewGuid().ToString().Replace("-", string.Empty)) : clientId;
            sessionDef.Encoded = ToFileName(sessionDef.ClientId);

            return sessionDef;
        }

        /// <summary>
        /// Delete session from the store and all of its related files.
        /// </summary>
        public static void Delete(string host, Session session)
        {
            var sessionStoreDef = LoadStore(host);

            sessionStoreDef.Sessions?.RemoveAll(def => def.ClientId == session.ClientId);
            if (sessionStoreDef.LastUsedSessionId == session.ClientId)
                sessionStoreDef.LastUsedSessionId = null;

            session.Delete();

            Save(host, sessionStoreDef);
        }

        private static SessionStoreDef LoadStore(string host)
        {
            var sessionStorePath = GetSessionStoreJSonPath(host);

            if (IOService.FileExists(sessionStorePath))
            {
                using (var jsonFile = new StreamReader(IOService.CreateFileStream(sessionStorePath, BestHTTP.PlatformSupport.FileSystem.FileStreamModes.OpenRead)))
                    return BestHTTP.JSON.LitJson.JsonMapper.ToObject<SessionStoreDef>(jsonFile);
            }
            else
            {
                IOService.DirectoryCreate(Path.GetDirectoryName(sessionStorePath));
                return new SessionStoreDef() { Host = host };
            }
        }

        private static void Save(string host, SessionStoreDef sessionStoreDef)
        {
            using (var jsonFile = new StreamWriter(IOService.CreateFileStream(GetSessionStoreJSonPath(host), BestHTTP.PlatformSupport.FileSystem.FileStreamModes.Create)))
                BestHTTP.JSON.LitJson.JsonMapper.ToJson(sessionStoreDef, new BestHTTP.JSON.LitJson.JsonWriter(jsonFile));
        }
    }

    public sealed class Session
    {
        private SessionDef _sessionDef;

        public string Host { get { return this.__host; } }
        private string __host;

        public string ClientId { get { return this._sessionDef.ClientId; } }

        public bool IsNull { get { return this._sessionDef.ClientId == null; } }

        internal UInt16Database PublishReceivedPacketIDs { get { return this._publishReceivedPacketIDs ?? (this._publishReceivedPacketIDs = new UInt16Database(GetRootDir(), "PublishReceivedPacketIDs", new UInt16IndexingService())); } }
        private UInt16Database _publishReceivedPacketIDs;

        internal TopicAliasMappingDatabase ClientTopicAliasMapping { get { return this._clientTopicAliasMapping ?? (this._clientTopicAliasMapping = new TopicAliasMappingDatabase(GetQoSDir(), "ClientTopicAliasMapping", new TopicAliasMappingIndexingService())); } }
        private TopicAliasMappingDatabase _clientTopicAliasMapping;

        internal OutgoingPacketDatabase UnacknowledgedPackets { get { return this._unacknowledgedPackets ?? (this._unacknowledgedPackets = new OutgoingPacketDatabase(GetQoSDir(), "UnacknowledgedPackets", new OutgoingPacketIndexingService())); } }
        private OutgoingPacketDatabase _unacknowledgedPackets;

        internal UInt16Database PublishReleasedPacketIDs { get { return this._publishReleasedPacketIDs ?? (this._publishReleasedPacketIDs = new UInt16Database(GetQoSDir(), "PublishReleasedPacketIDs", new UInt16IndexingService())); } }
        private UInt16Database _publishReleasedPacketIDs;

        internal OutgoingPacketDatabase QueuedPackets { get { return this._queuedPackets ?? (this._queuedPackets = new OutgoingPacketDatabase(GetQoSDir(), "QueuedPackets", new OutgoingPacketIndexingService())); } }
        private OutgoingPacketDatabase _queuedPackets;

        internal Session(string host, SessionDef sessionDef)
        {
            this.__host = host;
            this._sessionDef = sessionDef;
        }

        private string GetRootDir() => Path.Combine(SessionHelper.GetRootSessionsDirectory(this.__host), this._sessionDef.Encoded);
        private string GetQoSDir() => Path.Combine(GetRootDir(), "QoS");
        
        internal void Delete()
        {
            var rootDir = GetRootDir();
            var qosDir = GetQoSDir();

            DeleteFiles(Path.Combine(rootDir, "PublishReceivedPacketIDs"));

            DeleteFiles(Path.Combine(qosDir, "ClientTopicAliasMapping"));
            DeleteFiles(Path.Combine(qosDir, "UnacknowledgedPackets"));
            DeleteFiles(Path.Combine(qosDir, "PublishReleasedPacketIDs"));
            DeleteFiles(Path.Combine(qosDir, "QueuedPackets"));
        }

        private void DeleteFiles(string path)
        {
            DeleteFile(path + ".db");
            DeleteFile(path + ".freelist");
            DeleteFile(path + ".metadata");
        }

        private void DeleteFile(string path)
        {
            try
            {
                if (IOService.FileExists(path))
                    IOService.FileDelete(path);
            }
            catch { }
        }
    }
}
