namespace company.BettingOnColors
{
    [System.Serializable]
    public class Bet
    {
        public int[] chips { get; }
        public BettingColor color { get; }

        public Bet(BettingColor color, int[] chips)
        {
            this.color = color;
            this.chips = chips;
        }
    }
}