using Elin_CommonDrama;

namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Ars Moriendi implementation of drama dependency resolution.
    /// </summary>
    public sealed class ArsDramaResolver : IDramaDependencyResolver
    {
        private readonly IArsDramaRuntimeContext _ctx;

        public ArsDramaResolver(IArsDramaRuntimeContext ctx)
        {
            _ctx = ctx;
        }

        public bool TryResolveBool(string key, out bool value)
        {
            value = false;
            if (string.IsNullOrEmpty(key)) return false;

            const string questIsCompletePrefix = "state.quest.is_complete.";
            if (key.StartsWith(questIsCompletePrefix))
            {
                string dramaId = key.Substring(questIsCompletePrefix.Length);
                if (string.IsNullOrEmpty(dramaId)) return false;
                value = _ctx.IsDramaComplete(dramaId);
                return true;
            }

            const string questCanStartPrefix = "state.quest.can_start.";
            if (key.StartsWith(questCanStartPrefix))
            {
                string dramaId = key.Substring(questCanStartPrefix.Length);
                if (string.IsNullOrEmpty(dramaId)) return false;
                value = _ctx.CanStartDrama(dramaId);
                return true;
            }

            switch (key)
            {
                case "state.quest.is_complete":
                case "quest.is_complete":
                    value = _ctx.IsQuestComplete();
                    return true;
                case "state.erenos.is_borrowed":
                case "erenos.is_borrowed":
                    value = _ctx.IsErenosBorrowed();
                    return true;
                default:
                    return false;
            }
        }

        public bool TryExecute(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;

            const string questTryStartUntilCompletePrefix = "cmd.quest.try_start_until_complete.";
            if (key.StartsWith(questTryStartUntilCompletePrefix))
            {
                string dramaId = key.Substring(questTryStartUntilCompletePrefix.Length);
                if (string.IsNullOrEmpty(dramaId)) return false;
                _ctx.TryStartDramaUntilComplete(dramaId);
                return true;
            }

            const string questTryStartRepeatablePrefix = "cmd.quest.try_start_repeatable.";
            if (key.StartsWith(questTryStartRepeatablePrefix))
            {
                string dramaId = key.Substring(questTryStartRepeatablePrefix.Length);
                if (string.IsNullOrEmpty(dramaId)) return false;
                _ctx.TryStartDramaRepeatable(dramaId);
                return true;
            }

            const string questTryStartPrefix = "cmd.quest.try_start.";
            if (key.StartsWith(questTryStartPrefix))
            {
                string dramaId = key.Substring(questTryStartPrefix.Length);
                if (string.IsNullOrEmpty(dramaId)) return false;
                _ctx.TryStartDrama(dramaId);
                return true;
            }

            if (TryExecuteGenericFxCommand(key))
                return true;

            switch (key)
            {
                case "cmd.erenos.ensure_near_player":
                case "erenos.ensure_near_player":
                    _ctx.EnsureErenosNearPlayerForDrama();
                    return true;
                case "cmd.erenos.borrow":
                case "erenos.borrow":
                    _ctx.BorrowErenos();
                    return true;
                case "cmd.scene.stop_bgm":
                case "scene.stop_bgm":
                    _ctx.StopBgmNow();
                    return true;
                case "cmd.hecatia.party_show":
                case "hecatia.party_show":
                    _ctx.PlayHecatiaPartyShow();
                    return true;
                case "cmd.hecatia.set_party_portrait":
                case "hecatia.set_party_portrait":
                    _ctx.SetTalkPortrait("UN_ars_hecatia_happy");
                    return true;
                case "cue.apotheosis.darkwomb":
                case "apotheosis.fx.darkwomb":
                    _ctx.PlayPcEffect("darkwomb");
                    return true;
                case "cue.apotheosis.silence":
                case "apotheosis.fx.silence":
                    _ctx.StopBgmNow();
                    return true;
                case "cue.apotheosis.curse_burst":
                case "apotheosis.fx.curse_burst":
                    _ctx.PlayPcEffect("curse", "curse3");
                    return true;
                case "cue.apotheosis.revive":
                case "apotheosis.fx.revive":
                    _ctx.PlayPcEffect("revive");
                    return true;
                case "cue.apotheosis.mutation":
                case "apotheosis.fx.mutation":
                    _ctx.PlayPcEffect("mutation");
                    return true;
                case "cue.apotheosis.teleport_rebirth":
                case "apotheosis.fx.teleport_rebirth":
                    _ctx.PlayPcEffect("teleport", "revive");
                    return true;
                default:
                    return false;
            }
        }

        private bool TryExecuteGenericFxCommand(string key)
        {
            if (key == "cmd.scene.stop_bgm")
            {
                _ctx.StopBgmNow();
                return true;
            }
            if (key == "scene.stop_bgm")
            {
                _ctx.StopBgmNow();
                return true;
            }

            const string fxPrefix = "fx.pc.";
            const string fxSfxDelimiter = "+sfx.pc.";
            if (!key.StartsWith(fxPrefix))
                return false;

            int delimiter = key.IndexOf(fxSfxDelimiter);
            if (delimiter < 0)
            {
                string effectId = key.Substring(fxPrefix.Length);
                if (string.IsNullOrEmpty(effectId)) return false;
                _ctx.PlayPcEffect(effectId);
                return true;
            }

            string effectPart = key.Substring(fxPrefix.Length, delimiter - fxPrefix.Length);
            string soundPart = key.Substring(delimiter + fxSfxDelimiter.Length);
            if (string.IsNullOrEmpty(effectPart) || string.IsNullOrEmpty(soundPart)) return false;
            _ctx.PlayPcEffect(effectPart, soundPart);
            return true;
        }
    }

    public interface IArsDramaRuntimeContext
    {
        bool IsQuestComplete();
        bool IsDramaComplete(string dramaId);
        bool IsErenosBorrowed();
        bool CanStartDrama(string dramaId);
        bool TryStartDrama(string dramaId);
        bool TryStartDramaRepeatable(string dramaId);
        bool TryStartDramaUntilComplete(string dramaId);
        void EnsureErenosNearPlayerForDrama();
        void BorrowErenos();
        void StopBgmNow();
        void PlayPcEffect(string effectId, string? soundId = null);
        void PlayHecatiaPartyShow();
        void SetTalkPortrait(string portraitId);
    }
}
