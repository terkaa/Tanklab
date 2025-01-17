using System.Collections;
using System.Collections.Generic;

using BestHTTP;

using BestMQTT;
using BestMQTT.Examples;
using BestMQTT.Examples.Helpers;

using UnityEditor;

using UnityEngine;
using UnityEngine.UI;

public partial class GenericClient : MonoBehaviour
{
#pragma warning disable 0649
    [Header("Connect")]
    [SerializeField]
    private Dropdown templatesDropdown;

    [SerializeField]
    private InputField hostInput;

    [SerializeField]
    private InputField portInput;

    [SerializeField]
    private Dropdown transportDropdown;

    [SerializeField]
    private InputField pathInput;

    [SerializeField]
    private Toggle isSecureToggle;

    [SerializeField]
    private InputField userNameInput;

    [SerializeField]
    private InputField passwordInput;

    [SerializeField]
    private InputField keepAliveInput;

    [SerializeField]
    private Dropdown protocolVersionDropdown;

    [SerializeField]
    private Button connectButton;

    [Header("Connect Last-Will")]

    [SerializeField]
    private InputField lastWill_TopicInput;

    [SerializeField]
    private Dropdown lastWill_QoSDropdown;

    [SerializeField]
    private Toggle lastWill_RetainToggle;

    [SerializeField]
    private InputField lastWill_MessageInput;


    [Header("Publish")]
    [SerializeField]
    private InputField publish_TopicInput;

    [SerializeField]
    private Dropdown publish_QoSDropdown;

    [SerializeField]
    private Toggle publish_RetainToggle;

    [SerializeField]
    private InputField publish_MessageInput;

    [Header("Subscribe")]
    [SerializeField]
    private InputField subscribe_ColorInput;

    [SerializeField]
    private Dropdown subscribe_QoSDropdown;

    [SerializeField]
    private InputField subscribe_TopicInput;

    [SerializeField]
    private Transform subscribe_ListItemRoot;

    [SerializeField]
    private SubscriptionListItem subscription_ListItem;

    [SerializeField]
    private Transform publishSubscribePanel;

    [Header("Logs")]
    [SerializeField]
    private InputField logs_MaxEntriesInput;

    [SerializeField]
    private Toggle logs_AutoScroll;

    [SerializeField]
    private TextListItem textListItem;

    [SerializeField]
    private ScrollRect log_view;

    [SerializeField]
    private Transform logRoot;

#pragma warning restore

    private void Awake()
    {
        InitUI();
        PopulateTemplates();
    }

    private void AddText(string text)
    {
        int maxEntries = this.logs_MaxEntriesInput.GetIntValue(100);

        if (this.logRoot.childCount >= maxEntries)
        {
            TrimLogEntries(maxEntries);

            var child = this.logRoot.GetChild(0);
            child.GetComponent<TextListItem>().SetText(text);
            child.SetAsLastSibling();
        }
        else
        {
            var item = Instantiate<TextListItem>(this.textListItem, this.logRoot);
            item.SetText(text);
        }

        bool autoScroll = this.logs_AutoScroll.GetBoolValue();
        if (autoScroll)
        {
            this.log_view.normalizedPosition = new Vector2(0, 0);
        }
    }

    private void TrimLogEntries(int maxEntries)
    {
        while (this.logRoot.childCount > maxEntries)
        {
            var child = this.logRoot.GetChild(0);
            child.transform.SetParent(this.transform);
            
            Destroy(child.gameObject);
        }
    }

    private void InitUI()
    {
        this.connectButton.GetComponentInChildren<Text>().text = "Connect";
        this.connectButton.interactable = true;
        this.connectButton.onClick.RemoveAllListeners();
        this.connectButton.onClick.AddListener(OnConnectButton);

        foreach (var button in this.publishSubscribePanel.GetComponentsInChildren<Button>())
            button.interactable = false;
    }

    class Template
    {
        public string name;

        public string host;
        public int port;
        public SupportedTransports transport;
        public string path = "/mqtt";
        public bool isSecure;

        public string username = string.Empty;
        public string password = string.Empty;
        public int keepAliveSeconds = 60;
        public SupportedProtocolVersions protocolVersion;

