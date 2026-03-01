using System;
using Elin_SukutsuArena.Core;
using Elin_SukutsuArena.Flags;
using StoryPhase = Elin_SukutsuArena.Quests.StoryPhase;

namespace Elin_SukutsuArena.State
{
    /// <summary>
    /// プレイヤーの状態管理
    /// </summary>
    public class PlayerState
    {
        private readonly IFlagStorage _storage;

        public PlayerState(IFlagStorage storage)
        {
            _storage = storage;
        }

        // --- Rank ---
        public Rank Rank
        {
            get
            {
                var value = _storage.GetInt(ArenaFlagKeys.Rank, 0);
                if (value < 0 || value > 15) return Rank.Unranked;
                return (Rank)value;
            }
            set => _storage.SetInt(ArenaFlagKeys.Rank, (int)value);
        }

        public bool IsRankAtLeast(Rank minRank)
        {
            return Rank >= minRank;
        }

        // --- Phase ---
        // Phase (Flags) = dialogFlags用の永続ストレージ。
        // StoryPhase (Quests) = クエストフェーズ用。数値はPhaseと同じ前提。
        public Phase CurrentPhase
        {
            get
            {
                var value = _storage.GetInt(ArenaFlagKeys.CurrentPhase, 0);
                if (value < 0 || value > 7) return Phase.Prologue;
                return (Phase)value;
            }
            set => _storage.SetInt(ArenaFlagKeys.CurrentPhase, (int)value);
        }

        public StoryPhase CurrentStoryPhase
        {
            get
            {
                var value = _storage.GetInt(ArenaFlagKeys.CurrentPhase, 0);
                if (value < 0 || value > 7) return StoryPhase.Prologue;
                return (StoryPhase)value;
            }
            set => _storage.SetInt(ArenaFlagKeys.CurrentPhase, (int)value);
        }

        // --- Boolean Flags ---
        public bool IsFugitive
        {
            get => GetBool(ArenaFlagKeys.FugitiveStatus);
            set => SetBool(ArenaFlagKeys.FugitiveStatus, value);
        }

        // --- Choice Flags ---
        public BottleChoice? GetBottleChoice()
        {
            var value = _storage.GetInt(ArenaFlagKeys.BottleChoice, -1);
            if (value < 0 || value >= 2) return null;
            return (BottleChoice)value;
        }

        public void SetBottleChoice(BottleChoice choice)
        {
            _storage.SetInt(ArenaFlagKeys.BottleChoice, (int)choice);
        }

        public KainSoulChoice? GetKainSoulChoice()
        {
            var value = _storage.GetInt(ArenaFlagKeys.KainSoulChoice, -1);
            if (value < 0 || value >= 2) return null;
            return (KainSoulChoice)value;
        }

        public void SetKainSoulChoice(KainSoulChoice choice)
        {
            _storage.SetInt(ArenaFlagKeys.KainSoulChoice, (int)choice);
        }

        public BalgasChoice? GetBalgasChoice()
        {
            var value = _storage.GetInt(ArenaFlagKeys.BalgasKilled, -1);
            if (value < 0 || value >= 2) return null;
            return (BalgasChoice)value;
        }

        public void SetBalgasChoice(BalgasChoice choice)
        {
            _storage.SetInt(ArenaFlagKeys.BalgasKilled, (int)choice);
        }

        public LilyBottleConfession? GetLilyBottleConfession()
        {
            var value = _storage.GetInt(ArenaFlagKeys.LilyBottleConfession, -1);
            if (value < 0 || value >= 3) return null;
            return (LilyBottleConfession)value;
        }

        public void SetLilyBottleConfession(LilyBottleConfession choice)
        {
            _storage.SetInt(ArenaFlagKeys.LilyBottleConfession, (int)choice);
        }

        public Ending? GetEnding()
        {
            var value = _storage.GetInt(ArenaFlagKeys.Ending, -1);
            if (value < 0 || value >= 3) return null;
            return (Ending)value;
        }

        public void SetEnding(Ending ending)
        {
            _storage.SetInt(ArenaFlagKeys.Ending, (int)ending);
        }

        // --- Private Helpers ---
        private bool GetBool(string key)
        {
            return _storage.GetInt(key) != 0;
        }

        private void SetBool(string key, bool value)
        {
            _storage.SetInt(key, value ? 1 : 0);
        }

    }
}
