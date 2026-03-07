using System;

namespace Elin_JustDoomIt
{
    public readonly struct DoomResolutionPreset
    {
        public DoomResolutionPreset(string id, string label, int width, int height)
        {
            Id = id;
            Label = label;
            Width = width;
            Height = height;
        }

        public string Id { get; }
        public string Label { get; }
        public int Width { get; }
        public int Height { get; }
    }

    public static class DoomVideoSettings
    {
        public static readonly DoomResolutionPreset[] ResolutionPresets =
        {
            new DoomResolutionPreset("small", "SMALL", 320, 200),
            new DoomResolutionPreset("medium", "MEDIUM", 640, 400),
            new DoomResolutionPreset("large", "LARGE", 960, 600)
        };

        public static int GetClosestResolutionPresetIndex(int width, int height)
        {
            var bestIndex = 0;
            var bestDistance = int.MaxValue;
            for (var i = 0; i < ResolutionPresets.Length; i++)
            {
                var preset = ResolutionPresets[i];
                var distance = Math.Abs(width - preset.Width) + Math.Abs(height - preset.Height);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        public static DoomResolutionPreset GetCurrentResolutionPreset(int width, int height)
        {
            return ResolutionPresets[GetClosestResolutionPresetIndex(width, height)];
        }

        public static string FormatResolutionSummary(int width, int height)
        {
            var preset = GetCurrentResolutionPreset(width, height);
            return preset.Label + " (" + preset.Width + "x" + preset.Height + ")";
        }

        public static string FormatBrightnessSummary(int brightness)
        {
            var clamped = brightness < 0 ? 0 : (brightness > 10 ? 10 : brightness);
            return clamped + " / 10";
        }
    }
}