        public Template()
        {
            this.protocolVersion = SupportedProtocolVersions.MQTT_5_0;
        }
    }

    private Template[] templates = new Template[]
    {
        // broker.mqttdashboard.com
#if !UNITY_WEBGL || UNITY_EDITOR
        new Template { name = "HiveMQ - TCP - Unsecure - Unauthenticated", host = "broker.hivemq.com", port = 1883, transport = SupportedTransports.TCP, isSecure = false },
        new Template { name = "HiveMQ - WebSocket - Unauthenticated", host = "broker.hivemq.com", port = 8000, transport = SupportedTransports.WebSocket, isSecure = false },
#endif

        // broker.emqx.io
#if !UNITY_WEBGL || UNITY_EDITOR
        new Template { name = "emqx.com - TCP - Unsecure - Unauthenticated", host = "broker.emqx.io", port = 1883, transport = SupportedTransports.TCP, isSecure = false },
        new Template { name = "emqx.com - TCP - Secure - Unauthenticated", host = "broker.emqx.io", port = 8883, transport = SupportedTransports.TCP, isSecure = true },
        new Template { name = "emqx.com - WebSocket - Unsecure - Unauthenticated", host = "broker.emqx.io", port = 8083, transport = SupportedTransports.WebSocket, isSecure = false },
#endif
        new Template { name = "emqx.com - WebSocket - Secure - Unauthenticated", host = "broker.emqx.io", port = 8084, transport = SupportedTransports.WebSocket, isSecure = true },

        // test.mosquitto.org
#if !UNITY_WEBGL || UNITY_EDITOR
        new Template{ name ="test.mosquitto.org - TCP - Unsecure - Unauthenticated", host = "test.mosquitto.org", port = 1883, transport = SupportedTransports.TCP, isSecure = false },
        new Template{ name ="test.mosquitto.org - TCP - Unsecure - Authenticated (Read/Write)", host = "test.mosquitto.org", port = 1884, transport = SupportedTransports.TCP, isSecure = false, username = "rw", password = "readwrite" },
        new Template{ name ="test.mosquitto.org - TCP - Unsecure - Authenticated (Read only)", host = "test.mosquitto.org", port = 1884, transport = SupportedTransports.TCP, isSecure = false, username = "ro", password = "readonly" },
        new Template{ name ="test.mosquitto.org - TCP - Unsecure - Authenticated (Write only)", host = "test.mosquitto.org", port = 1884, transport = SupportedTransports.TCP, isSecure = false, username = "wo", password = "writeonly" },
        new Template{ name ="test.mosquitto.org - TCP - Secure (Self signed cert) - Unauthenticated", host = "test.mosquitto.org", port = 8883, transport = SupportedTransports.TCP, isSecure = true },
        new Template{ name ="test.mosquitto.org - TCP - Secure (Self signed cert) - Client Certificate Required (see docs)", host = "test.mosquitto.org", port = 8884, transport = SupportedTransports.TCP, isSecure = true },
        new Template{ name ="test.mosquitto.org - TCP - Secure - Authenticated (Read/Write)", host = "test.mosquitto.org", port = 8885, transport = SupportedTransports.TCP, isSecure = true, username = "rw", password = "readwrite" },
        new Template{ name ="test.mosquitto.org - TCP - Secure - Authenticated (Read only)", host = "test.mosquitto.org", port = 8885, transport = SupportedTransports.TCP, isSecure = true, username = "ro", password = "readonly" },
        new Template{ name ="test.mosquitto.org - TCP - Secure - Authenticated (Write only)", host = "test.mosquitto.org", port = 8885, transport = SupportedTransports.TCP, isSecure = true, username = "wo", password = "writeonly" },
        new Template{ name ="test.mosquitto.org - TCP - Secure (Lets Encrypt cert) - Unauthenticated", host = "test.mosquitto.org", port = 8886, transport = SupportedTransports.TCP, isSecure = true },
        new Template{ name ="test.mosquitto.org - TCP - Secure (server certificate deliberately expired) - Unauthenticated", host = "test.mosquitto.org", port = 8887, transport = SupportedTransports.TCP, isSecure = true },
        new Template{ name ="test.mosquitto.org - WebSocket - Unsecure - Unauthenticated", host = "test.mosquitto.org", port = 8080, transport = SupportedTransports.WebSocket, isSecure = false },
        new Template{ name ="test.mosquitto.org - WebSocket - Unsecure - Authenticated (Read/Write)", host = "test.mosquitto.org", port = 8090, transport = SupportedTransports.WebSocket, isSecure = false, username = "rw", password = "readwrite" },
        new Template{ name ="test.mosquitto.org - WebSocket - Unsecure - Authenticated (Read only)", host = "test.mosquitto.org", port = 8090, transport = SupportedTransports.WebSocket, isSecure = false, username = "ro", password = "readonly" },
        new Template{ name ="test.mosquitto.org - WebSocket - Unsecure - Authenticated (Write only)", host = "test.mosquitto.org", port = 8090, transport = SupportedTransports.WebSocket, isSecure = false, username = "wo", password = "writeonly" },
#endif
        new Template{ name ="test.mosquitto.org - WebSocket - Secure - Unauthenticated", host = "test.mosquitto.org", port = 8081, transport = SupportedTransports.WebSocket, isSecure = true },
        new Template{ name ="test.mosquitto.org - WebSocket - Secure - Authenticated (Read/Write)", host = "test.mosquitto.org", port = 8091, transport = SupportedTransports.WebSocket, isSecure = true, username = "rw", password = "readwrite" },
        new Template{ name ="test.mosquitto.org - WebSocket - Secure - Authenticated (Read only)", host = "test.mosquitto.org", port = 8091, transport = SupportedTransports.WebSocket, isSecure = true, username = "ro", password = "readonly" },
        new Template{ name ="test.mosquitto.org - WebSocket - Secure - Authenticated (Write only)", host = "test.mosquitto.org", port = 8091, transport = SupportedTransports.WebSocket, isSecure = true, username = "wo", password = "writeonly" },
    };

