using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;

namespace company.BettingOnColors.Networking
{
    public class Lobby : MonoBehaviourPunCallbacks
    {
        [SerializeField] private GameObject _buttonPlay;
        [SerializeField] private GameObject _progressLabel;
        [SerializeField] private string roomName = "room1";

        private Text _progressLabelText;
        private bool _isConnecting;
        private string _gameVersion = "1";

        private const int maxPlayersPerRoom = 2;

        public void Connect()
        {
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.JoinOrCreateRoom(roomName, new RoomOptions { MaxPlayers = maxPlayersPerRoom }, TypedLobby.Default);
                _isConnecting = false;
            }
            else
            {
                _isConnecting = PhotonNetwork.ConnectUsingSettings();
                PhotonNetwork.GameVersion = _gameVersion;
            }
            _progressLabel.SetActive(true);
            _buttonPlay.SetActive(false);
        }

        private void LoadGame()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogError("PhotonNetwork : Trying to Load a level but we are not the master Client");
            }
            if (PhotonNetwork.CurrentRoom.PlayerCount > 1)
            {
                PhotonNetwork.LoadLevel("Game");
            }
        }


        public override void OnConnectedToMaster()
        {
            if (_isConnecting)
            {
                PhotonNetwork.JoinOrCreateRoom(roomName, new RoomOptions { MaxPlayers = maxPlayersPerRoom }, TypedLobby.Default);
                _isConnecting = false;
            }
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            _progressLabel.SetActive(false);
            _buttonPlay.SetActive(true);
            Debug.LogWarningFormat("PUN Basics Tutorial/Launcher: OnDisconnected() was called by PUN with reason {0}", cause);
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            PhotonNetwork.JoinOrCreateRoom(roomName, new RoomOptions { MaxPlayers = maxPlayersPerRoom }, TypedLobby.Default);
        }

        public override void OnJoinedRoom()
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
            {
                _progressLabelText.text = "Entering the game...";
                PhotonNetwork.LoadLevel("Game");
            }
            else
            {
                _progressLabelText.text = "Waiting for the other player to join...";
            }
        }

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player other)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                LoadGame();
            }
        }

        void Awake()
        {
            PhotonNetwork.AutomaticallySyncScene = true;
        }

        void Start()
        {
            _progressLabel.SetActive(false);
            _buttonPlay.SetActive(true);

            _progressLabelText = _progressLabel.GetComponent<Text>();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
        }
    }
}