using System;

namespace Elin_JustDoomIt
{
    public readonly struct DoomRewardRoundState
    {
        public readonly int Skill;
        public readonly DoomRewardRate CurrentRate;
        public readonly int CurrentPool;
        public readonly int MultiplierStage;
        public readonly bool RoundActive;
        public readonly bool RateSelectionOpen;
        public readonly string MapCode;
        public readonly string MapTitle;

        public DoomRewardRoundState(
            int skill,
            DoomRewardRate currentRate,
            int currentPool,
            int multiplierStage,
            bool roundActive,
            bool rateSelectionOpen,
            string mapCode,
            string mapTitle)
        {
            Skill = Math.Max(1, Math.Min(5, skill));
            CurrentRate = currentRate;
            CurrentPool = Math.Max(0, currentPool);
            MultiplierStage = Math.Max(0, multiplierStage);
            RoundActive = roundActive;
            RateSelectionOpen = rateSelectionOpen;
            MapCode = mapCode ?? string.Empty;
            MapTitle = mapTitle ?? string.Empty;
        }
    }

    public readonly struct DoomRewardRoundResult
    {
        public readonly int EntryCost;
        public readonly int PoolDelta;
        public readonly int CashOutAmount;
        public readonly int BonusAmount;
        public readonly int LostPoolAmount;
        public readonly int UsedMultiplierStage;
        public readonly bool RoundStarted;
        public readonly bool RoundEnded;
        public readonly bool BonusReset;
        public readonly bool OpenedRateSelection;
        public readonly bool ClosedRateSelection;
        public readonly bool InsufficientFunds;

        public DoomRewardRoundResult(
            int entryCost = 0,
            int poolDelta = 0,
            int cashOutAmount = 0,
            int bonusAmount = 0,
            int lostPoolAmount = 0,
            int usedMultiplierStage = 0,
            bool roundStarted = false,
            bool roundEnded = false,
            bool bonusReset = false,
            bool openedRateSelection = false,
            bool closedRateSelection = false,
            bool insufficientFunds = false)
        {
            EntryCost = Math.Max(0, entryCost);
            PoolDelta = poolDelta;
            CashOutAmount = Math.Max(0, cashOutAmount);
            BonusAmount = Math.Max(0, bonusAmount);
            LostPoolAmount = Math.Max(0, lostPoolAmount);
            UsedMultiplierStage = Math.Max(0, usedMultiplierStage);
            RoundStarted = roundStarted;
            RoundEnded = roundEnded;
            BonusReset = bonusReset;
            OpenedRateSelection = openedRateSelection;
            ClosedRateSelection = closedRateSelection;
            InsufficientFunds = insufficientFunds;
        }
    }

    public sealed class DoomRewardRoundController
    {
        private DoomRewardRoundState _state = new DoomRewardRoundState(
            3,
            DoomRewardRate.None,
            0,
            0,
            roundActive: false,
            rateSelectionOpen: false,
            mapCode: string.Empty,
            mapTitle: string.Empty);

        public DoomRewardRoundState State => _state;

        public void ResetSession()
        {
            _state = new DoomRewardRoundState(
                3,
                DoomRewardRate.None,
                0,
                0,
                roundActive: false,
                rateSelectionOpen: false,
                mapCode: string.Empty,
                mapTitle: string.Empty);
        }

        public DoomRewardRoundResult HandleMapStart(int skill, string mapCode, string mapTitle)
        {
            _state = new DoomRewardRoundState(
                skill,
                DoomRewardRate.None,
                0,
                DoomRewardRoundLogic.ResetMultiplierStage(),
                roundActive: false,
                rateSelectionOpen: false,
                mapCode: mapCode,
                mapTitle: mapTitle);
            return default;
        }

