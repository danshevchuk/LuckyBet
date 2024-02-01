using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.SceneManagement;
using company.BettingOnColors.Misc;
using company.BettingOnColors.Utility;

namespace company.BettingOnColors.Managers
{
    public class GameManager : MonoBehaviourPunCallbacks
    {
        [Header("Gameplay Settings")]
        [SerializeField] private GameplaySettings _gameplaySettings;
        
        [Header("Players")]
        [SerializeField] private PlayerController _localPlayer;
        [SerializeField] private PlayerController _remotePlayer;

        [Header("Roulette Wheel")]
        [SerializeField] private Roulette roulette;

        [Header("UI")]
        [SerializeField] private Text sendChipsText;
        [SerializeField] private Text stateMessageText;
        [SerializeField] private GameObject leavingPanel;
        [SerializeField] private GameObject playerLeftPanel;

        private PhotonView _photonView;
        private BettingColor _pickedColor = BettingColor.None;
        private Dictionary<PlayerController, Bet> _betPlayerDict = new Dictionary<PlayerController, Bet>();

        // Start is called before the first frame update
        void Start()
        {
            _photonView = GetComponent<PhotonView>();

            if (!_photonView)
            {
                Debug.LogWarning("Please attach 'PhotonView' component to the GameManager game object", this);
                return;
            }

            if (!_gameplaySettings)
            {
                Debug.LogWarning("Please specify Gameplay Settings in the inspector", this);
                return;
            }

            InitializePlayerEvents(_localPlayer);
            _localPlayer.InitPlayer(_gameplaySettings, true);
            _remotePlayer.InitPlayer(_gameplaySettings, false);

            Message("Please place a bet", Color.black);
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                leavingPanel.SetActive(true);
                _photonView.RPC(nameof(OtherPlayerLeft), RpcTarget.OthersBuffered);
                Leave(0.5f);
            }
        }

        private void InitializePlayerEvents(PlayerController player)
        {
            player.onSelectChips += (int[] chips) =>
            {
                _photonView.RPC(nameof(OtherPlayerSelectChips), RpcTarget.OthersBuffered, chips);
            };
            
            player.onReturnChips += (int[] chips) =>
            {
                _photonView.RPC(nameof(OtherPlayerReturnChips), RpcTarget.OthersBuffered, chips);
            };

            if (_gameplaySettings.playerColorChoiceMode == DisplayPlayerColorChoiceMode.Always)
            {
                player.onPickedColor += (bettingColor) =>
                {
                    _photonView.RPC(nameof(OtherPlayerPickedColor), RpcTarget.OthersBuffered, bettingColor);
                };
            }
            
            player.onReady += (Bet bet) =>
            {
                AddPlayerBet(bet, player);
                _photonView.RPC(nameof(OtherPlayerPlacedBet), RpcTarget.OthersBuffered, JsonUtility.ToJson(bet));
            };
            
            player.onMessage += (string message) =>
            {
                Message(message, Color.black);
            };
            
            player.onRoundReset += () =>
            {
                _photonView.RPC(nameof(OtherPlayerRoundReset), RpcTarget.OthersBuffered);
            };
            
            if (sendChipsText)
            {
                player.onTotalSentChanged += (int totalSent, bool isReady) =>
                {
                    if (totalSent < _gameplaySettings.chipsRequiredToBet && !isReady)
                    {
                        sendChipsText.text = $"Please place {_gameplaySettings.chipsRequiredToBet - totalSent} more chips";
                    }
                    else
                    {
                        sendChipsText.text = "";
                    }
                };
            }
        }

        private void AddPlayerBet(Bet bet, PlayerController player, string waitingForPlayersMessage = "Waiting for other players to place a bet")
        {
            if (!_betPlayerDict.ContainsKey(player))
            {
                _betPlayerDict.Add(player, bet);
            }
            else
            {
                _betPlayerDict[player] = bet;
            }
            
            StartCoroutine(AddPlayerBetEnum(waitingForPlayersMessage));
        }

        private IEnumerator AddPlayerBetEnum(string waitingForPlayersMessage)
        {
            if (_betPlayerDict.Count == 2) // All bets placed
            {
                if (_pickedColor == BettingColor.None)
                {
                    PickRandomColor();
                }

                Message("Please place a bet", Color.black);

                yield return RoundResetEnum();

                // Restarting only if one of the players lost all chips
                yield return RestartGameEnum();

                ResetAll();
            }
            else
            {
                Message(waitingForPlayersMessage, Color.black);
            }
        }

