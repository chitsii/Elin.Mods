using System.Collections;
using UnityEngine;

namespace Elin_SukutsuArena.Effects;

/// <summary>
/// LUT（カラーグレーディング）エフェクト用ヘルパー
/// 演出用の一時的なLUT切り替えを提供
/// </summary>
public static class LutEffect
{
    /// <summary>
    /// LUTを一時的に切り替える（フェードトランジション付き）
    /// </summary>
    /// <param name="lutName">切り替え先のLUT名（例: "LUT_Invert"）</param>
    /// <param name="duration">効果時間（秒）</param>
    /// <param name="fadeTime">フェード時間（秒）</param>
    public static void Flash(string lutName, float duration = 2f, float fadeTime = 0.3f)
    {
        if (EClass._map == null) return;
        EMono.core.StartCoroutine(FlashCoroutine(lutName, duration, fadeTime));
    }

    private static IEnumerator FlashCoroutine(string lutName, float duration, float fadeTime)
    {
        var config = EClass._map.config;
        var originalLut = config.idLut;
        var originalBrightness = config.lutBrightness;

        // フェードアウト（暗くする）
        yield return FadeBrightness(config, originalBrightness, 0f, fadeTime);

        // LUT切り替え
        config.idLut = lutName;
        EClass.scene.ApplyZoneConfig();

        // フェードイン（明るくする）
        yield return FadeBrightness(config, 0f, originalBrightness, fadeTime);

        // 効果時間待機
        yield return new WaitForSeconds(duration - fadeTime * 2);

        // フェードアウト（暗くする）
        yield return FadeBrightness(config, originalBrightness, 0f, fadeTime);

        // LUT戻し
        config.idLut = originalLut;
        EClass.scene.ApplyZoneConfig();

        // フェードイン（明るくする）
        yield return FadeBrightness(config, 0f, originalBrightness, fadeTime);

        ModLog.Log($"[SukutsuArena] LUT flash completed: {lutName} -> {originalLut}");
    }

    private static IEnumerator FadeBrightness(MapConfig config, float from, float to, float duration)
    {
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            config.lutBrightness = Mathf.Lerp(from, to, t / duration);
            EClass.scene.ApplyZoneConfig();
            yield return null;
        }
        config.lutBrightness = to;
        EClass.scene.ApplyZoneConfig();
    }
}

