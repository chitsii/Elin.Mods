namespace Elin_JustDoomIt
{
    public readonly struct DoomHudViewModel
    {
        public readonly string WagerText;
        public readonly string PerKillPayoutText;
        public readonly string PoolLabelText;
        public readonly string PoolValueText;
        public readonly int PoolValue;
        public readonly int RiskLoss;
        public readonly bool RoundActive;

        public DoomHudViewModel(
            string wagerText,
            string perKillPayoutText,
            string poolLabelText,
            string poolValueText,
            int poolValue,
            int riskLoss,
            bool roundActive)
        {
            WagerText = wagerText ?? string.Empty;
            PerKillPayoutText = perKillPayoutText ?? string.Empty;
            PoolLabelText = poolLabelText ?? string.Empty;
            PoolValueText = poolValueText ?? string.Empty;
            PoolValue = poolValue < 0 ? 0 : poolValue;
            RiskLoss = riskLoss < 0 ? 0 : riskLoss;
            RoundActive = roundActive;
        }
    }

    public readonly struct DoomRateSelectionViewModel
    {
        public readonly string Title;
        public readonly string HelperText;
        public readonly string[] Options;
        public readonly int SelectedIndex;

        public DoomRateSelectionViewModel(string title, string helperText, string[] options, int selectedIndex)
        {
            Title = title ?? string.Empty;
            HelperText = helperText ?? string.Empty;
            Options = options ?? System.Array.Empty<string>();
            SelectedIndex = selectedIndex;
        }
    }
}