        private IEnumerator RoundResetEnum()
        {
            var betLocal = _betPlayerDict[_localPlayer];
            var betRemote = _betPlayerDict[_remotePlayer];

            if (_gameplaySettings.playerColorChoiceMode == DisplayPlayerColorChoiceMode.AfterAllPlayersPlacedBet)
            {
                _remotePlayer.SetPickedColor(betRemote.color); // reveal what other player picked
            }
            yield return new WaitForSeconds(_gameplaySettings.displayPickedColorPause);

            if ((betLocal.color == _pickedColor && betRemote.color == _pickedColor)
                || (betLocal.color != _pickedColor && betRemote.color != _pickedColor))
            {
                yield return _localPlayer.GetBackPlacedChips();
                yield return _remotePlayer.GetBackPlacedChips();
            }
            else if (betLocal.color == _pickedColor && betRemote.color != _pickedColor)
            {
                yield return _localPlayer.GetBackPlacedChips();
                yield return _remotePlayer.SendBetChipsToPlayer(_localPlayer);
            }
            else if (betLocal.color != _pickedColor && betRemote.color == _pickedColor)
            {
                yield return _remotePlayer.GetBackPlacedChips();
                yield return _localPlayer.SendBetChipsToPlayer(_remotePlayer);
            }
        }

        private IEnumerator RestartGameEnum()
        {
            if (!_localPlayer.hasChips)
            {
                Message("You lost", Color.red);
                yield return new WaitForSeconds(3f);
                yield return _remotePlayer.SendChipsToPlayer(_localPlayer, IntArrayUtility.Init(_gameplaySettings.numberOfStacks, _gameplaySettings.initialChipsPerStack));
            }
            else if (!_remotePlayer.hasChips)
            {
                Message("Congratulations! You won!", Color.green);
                yield return new WaitForSeconds(3f);
                yield return _localPlayer.SendChipsToPlayer(_remotePlayer, IntArrayUtility.Init(_gameplaySettings.numberOfStacks, _gameplaySettings.initialChipsPerStack));
            }
        }

        private void ResetAll()
        {
            _betPlayerDict = new Dictionary<PlayerController, Bet>();
            _pickedColor = BettingColor.None;
            roulette.DisplayColor(BettingColor.None);
            _localPlayer.RoundReset();
            _remotePlayer.RoundReset();
            Message("Please place a bet", Color.black);
        }
        
        private void PickRandomColor()
        {
            _pickedColor = Random.Range(0f, 1f) < 0.5f ? BettingColor.Red : BettingColor.Green;
            roulette.DisplayColor(_pickedColor);

            _photonView.RPC(nameof(SetColorRPC), RpcTarget.OthersBuffered, _pickedColor);
        }

        private void Message(string message, Color color)
        {
            stateMessageText.text = message;
            stateMessageText.color = color;
        }

        private void Leave(float waitSeconds)
        {
            StartCoroutine(SwitchLevelEnum("Lobby", waitSeconds));
        }

        private IEnumerator SwitchLevelEnum(string level, float waitSeconds)
        {
            yield return new WaitForSeconds(waitSeconds);
            PhotonNetwork.Disconnect();
            while (PhotonNetwork.IsConnected)
            {
                yield return null;
            }
            SceneManager.LoadScene(level);
        }

        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            OtherPlayerLeft();
        }

        [PunRPC]
        private void OtherPlayerRoundReset()
        {
            _localPlayer.freezeControl = false;
        }

        [PunRPC]
        private void OtherPlayerSelectChips(int[] chips)
        {
            _remotePlayer.SelectChips(chips, _gameplaySettings.minChipsSent, _gameplaySettings.maxChipsSent);
        }

        [PunRPC]
        private void OtherPlayerReturnChips(int[] chips)
        {
            _remotePlayer.ReturnChips(chips);
        }

        [PunRPC]
        private void OtherPlayerPickedColor(BettingColor color)
        {
            _remotePlayer.SetPickedColor(color);
        }

        [PunRPC]
        private void OtherPlayerPlacedBet(string betJson)
        {
            var bet = JsonUtility.FromJson<Bet>(betJson);
            AddPlayerBet(bet, _remotePlayer, "The other player placed a bet. Please make your move");
        }

        [PunRPC]
        private void SetColorRPC(BettingColor color)
        {
            if (_pickedColor == BettingColor.None)
            {
                _pickedColor = color;
                roulette.DisplayColor(_pickedColor);
            }
        }

        [PunRPC]
        private void OtherPlayerLeft()
        {
            playerLeftPanel.SetActive(true);
            Leave(2.5f);
        }
    }
}