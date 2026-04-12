using System;
using Elin_Elinikki.Quest.Quest;

namespace Elin_Elinikki.Quest.Drama
{
    /// <summary>
    /// Elinikki drama dependency resolver. Handles both the generic template
    /// keys inherited from Elin_QuestMod (cmd.quest.*, state.quest.*, fx.pc.*,
    /// cue.*) and the Elinikki-specific stage machine keys:
    ///
    /// - <c>state.elinikki.stage.at_least.&lt;stage_name&gt;</c> — returns true
    ///   if the current quest stage is at least the named stage. Stage names
    ///   are the <see cref="ElinikkiQuestStage"/> members, snake_case lower
    ///   (e.g. <c>accepted</c>, <c>layer1_clear</c>, <c>yuu_found</c>).
    /// - <c>cmd.elinikki.stage.advance.&lt;stage_name&gt;</c> — advances the
    ///   quest to the named stage via <see cref="ElinikkiQuestFlow.TryAdvanceStage"/>.
    ///   Forward-only: a key targeting an earlier stage is a no-op (returns true
    ///   so the drama continues).
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

            const string elinikkiStageAtLeastPrefix = "state.elinikki.stage.at_least.";
            if (key.StartsWith(elinikkiStageAtLeastPrefix, StringComparison.Ordinal))
            {
                string stageName = key.Substring(elinikkiStageAtLeastPrefix.Length);
                if (TryParseStage(stageName, out ElinikkiQuestStage target))
                {
                    ElinikkiQuestStage current = ElinikkiQuestStageExtensions.GetCurrentStage();
                    value = (int)current >= (int)target;
                    return true;
                }

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

            const string elinikkiStageAdvancePrefix = "cmd.elinikki.stage.advance.";
            if (key.StartsWith(elinikkiStageAdvancePrefix, StringComparison.Ordinal))
            {
                string stageName = key.Substring(elinikkiStageAdvancePrefix.Length);
                if (TryParseStage(stageName, out ElinikkiQuestStage target))
                {
                    // TryAdvanceStage returns false when target is not strictly
                    // greater than the current stage; that is still a "success"
                    // from the drama's perspective (the requested state holds),
                    // so we return true regardless.
                    ElinikkiQuestFlow.TryAdvanceStage(target);
                    return true;
                }

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

        /// <summary>
        /// Parses a stage name from a drama key suffix. Accepts snake_case
        /// lower (e.g. <c>layer1_clear</c>, <c>yuu_found</c>) and maps to the
        /// corresponding <see cref="ElinikkiQuestStage"/> member. Returns
        /// false if the suffix is empty or does not match any stage.
        /// </summary>
        private static bool TryParseStage(string stageName, out ElinikkiQuestStage stage)
        {
            stage = ElinikkiQuestStage.NotStarted;
            if (string.IsNullOrWhiteSpace(stageName))
            {
                return false;
            }

            // Normalize snake_case -> PascalCase for Enum.TryParse.
            // e.g. layer1_clear -> Layer1Clear, yuu_found -> YuuFound.
            string normalized = SnakeCaseToPascalCase(stageName);
            return Enum.TryParse(normalized, ignoreCase: true, result: out stage)
                && Enum.IsDefined(typeof(ElinikkiQuestStage), stage);
        }

        private static string SnakeCaseToPascalCase(string snake)
        {
            if (string.IsNullOrEmpty(snake))
            {
                return string.Empty;
            }

            var parts = snake.Split('_');
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (part.Length == 0)
                {
                    continue;
                }

                sb.Append(char.ToUpperInvariant(part[0]));
                if (part.Length > 1)
                {
                    sb.Append(part.Substring(1));
                }
            }

            return sb.ToString();
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