    private void PopulateTemplates()
    {
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>(templates.Length);
        for(int i = 0; i < templates.Length; i++)
        {
            var template = templates[i];

            options.Add(new Dropdown.OptionData(template.name));
        }

        this.templatesDropdown.AddOptions(options);
        this.templatesDropdown.onValueChanged.AddListener(OnTemplateSelected);
        OnTemplateSelected(0);
    }

    private void OnTemplateSelected(int idx)
    {
        var template = this.templates[idx];

        this.hostInput.text = template.host;
        this.portInput.text = template.port.ToString();
        this.transportDropdown.value = (int)template.transport;
        this.pathInput.text = template.path;
        this.isSecureToggle.isOn = template.isSecure;

        this.userNameInput.text = template.username;
        this.passwordInput.text = template.password;
        this.keepAliveInput.text = template.keepAliveSeconds.ToString();
    }

    private void SetConnectingUI()
    {
        this.connectButton.interactable = false;

        foreach (var button in this.publishSubscribePanel.GetComponentsInChildren<Button>())
            button.interactable = false;
    }

    private void SetDisconnectedUI()
    {
        InitUI();
        for (int i = 0; i < this.subscriptionListItems.Count; ++i)
            Destroy(this.subscriptionListItems[i].gameObject);
        this.subscriptionListItems.Clear();
    }

    private void SetConnectedUI()
    {
        this.connectButton.GetComponentInChildren<Text>().text = "Disconnect";
        this.connectButton.interactable = true;
        this.connectButton.onClick.RemoveAllListeners();
        this.connectButton.onClick.AddListener(OnDisconnectButton);
        
        foreach (var button in this.publishSubscribePanel.GetComponentsInChildren<Button>())
            button.interactable = true;
    }

    public void ClearLogEntries()
    {
        TrimLogEntries(0);
    }

    public void OnLogLevelChanged(int idx)
    {
        switch(idx)
        {
            case 0: HTTPManager.Logger.Level = BestHTTP.Logger.Loglevels.All; break;
            case 1: HTTPManager.Logger.Level = BestHTTP.Logger.Loglevels.Warning; break;
            case 2: HTTPManager.Logger.Level = BestHTTP.Logger.Loglevels.None; break;
        }
    }
}
