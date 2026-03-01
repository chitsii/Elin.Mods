#if DEBUG
using System;
using System.Reflection;
using ReflexCLI;
using ReflexCLI.Attributes;

namespace Elin_QuestMod.DebugTools
{
    [ConsoleCommandClassCustomizer("questmod.debug")]
    public static class QuestModDebugConsole
    {
        private const string ShowcaseBookId = "drama_quest_drama_feature_showcase";
        private const string ShowcaseDramaId = "quest_drama_feature_showcase";

        public static void Register()
        {
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                bool alreadyRegistered = false;
                for (int i = 0; i < CommandRegistry.assemblies.Count; i++)
                {
                    Assembly existing = CommandRegistry.assemblies[i];
                    if (existing == asm || string.Equals(existing?.FullName, asm.FullName, StringComparison.Ordinal))
                    {
                        alreadyRegistered = true;
                        break;
                    }
                }

                if (!alreadyRegistered)
                {
                    CommandRegistry.assemblies.Add(asm);
                }

                ModLog.Info("Debug console commands ready: questmod.debug.*");
            }
            catch (Exception ex)
            {
                ModLog.Warn("Debug console command registration failed: " + ex.Message);
            }
        }

        [ConsoleCommand("showcase")]
        public static string Showcase()
        {
            return StartShowcase();
        }

        [ConsoleCommand("showcase.help")]
        public static string ShowcaseHelp()
        {
            return string.Join(
                "\n",
                "questmod.debug.showcase",
                "cwl.cs.eval Elin_QuestMod.DebugTools.QuestModDebugConsole.StartShowcase();",
                "cwl.cs.eval Elin_QuestMod.DebugTools.QuestModDebugConsole.EvalShowcase();");
        }

        public static string StartShowcase()
        {
            if (EClass.pc == null)
            {
                return "questmod.debug.showcase failed: EClass.pc is null.";
            }

            if (EClass.ui == null)
            {
                return "questmod.debug.showcase failed: EClass.ui is null.";
            }

            try
            {
                CloseActiveDramaLayer();
                LayerDrama layer = LayerDrama.Activate(ShowcaseBookId, ShowcaseDramaId, null, EClass.pc, null, null);
                bool activated = layer != null || LayerDrama.Instance != null;
                if (!activated)
                {
                    return "questmod.debug.showcase failed: LayerDrama.Activate returned null.";
                }

                return "questmod.debug.showcase started: " + ShowcaseDramaId;
            }
            catch (Exception ex)
            {
                return "questmod.debug.showcase failed: " + ex.Message;
            }
        }

        public static string EvalShowcase()
        {
            string command = "questmod.debug.showcase";
            try
            {
                object result = Utils.ExecuteCommand(ref command);
                return result?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                return "EvalShowcase failed: " + ex.Message;
            }
        }

        private static void CloseActiveDramaLayer()
        {
            LayerDrama layer = LayerDrama.Instance;
            if (layer == null)
            {
                return;
            }

            try
            {
                if (layer.drama != null && layer.drama.sequence != null)
                {
                    layer.drama.sequence.Exit();
                }

                layer.Close();
            }
            catch
            {
            }
        }
    }
}
#endif
