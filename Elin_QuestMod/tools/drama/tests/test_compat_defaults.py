from pathlib import Path
import os
import subprocess
import tempfile
import textwrap
import unittest

from tools.drama.drama_builder import DramaBuilder


ROOT = Path(__file__).resolve().parents[3]


def _read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


class CompatDefaultTests(unittest.TestCase):
    def test_point_compat_preserves_allow_installed_default_true(self):
        text = _read("src/Compat/PointCompat.cs")
        self.assertIn("bool allowInstalled = true", text)

    def test_resolve_flag_uses_resolve_flag_contract(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.resolve_flag("state.quest.can_start.quest_drama_replace_me", "tmp.flag")
        self.assertEqual("eval", builder.entries[0]["action"])
        self.assertEqual(
            'Elin_QuestMod.Drama.DramaRuntime.ResolveFlag("state.quest.can_start.quest_drama_replace_me", "tmp.flag");',
            builder.entries[0]["param"],
        )

    def test_resolve_run_uses_resolve_run_contract(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.resolve_run("cmd.quest.try_start.quest_drama_replace_me")
        self.assertEqual("eval", builder.entries[0]["action"])
        self.assertEqual(
            'Elin_QuestMod.Drama.DramaRuntime.ResolveRun("cmd.quest.try_start.quest_drama_replace_me");',
            builder.entries[0]["param"],
        )

    def test_removed_dependency_wrapper_apis_are_absent(self):
        builder = DramaBuilder(mod_name="QuestMod")
        self.assertFalse(hasattr(builder, "sync_flag_from_dependency"))
        self.assertFalse(hasattr(builder, "run_dependency_command"))

    def test_mod_dll_api_catalog_is_explicit(self):
        apis = DramaBuilder.get_mod_dll_dependent_apis()
        self.assertIn("resolve_flag", apis)
        self.assertIn("resolve_run", apis)
        self.assertIn("quest_try_start", apis)
        self.assertNotIn("set_background", apis)

    def test_show_book_default_category_is_book(self):
        builder = DramaBuilder(mod_name="QuestMod")
        builder.show_book("questmod_feature_guide")
        self.assertEqual("invoke*", builder.entries[0]["action"])
        self.assertEqual("show_book(Book/questmod_feature_guide)", builder.entries[0]["param"])

    def test_pc_fx_runtime_uses_card_fx_signatures_and_reports_result(self):
        runtime_text = _read("src/Drama/GameQuestDramaRuntimeContext.cs")
        resolver_text = _read("src/Drama/QuestDramaResolver.cs")

        self.assertIn("public bool PlayPcEffect", runtime_text)
        self.assertIn("typeof(string), typeof(bool), typeof(float), typeof(Vector3)", runtime_text)
        self.assertIn("typeof(string), typeof(float), typeof(bool)", runtime_text)
        self.assertIn("new object[] { effectId, true, 0f, default(Vector3) }", runtime_text)
        self.assertIn("new object[] { soundId, 1f, true }", runtime_text)
        self.assertNotIn("new[] { typeof(string) }", runtime_text)
        self.assertIn("return _ctx.PlayPcEffect(effectId);", resolver_text)
        self.assertIn("return _ctx.PlayPcEffect(effectPart, soundPart);", resolver_text)

    def test_pc_fx_runtime_executes_against_source_linked_fixture(self):
        self._run_source_linked_fx_fixture()

    def _run_source_linked_fx_fixture(self):
        runtime_text = _read("src/Drama/GameQuestDramaRuntimeContext.cs")
        resolver_text = _read("src/Drama/QuestDramaResolver.cs")
        resolver_interface_text = _read("src/Drama/IDramaDependencyResolver.cs")

        with tempfile.TemporaryDirectory() as td:
            root = Path(td)
            (root / "GameQuestDramaRuntimeContext.cs").write_text(runtime_text, encoding="utf-8")
            (root / "QuestDramaResolver.cs").write_text(resolver_text, encoding="utf-8")
            (root / "IDramaDependencyResolver.cs").write_text(
                resolver_interface_text, encoding="utf-8"
            )
            (root / "Stubs.cs").write_text(_FX_FIXTURE_STUBS, encoding="utf-8")
            (root / "Program.cs").write_text(_FX_FIXTURE_PROGRAM, encoding="utf-8")
            (root / "QuestModFxFixture.csproj").write_text(
                textwrap.dedent(
                    """\
                    <Project Sdk="Microsoft.NET.Sdk">
                      <PropertyGroup>
                        <OutputType>Exe</OutputType>
                        <TargetFramework>net10.0</TargetFramework>
                        <ImplicitUsings>disable</ImplicitUsings>
                        <Nullable>disable</Nullable>
                      </PropertyGroup>
                    </Project>
                    """
                ),
                encoding="utf-8",
            )
            (root / "NuGet.Config").write_text(
                textwrap.dedent(
                    """\
                    <?xml version="1.0" encoding="utf-8"?>
                    <configuration>
                      <packageSources>
                        <clear />
                      </packageSources>
                    </configuration>
                    """
                ),
                encoding="utf-8",
            )

            env = os.environ.copy()
            env["DOTNET_CLI_HOME"] = str(root / "dotnet-home")
            env["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
            env["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1"
            build = subprocess.run(
                [
                    "dotnet",
                    "build",
                    str(root / "QuestModFxFixture.csproj"),
                    "-p:RestoreSources=",
                    "-p:RestoreIgnoreFailedSources=true",
                    "-p:RestoreConfigFile=" + str(root / "NuGet.Config"),
                ],
                cwd=root,
                env=env,
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                check=False,
            )
            self.assertEqual(0, build.returncode, build.stdout)

            result = subprocess.run(
                [
                    "dotnet",
                    str(root / "bin" / "Debug" / "net10.0" / "QuestModFxFixture.dll"),
                ],
                cwd=root,
                env=env,
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                check=False,
            )
            self.assertEqual(0, result.returncode, result.stdout)
            self.assertIn("fixture-ok", result.stdout)


_FX_FIXTURE_STUBS = textwrap.dedent(
    """\
    using System;
    using System.Collections.Generic;

    namespace UnityEngine
    {
        public struct Vector3
        {
            public float x;
            public float y;
            public float z;
        }
    }

    namespace Elin_QuestMod
    {
        public static class ModLog
        {
            public static readonly List<string> Messages = new List<string>();
            public static void Info(string message) { Messages.Add("INFO:" + message); }
            public static void Warn(string message) { Messages.Add("WARN:" + message); }
            public static void Error(string message) { Messages.Add("ERROR:" + message); }
        }
    }

    namespace Elin_QuestMod.Quest
    {
        public static class QuestStateService
        {
            public static bool IsQuestCompleted(string dramaId) { return false; }
            public static void CompleteQuest(string dramaId) { }
            public static string BuildFlagKey(string key) { return "quest." + key; }
        }

        public static class QuestFlow
        {
            public static int PulseCount;
            public static void Pulse() { PulseCount++; }
        }
    }

    public static class EClass
    {
        public static Card pc;
        public static Player player = new Player();
        public static object ui = new object();
        public static World world = new World();
    }

    public sealed class Player
    {
        public Dictionary<string, int> dialogFlags = new Dictionary<string, int>();
    }

    public sealed class World
    {
        public WorldDate date = new WorldDate();
    }

    public sealed class WorldDate
    {
        public int GetRaw() { return 12345; }
    }

    public sealed class LayerDrama
    {
        public static LayerDrama Activate(string bookId, string dramaId, object a, Card pc, object b, object c)
        {
            return new LayerDrama();
        }
    }

    public sealed class Effect { }
    public sealed class SoundSource { }

    public sealed class EffectCall
    {
        public Card Target;
        public string Id;
        public bool UseRenderPos;
        public float Range;
        public UnityEngine.Vector3 Fix;
    }

    public sealed class SoundCall
    {
        public Card Target;
        public string Id;
        public float Volume;
        public bool Spatial;
    }

    public class Card
    {
        public readonly string Name;
        public bool ThrowEffect;
        public bool ThrowSound;
        public readonly List<EffectCall> EffectCalls = new List<EffectCall>();
        public readonly List<SoundCall> SoundCalls = new List<SoundCall>();

        public Card(string name)
        {
            Name = name;
        }

        public Effect PlayEffect(string id, bool useRenderPos = true, float range = 0f, UnityEngine.Vector3 fix = default(UnityEngine.Vector3))
        {
            EffectCalls.Add(new EffectCall { Target = this, Id = id, UseRenderPos = useRenderPos, Range = range, Fix = fix });
            if (ThrowEffect)
            {
                throw new InvalidOperationException("effect-failed");
            }

            return new Effect();
        }

        public SoundSource PlaySound(string id, float v = 1f, bool spatial = true)
        {
            SoundCalls.Add(new SoundCall { Target = this, Id = id, Volume = v, Spatial = spatial });
            if (ThrowSound)
            {
                throw new InvalidOperationException("sound-failed");
            }

            return new SoundSource();
        }
    }
    """
)


_FX_FIXTURE_PROGRAM = textwrap.dedent(
    """\
    using System;
    using Elin_QuestMod.Drama;

    public static class Program
    {
        public static int Main()
        {
            TestPcEffectUsesPcAndVanillaDefaults();
            TestSoundStillRunsWhenEffectFails();
            TestEffectStillRunsWhenSoundFails();
            TestResolverPropagatesFxFailure();
            Console.WriteLine("fixture-ok");
            return 0;
        }

        private static void TestPcEffectUsesPcAndVanillaDefaults()
        {
            var pc = new Card("pc");
            EClass.pc = pc;
            bool ok = new GameQuestDramaRuntimeContext().PlayPcEffect("teleport", "revive");

            Require(ok, "combined effect/sound should succeed");
            Require(pc.EffectCalls.Count == 1, "effect call count");
            Require(pc.SoundCalls.Count == 1, "sound call count");
            Require(object.ReferenceEquals(pc.EffectCalls[0].Target, pc), "effect target should be EClass.pc");
            Require(pc.EffectCalls[0].Id == "teleport", "effect id");
            Require(pc.EffectCalls[0].UseRenderPos, "effect useRenderPos default");
            Require(pc.EffectCalls[0].Range == 0f, "effect range default");
            Require(pc.EffectCalls[0].Fix.x == 0f && pc.EffectCalls[0].Fix.y == 0f && pc.EffectCalls[0].Fix.z == 0f, "effect fix default");
            Require(object.ReferenceEquals(pc.SoundCalls[0].Target, pc), "sound target should be EClass.pc");
            Require(pc.SoundCalls[0].Id == "revive", "sound id");
            Require(pc.SoundCalls[0].Volume == 1f, "sound volume default");
            Require(pc.SoundCalls[0].Spatial, "sound spatial default");
        }

        private static void TestSoundStillRunsWhenEffectFails()
        {
            var pc = new Card("pc") { ThrowEffect = true };
            EClass.pc = pc;
            bool ok = new GameQuestDramaRuntimeContext().PlayPcEffect("bad_fx", "revive");

            Require(!ok, "effect failure should return false");
            Require(pc.EffectCalls.Count == 1, "failed effect should be attempted");
            Require(pc.SoundCalls.Count == 1, "sound should still run after effect failure");
        }

        private static void TestEffectStillRunsWhenSoundFails()
        {
            var pc = new Card("pc") { ThrowSound = true };
            EClass.pc = pc;
            bool ok = new GameQuestDramaRuntimeContext().PlayPcEffect("teleport", "bad_sound");

            Require(!ok, "sound failure should return false");
            Require(pc.EffectCalls.Count == 1, "effect should run before sound failure");
            Require(pc.SoundCalls.Count == 1, "failed sound should be attempted");
        }

        private static void TestResolverPropagatesFxFailure()
        {
            var ctx = new FakeContext { PlayPcEffectResult = false };
            var resolver = new QuestDramaResolver(ctx);
            bool ok = resolver.TryExecute("fx.pc.spark+sfx.pc.chime");

            Require(!ok, "resolver should return false when FX execution fails");
            Require(ctx.EffectId == "spark", "resolver effect id");
            Require(ctx.SoundId == "chime", "resolver sound id");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception(message);
            }
        }

        private sealed class FakeContext : IQuestDramaRuntimeContext
        {
            public bool PlayPcEffectResult;
            public string EffectId;
            public string SoundId;

            public bool CanStartDrama(string dramaId) { return false; }
            public bool IsDramaDone(string dramaId) { return false; }
            public bool TryStartDrama(string dramaId) { return false; }
            public bool TryStartDramaRepeatable(string dramaId) { return false; }
            public bool TryStartDramaUntilComplete(string dramaId) { return false; }
            public void CompleteDrama(string dramaId) { }
            public bool RunCue(string cueKey) { return false; }

            public bool PlayPcEffect(string effectId, string soundId = null)
            {
                EffectId = effectId;
                SoundId = soundId;
                return PlayPcEffectResult;
            }
        }
    }
    """
)


if __name__ == "__main__":
    unittest.main()
