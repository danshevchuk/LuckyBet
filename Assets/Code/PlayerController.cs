using UnityEngine;
using company.BettingOnColors.Chips;
using company.BettingOnColors.Utility;

namespace company.BettingOnColors
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private ChipsStacksController stacksController;
        [SerializeField] private ChipsStacksController bettingStacksController;
        [SerializeField] private Renderer chipsTableRenderer;

        private bool _ready = false;
        private int _chipsRequiredToBet;
        private BettingColor _pickedColor = BettingColor.None;
        private int _totalSentChips;
        private int _numberOfStacks;

        public System.Action onRoundReset;
        public System.Action<Bet> onReady;
        public System.Action<string> onMessage;
        public System.Action<int[]> onSelectChips;
        public System.Action<int[]> onReturnChips;
        public System.Action<int, bool> onTotalSentChanged;
        public System.Action<BettingColor> onPickedColor;

        public int totalSentChips
        {
            get => _totalSentChips;
            set
            {
                _totalSentChips = value;
                onTotalSentChanged?.Invoke(_totalSentChips, _ready);
            }
        }

        public bool freezeControl { get; set; } = false;

        public bool hasChips => stacksController.chips.Sum() > 0;

        private int[] _totalSentChipsArray { get; set; }

        public void RoundReset()
        {
            _ready = false;
            totalSentChips = 0;
            _totalSentChipsArray = IntArrayUtility.Zeros(_numberOfStacks);
            SetPickedColor(BettingColor.None);

            stacksController.allowInteraction = true;
            stacksController.RoundReset();

            bettingStacksController.RoundReset();
            bettingStacksController.SetSelectionMask(bettingStacksController.chips);
            bettingStacksController.allowInteraction = true;

            onRoundReset?.Invoke();
        }

        public void InitPlayer(GameplaySettings settings, bool isLocal)
        {
            totalSentChips = 0;
            
            _numberOfStacks = settings.numberOfStacks;
            _chipsRequiredToBet = settings.chipsRequiredToBet;
            _totalSentChipsArray = IntArrayUtility.Zeros(_numberOfStacks);

            stacksController.InitStacks(settings, useEvents: isLocal);
            stacksController.SetSelectionMask(IntArrayUtility.Zeros(_numberOfStacks));
            stacksController.onSelectChips += (int[] chips) =>
            {
                if (freezeControl)
                {
                    return;
                }
                if (SelectChips(chips, settings.minChipsSent, settings.maxChipsSent))
                {
                    onSelectChips?.Invoke(chips);
                }
            };

            bettingStacksController.InitStacks(settings, useEvents: isLocal, initialNumChips: 0);
            bettingStacksController.onSelectChips += (int[] chips) =>
            {
                if (freezeControl)
                {
                    return;
                }
                ReturnChips(chips);
                onReturnChips?.Invoke(chips);
            };
        }

        private int[] ProcessSelectedChips(int[] chips, int min, int max)
        {
            int[] result = new int[chips.Length];
            for (int i = 0; i < chips.Length; i++)
            {
                if (chips[i] < min)
                {
                    result[i] = 0;
                }
                else if (chips[i] > max)
                {
                    result[i] = max;
                }
                else
                {
                    result[i] = chips[i];
                }

                if (result[i] + totalSentChips > max)
                {
                    result[i] = max - totalSentChips;
                }
            }
            return result;
        }

        public bool SelectChips(int[] chips, int min, int max)
        {
            if (totalSentChips >= max)
            {
                return false;
            }
            var processedChips = ProcessSelectedChips(chips, min, max);
            var sentChipsSum = processedChips.Sum();
            totalSentChips += sentChipsSum;
            stacksController.SendChips(bettingStacksController, processedChips, (int[] originState, int[] targetState) =>
            {
                _totalSentChipsArray = targetState;
            });
            return sentChipsSum > 0;
        }

        public void ReturnChips(int[] chips)
        {
            if (totalSentChips <= 0)
            {
                return;
            }
            totalSentChips = Mathf.Max(totalSentChips - chips.Sum(), 0);
            bettingStacksController.SendChips(stacksController, chips, (int[] originState, int[] targetState) =>
            {
                _totalSentChipsArray = originState;
            });
        }

        public void PickColorRed()
        {
            SetPickedColor(BettingColor.Red);
            onPickedColor?.Invoke(_pickedColor);
        }

        public void PickColorGreen()
        {
            SetPickedColor(BettingColor.Green);
            onPickedColor?.Invoke(_pickedColor);
        }

        public void SetPickedColor(BettingColor color)
        {
            if (_ready)
            {
                return;
            }
            _pickedColor = color;
            UpdatePickedColorDisplay();
        }

        private void UpdatePickedColorDisplay()
        {
            switch (_pickedColor)
            {
                case BettingColor.Green:
                    {
                        chipsTableRenderer.material.color = Color.green;
                        break;
                    }
                case BettingColor.Red:
                    {
                        chipsTableRenderer.material.color = Color.red;
                        break;
                    }
                default:
                    {
                        chipsTableRenderer.material.color = Color.gray;
                        break;
                    }
            }
        }

        public void TrySetReady()
        {
            /* 
                1. sent chips must be a certain number defined in the settings
                2. the player must bet on one of the colors
             */
            if (_ready)
            {
                return;
            }

            bool allChipsSent = totalSentChips >= _chipsRequiredToBet;
            if (!allChipsSent)
            {
                onMessage?.Invoke($"Please place all { _chipsRequiredToBet } chips");
                return;
            }

            bool colorPicked = _pickedColor != BettingColor.None;
            if (!colorPicked)
            {
                onMessage?.Invoke("Please pick a color");
                return;
            }

            if (allChipsSent && colorPicked)
            {
                _ready = true;
                stacksController.allowInteraction = false;
                bettingStacksController.allowInteraction = false;
                var bet = new Bet(_pickedColor, _totalSentChipsArray);
                onReady?.Invoke(bet);
                freezeControl = true;
            }
        }

        public Coroutine GetBackPlacedChips()
        {
            return StartCoroutine(bettingStacksController.SendChipsEnum(bettingStacksController.chips, stacksController));
        }

        public Coroutine SendBetChipsToPlayer(PlayerController receiver)
        {
            return receiver.ReceiveChipsFromStackController(bettingStacksController, bettingStacksController.chips);
        }

        public Coroutine SendChipsToPlayer(PlayerController receiver, int[] chips)
        {
            return receiver.ReceiveChipsFromStackController(stacksController, chips);
        }

        private Coroutine ReceiveChipsFromStackController(ChipsStacksController otherStacksController, int[] chips)
        {
            return StartCoroutine(otherStacksController.SendChipsEnum(chips, stacksController));
        }
    }
}