        public DoomRewardRoundResult TryBeginRound(DoomRewardRate rate, bool canAfford, bool chargeEntryCost = true)
        {
            if (rate == DoomRewardRate.None)
            {
                return default;
            }

            var entryCost = chargeEntryCost ? DoomRewardRoundLogic.GetEntryCost(rate) : 0;
            if (chargeEntryCost && !canAfford)
            {
                _state = new DoomRewardRoundState(
                    _state.Skill,
                    DoomRewardRate.None,
                    0,
                    DoomRewardRoundLogic.ResetMultiplierStage(),
                    roundActive: false,
                    rateSelectionOpen: false,
                    _state.MapCode,
                    _state.MapTitle);
                return new DoomRewardRoundResult(entryCost: entryCost, insufficientFunds: true);
            }

            _state = new DoomRewardRoundState(
                _state.Skill,
                rate,
                0,
                DoomRewardRoundLogic.ResetMultiplierStage(),
                roundActive: true,
                rateSelectionOpen: false,
                _state.MapCode,
                _state.MapTitle);

            return new DoomRewardRoundResult(
                entryCost: entryCost,
                roundStarted: true,
                closedRateSelection: true);
        }

        public DoomRewardRoundResult HandleKill()
        {
            if (!_state.RoundActive || _state.CurrentRate == DoomRewardRate.None)
            {
                return default;
            }

            var usedStage = _state.MultiplierStage;
            var add = DoomRewardRoundLogic.CalculateKillPoolGain(_state.Skill, usedStage);
            if (add <= 0)
            {
                return default;
            }

            _state = new DoomRewardRoundState(
                _state.Skill,
                _state.CurrentRate,
                _state.CurrentPool + add,
                DoomRewardRoundLogic.AdvanceMultiplierStage(_state.MultiplierStage),
                roundActive: true,
                rateSelectionOpen: false,
                _state.MapCode,
                _state.MapTitle);

            return new DoomRewardRoundResult(poolDelta: add, usedMultiplierStage: usedStage);
        }

        public DoomRewardRoundResult HandleSecret(int count)
        {
            if (!_state.RoundActive || _state.CurrentRate == DoomRewardRate.None)
            {
                return default;
            }

            var add = DoomRewardRoundLogic.GetSecretPoolGain() * Math.Max(1, count);
            if (add <= 0)
            {
                return default;
            }

            _state = new DoomRewardRoundState(
                _state.Skill,
                _state.CurrentRate,
                _state.CurrentPool + add,
                _state.MultiplierStage,
                roundActive: true,
                rateSelectionOpen: false,
                _state.MapCode,
                _state.MapTitle);

            return new DoomRewardRoundResult(poolDelta: add);
        }

        public DoomRewardRoundResult HandleDamage()
        {
            if (!_state.RoundActive || _state.CurrentRate == DoomRewardRate.None)
            {
                return default;
            }

            var bonusReset = _state.MultiplierStage > 0;

            _state = new DoomRewardRoundState(
                _state.Skill,
                _state.CurrentRate,
                _state.CurrentPool,
                DoomRewardRoundLogic.ResetMultiplierStage(),
                roundActive: true,
                rateSelectionOpen: false,
                _state.MapCode,
                _state.MapTitle);

            return new DoomRewardRoundResult(bonusReset: bonusReset);
        }

        public DoomRewardRoundResult HandleClear(int clearBonus)
        {
            var rewardActive = _state.RoundActive && _state.CurrentRate != DoomRewardRate.None;
            _state = new DoomRewardRoundState(
                _state.Skill,
                DoomRewardRate.None,
                0,
                DoomRewardRoundLogic.ResetMultiplierStage(),
                roundActive: false,
                rateSelectionOpen: false,
                _state.MapCode,
                _state.MapTitle);

            return new DoomRewardRoundResult(
                bonusAmount: rewardActive ? clearBonus : 0,
                roundEnded: true);
        }

        public DoomRewardRoundResult HandleDeath()
        {
            _state = new DoomRewardRoundState(
                _state.Skill,
                DoomRewardRate.None,
                0,
                DoomRewardRoundLogic.ResetMultiplierStage(),
                roundActive: false,
                rateSelectionOpen: false,
                _state.MapCode,
                _state.MapTitle);

            return new DoomRewardRoundResult(roundEnded: true);
        }

        public DoomRewardRoundResult HandleAbort()
        {            
            _state = new DoomRewardRoundState(
                _state.Skill,
                DoomRewardRate.None,
                0,
                DoomRewardRoundLogic.ResetMultiplierStage(),
                roundActive: false,
                rateSelectionOpen: false,
                _state.MapCode,
                _state.MapTitle);

            return new DoomRewardRoundResult(roundEnded: true);
        }
    }
}
