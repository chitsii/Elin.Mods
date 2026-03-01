using System;

namespace Elin_QuestMod.Drama
{
    /// <summary>
    /// QuestMod local resolver for drama dependency keys.
    /// Each copied mod can keep its own resolver/context implementation.
    /// </summary>
    public sealed class QuestDramaResolver : IDramaDependencyResolver
    {
        private readonly IQuestDramaRuntimeContext _ctx;

        public QuestDramaResolver(IQuestDramaRuntimeContext ctx)
        {
            _ctx = ctx ?? new GameQuestDramaRuntimeContext();
        }

        public bool TryResolveBool(string key, out bool value)
        {
            value = false;
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            switch (key)
            {
                case "state.quest.can_start.quest_drama_replace_me":
                    value = _ctx.CanStartDrama("quest_drama_replace_me");
                    return true;
                case "state.quest.is_done.quest_drama_replace_me":
                    value = _ctx.IsDramaDone("quest_drama_replace_me");
                    return true;
                case "state.quest.can_start.quest_drama_feature_showcase":
                    value = _ctx.CanStartDrama("quest_drama_feature_showcase");
                    return true;
                case "state.quest.is_done.quest_drama_feature_showcase":
                    value = _ctx.IsDramaDone("quest_drama_feature_showcase");
                    return true;
                case "state.quest.can_start.quest_drama_feature_followup":
                    value = _ctx.CanStartDrama("quest_drama_feature_followup");
                    return true;
                case "state.quest.is_done.quest_drama_feature_followup":
                    value = _ctx.IsDramaDone("quest_drama_feature_followup");
                    return true;
            }

            const string canStartPrefix = "state.quest.can_start.";
            if (key.StartsWith(canStartPrefix, StringComparison.Ordinal))
            {
                string dramaId = key.Substring(canStartPrefix.Length);
                if (string.IsNullOrEmpty(dramaId))
                {
                    return false;
                }

                value = _ctx.CanStartDrama(dramaId);
                return true;
            }

            const string isDonePrefix = "state.quest.is_done.";
            if (key.StartsWith(isDonePrefix, StringComparison.Ordinal))
            {
                string dramaId = key.Substring(isDonePrefix.Length);
                if (string.IsNullOrEmpty(dramaId))
                {
                    return false;
                }

                value = _ctx.IsDramaDone(dramaId);
                return true;
            }

            return false;
        }

        public bool TryExecute(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            if (TryExecuteGenericFxCommand(key))
            {
                return true;
            }

            switch (key)
            {
                case "cmd.quest.try_start.quest_drama_replace_me":
                    return _ctx.TryStartDrama("quest_drama_replace_me");
                case "cmd.quest.complete.quest_drama_replace_me":
                    _ctx.CompleteDrama("quest_drama_replace_me");
                    return true;
                case "cmd.quest.try_start.quest_drama_feature_showcase":
                    return _ctx.TryStartDrama("quest_drama_feature_showcase");
                case "cmd.quest.try_start_repeatable.quest_drama_feature_showcase":
                    return _ctx.TryStartDramaRepeatable("quest_drama_feature_showcase");
                case "cmd.quest.try_start_until_complete.quest_drama_feature_showcase":
                    return _ctx.TryStartDramaUntilComplete("quest_drama_feature_showcase");
                case "cmd.quest.complete.quest_drama_feature_showcase":
                    _ctx.CompleteDrama("quest_drama_feature_showcase");
                    return true;
                case "cmd.quest.try_start.quest_drama_feature_followup":
                    return _ctx.TryStartDrama("quest_drama_feature_followup");
                case "cmd.quest.complete.quest_drama_feature_followup":
                    _ctx.CompleteDrama("quest_drama_feature_followup");
                    return true;
                case "cue.questmod.placeholder_pulse":
                    return _ctx.RunCue("cue.questmod.placeholder_pulse");
                case "cue.questmod.feature_showcase_pulse":
                    return _ctx.RunCue("cue.questmod.feature_showcase_pulse");
            }

            const string tryStartUntilCompletePrefix = "cmd.quest.try_start_until_complete.";
            if (key.StartsWith(tryStartUntilCompletePrefix, StringComparison.Ordinal))
            {
                string dramaId = key.Substring(tryStartUntilCompletePrefix.Length);
                if (string.IsNullOrEmpty(dramaId))
                {
                    return false;
                }

                return _ctx.TryStartDramaUntilComplete(dramaId);
            }

            const string tryStartRepeatablePrefix = "cmd.quest.try_start_repeatable.";
            if (key.StartsWith(tryStartRepeatablePrefix, StringComparison.Ordinal))
            {
                string dramaId = key.Substring(tryStartRepeatablePrefix.Length);
                if (string.IsNullOrEmpty(dramaId))
                {
                    return false;
                }

                return _ctx.TryStartDramaRepeatable(dramaId);
            }

            const string tryStartPrefix = "cmd.quest.try_start.";
            if (key.StartsWith(tryStartPrefix, StringComparison.Ordinal))
            {
                string dramaId = key.Substring(tryStartPrefix.Length);
                if (string.IsNullOrEmpty(dramaId))
                {
                    return false;
                }

                return _ctx.TryStartDrama(dramaId);
            }

            const string completePrefix = "cmd.quest.complete.";
            if (key.StartsWith(completePrefix, StringComparison.Ordinal))
            {
                string dramaId = key.Substring(completePrefix.Length);
                if (string.IsNullOrEmpty(dramaId))
                {
                    return false;
                }

                _ctx.CompleteDrama(dramaId);
                return true;
            }

            const string cuePrefix = "cue.";
            if (key.StartsWith(cuePrefix, StringComparison.Ordinal))
            {
                return _ctx.RunCue(key);
            }

            return false;
        }

        private bool TryExecuteGenericFxCommand(string key)
        {
            const string fxPrefix = "fx.pc.";
            const string fxSfxDelimiter = "+sfx.pc.";

            if (!key.StartsWith(fxPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            int delimiter = key.IndexOf(fxSfxDelimiter, StringComparison.Ordinal);
            if (delimiter < 0)
            {
                string effectId = key.Substring(fxPrefix.Length);
                if (string.IsNullOrEmpty(effectId))
                {
                    return false;
                }

                _ctx.PlayPcEffect(effectId);
                return true;
            }

            string effectPart = key.Substring(fxPrefix.Length, delimiter - fxPrefix.Length);
            string soundPart = key.Substring(delimiter + fxSfxDelimiter.Length);
            if (string.IsNullOrEmpty(effectPart) || string.IsNullOrEmpty(soundPart))
            {
                return false;
            }

            _ctx.PlayPcEffect(effectPart, soundPart);
            return true;
        }
    }

    public interface IQuestDramaRuntimeContext
    {
        bool CanStartDrama(string dramaId);
        bool IsDramaDone(string dramaId);
        bool TryStartDrama(string dramaId);
        bool TryStartDramaRepeatable(string dramaId);
        bool TryStartDramaUntilComplete(string dramaId);
        void CompleteDrama(string dramaId);
        bool RunCue(string cueKey);
        void PlayPcEffect(string effectId, string soundId = null);
    }
}
