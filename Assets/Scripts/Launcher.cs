using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class Launcher : MonoBehaviourPunCallbacks
{
    #region Private Fields

    /// <summary>
    /// This client's version number. Users are separated from each other by gameVersion (which allows you to make breaking changes).
    /// </summary>
    private string gameVersion = "1";
    private byte maxPlayersPerRoom = 5;
    private Dictionary<string, RoomInfo> allRooms;

    private List<string> allPlayerNamesInRoom;
    #endregion

    #region panel properties
    // loading panel
    public GameObject LoadingPanel;
    public TextMeshProUGUI LoadingTextLabel;
    // menu panel
    public GameObject MenuPanel;
    // creat room panel
    public GameObject CreatRoomPanel;
    public TMP_InputField roomNameInput;
    // Joined room panel
    public GameObject JoinedRoomPanel;
    public TextMeshProUGUI JoinedRoomHeader;
    public GameObject PlayerListScrollView;
    public GameObject PlayerNameTMPprefab;
    public Transform playerNamesContentTransform;
    public GameObject StartMatchButton;
    // find match panel
    public GameObject FindMatchPanel;
    public GameObject AvalilableRoomsScrollView;
    public Transform roomNameContentTransform;
    public GameObject joinMatchButtonPrefab;
    // Settings panel
    public GameObject SettingsPanel;
    public TMP_InputField PlayerNameInputField;
    // genericerror panel
    public GameObject genericErrorPanel;
    public TextMeshProUGUI errorText;

    #endregion



    void Awake()
    {
        // #Critical
        // this makes sure we can use PhotonNetwork.LoadLevel() on the master client and all clients in the same room sync their level automatically
        PhotonNetwork.AutomaticallySyncScene = true;
        if (allRooms != null) allRooms.Clear();
        if(allPlayerNamesInRoom != null) allPlayerNamesInRoom.Clear();
    }


    void Start()
    {
        CloseAllPanels();
        Connect();
        allRooms = new Dictionary<string, RoomInfo>();
        allPlayerNamesInRoom = new List<string>();
    }

    // Update is called once per frame
    void Update()
    {

    }
    #region network related
    private void Connect()
    {
        NavigateToLoadingPanel();
        LoadingTextLabel.text = "connecting to server ...";
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.AutomaticallySyncScene = true; // sync game scene with master client
            PhotonNetwork.GameVersion = gameVersion;
        }
    }

    public void CreateRoom()
    {
        string roomName = roomNameInput.text;
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = maxPlayersPerRoom;
        PhotonNetwork.CreateRoom(roomName, roomOptions);
        NavigateToLoadingPanel();
        LoadingTextLabel.text = "creating room ...";
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        NavigateMainMenuPanel();
    }
    
    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }
    public IEnumerable<string> FetchPlayerNamesInRoom()
    {
        Dictionary<int, Photon.Realtime.Player> pList = Photon.Pun.PhotonNetwork.CurrentRoom.Players;
        foreach (KeyValuePair<int, Photon.Realtime.Player> p in pList)
        {
            yield return p.Value.NickName;
        }
    }
    #endregion


    #region panelNavigaiton related
    private void CloseAllPanels()
    {
        LoadingPanel.SetActive(false);
        MenuPanel.SetActive(false);
        CreatRoomPanel.SetActive(false);
        JoinedRoomPanel.SetActive(false);
        FindMatchPanel.SetActive(false);
        SettingsPanel.SetActive(false);
        genericErrorPanel.SetActive(false);
    }
    public void QuitGame()
    {
        // save any game data here
#if UNITY_EDITOR
        // Application.Quit() does not work in the editor so
        // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void NavigateMainMenuPanel()
    {
        CloseAllPanels();
        MenuPanel.SetActive(true);
    }
    public void NavigateCreateRoomPanel()
    {
        CloseAllPanels();
        CreatRoomPanel.SetActive(true);
    }
    public void NavigateToLoadingPanel()
    {
        CloseAllPanels();
        LoadingPanel.SetActive(true);
    }
    public void NavigateJoinedRoomPanel()
    {
        CloseAllPanels();
        JoinedRoomPanel.SetActive(true);

        // fetch player names joined in same room
        this.allPlayerNamesInRoom = FetchPlayerNamesInRoom().ToList<string>();
        hydratePlayerNameScrollView();

        StartMatchButton.SetActive(false);
        if (PhotonNetwork.IsMasterClient) StartMatchButton.SetActive(true);
    }
    public void NavigateSettingsPanel()
    {
        CloseAllPanels();
        SettingsPanel.SetActive(true);
        PlayerNameInputField.text = PlayerPrefs.GetString(SD.playerNickName);
    }
    public void NavigateToFindMatchPanel()
    {
        CloseAllPanels();
        FindMatchPanel.SetActive(true);

        ScrollRect rect = AvalilableRoomsScrollView.GetComponent<ScrollRect>();
        foreach (Transform child in roomNameContentTransform)
        {
            Destroy(child.gameObject);
        }

        foreach (RoomInfo info in this.allRooms.Values)
        {
            if (info.IsVisible && info.IsOpen && !info.RemovedFromList)
            {
                GameObject matchButton = Instantiate(joinMatchButtonPrefab, roomNameContentTransform);
                matchButton.GetComponentInChildren<TMP_Text>().text = $"Room : {info.Name} players : {info.PlayerCount}/{info.MaxPlayers}";
                Button button = matchButton.GetComponent<Button>();
                button.onClick.AddListener(() => JoinRoom(info.Name));
            }
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(roomNameContentTransform.GetComponent<RectTransform>());

    }
    public void NavigateGenericErrorPanel()
    {
        CloseAllPanels();
        genericErrorPanel.SetActive(true);
    }
    #endregion

    #region MonoBehaviourPunCallbacks Callbacks

    public override void OnConnectedToMaster()
    {
        NavigateToLoadingPanel();
        LoadingTextLabel.text = "joining lobby ...";
        PhotonNetwork.JoinLobby();
        PhotonNetwork.NickName = PlayerPrefs.GetString(SD.playerNickName);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"Failed to connect to Photon server. Reason: {cause}");

        // Handle specific cases of disconnection
        switch (cause)
        {
            case DisconnectCause.Exception:
            case DisconnectCause.ExceptionOnConnect:
                NavigateGenericErrorPanel();
                errorText.text = "Exception during connection. Check your internet connection.";
                break;

            case DisconnectCause.ServerTimeout:
            case DisconnectCause.ClientTimeout:
                NavigateGenericErrorPanel();
                errorText.text = "Connection timed out. The server or client did not respond in time.";
                break;

            case DisconnectCause.InvalidRegion:
                NavigateGenericErrorPanel();
                errorText.text = "Invalid region specified. Ensure the region is correctly set.";
                break;

            case DisconnectCause.AuthenticationTicketExpired:
                NavigateGenericErrorPanel();
                errorText.text = "Authentication failed or the ticket expired. Please check your credentials.";
                break;

            case DisconnectCause.MaxCcuReached:
                NavigateGenericErrorPanel();
                errorText.text = "Maximum Concurrent Users (CCU) reached on the server. Try again later.";
                break;

            // Add additional cases as needed for specific DisconnectCause values

            default:
                NavigateGenericErrorPanel();
                errorText.text = "Disconnected due to an unknown cause.";
                break;
        }
    }
    public override void OnCreatedRoom()
    {
        Debug.Log("room created");
    }
    public override void OnJoinedRoom()
    {
        NavigateJoinedRoomPanel();
        JoinedRoomHeader.text = $"Joined room :  {PhotonNetwork.CurrentRoom.Name}";
    }
    public override void OnJoinedLobby()
    {
        NavigateMainMenuPanel();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo room in roomList)
        {
            if (allRooms.ContainsKey(room.Name))
            {
                allRooms[room.Name] = room;
            }
            else
            {
                allRooms.Add(room.Name, room);
            }
        }
    }
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        this.allPlayerNamesInRoom = FetchPlayerNamesInRoom().ToList<string>();
        // fetch player names joined in same room
        this.allPlayerNamesInRoom = FetchPlayerNamesInRoom().ToList<string>();
        hydratePlayerNameScrollView();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        StartMatchButton.SetActive(true);
    }

    // error callbacks
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        NavigateGenericErrorPanel();
        errorText.text = message;
    }
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        NavigateGenericErrorPanel();
        errorText.text = message;
    }
    #endregion


    // utility 
    public void hydratePlayerNameScrollView()
    {
        foreach (Transform child in playerNamesContentTransform)
        {
            Destroy(child.gameObject);
        }

        foreach (string playerNickname in this.allPlayerNamesInRoom)
        {
            GameObject textMeshPro = Instantiate(PlayerNameTMPprefab, playerNamesContentTransform);
            textMeshPro.GetComponentInChildren<TMP_Text>().text = $"Nick : {playerNickname}";
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(playerNamesContentTransform.GetComponent<RectTransform>());
    }

    public void SaveSettings()
    {
        string newPlayerNickname = PlayerNameInputField.text.Trim().Length == 0 ? "anonim" : PlayerNameInputField.text;
        PlayerPrefs.SetString(SD.playerNickName , newPlayerNickname);
        PhotonNetwork.NickName = newPlayerNickname;
        NavigateMainMenuPanel();
    }

    
    public void StartMatch()
    {
        // close room after match start
        PhotonNetwork.CurrentRoom.IsVisible = false;
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.LoadLevel(1); // loading game scene
    }
}
