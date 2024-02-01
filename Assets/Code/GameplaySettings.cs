using System.Collections.Generic;
using UnityEngine;

namespace company.BettingOnColors
{
   [CreateAssetMenu(fileName = "GameplaySettings", menuName = "BettingOnColor/GameplaySettings", order = 1)]
    public class GameplaySettings : ScriptableObject
    {
        public int initialChipsPerStack = 10;
        public int numberOfStacks = 10;
        [Tooltip("Per bet")]
        public int minChipsSent = 1;
        [Tooltip("Per bet")]
        public int maxChipsSent = 10;
        [Tooltip("The number of chips a player needs to send to place a bet")]
        public int chipsRequiredToBet = 10;
        public List<Color> stacksColors = new List<Color> {
            Color.black,
            Color.blue,
            Color.cyan,
            Color.gray,
            Color.green,
            Color.magenta,
            Color.red,
            Color.white,
            Color.yellow,
            new Color(0.5f, 0.5f, 1f)            
         };
        public DisplayPlayerColorChoiceMode playerColorChoiceMode = DisplayPlayerColorChoiceMode.AfterAllPlayersPlacedBet;
        public float displayPickedColorPause = 1f;
        public float chipsFlySpeed = 30f;
    }
}
