//
// Copyright (C) 1993-1996 Id Software, Inc.
// Copyright (C) 2019-2020 Nobuaki Tanaka
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//



using System;

namespace ManagedDoom.Video
{
    public class OpeningSequenceRenderer
    {
        private DrawScreen screen;
        private Renderer parent;

        private PatchCache cache;

        public OpeningSequenceRenderer(Wad wad, DrawScreen screen, Renderer parent)
        {
            this.screen = screen;
            this.parent = parent;

            cache = new PatchCache(wad);
        }

        public void Render(OpeningSequence sequence, Fixed frameFrac)
        {
            var scale = screen.Width / 320;

            switch (sequence.State)
            {
                case OpeningSequenceState.Title:
                    screen.DrawPatch(cache["TITLEPIC"], 0, 0, scale);
                    DrawPlaytimeOverlay(scale);
                    break;

                case OpeningSequenceState.Demo:
                    parent.RenderGame(sequence.DemoGame, frameFrac);
                    break;

                case OpeningSequenceState.Credit:
                    screen.DrawPatch(cache["CREDIT"], 0, 0, scale);
                    break;
            }
        }

        private void DrawPlaytimeOverlay(int scale)
        {
            var totalSeconds = Elin_JustDoomIt.DoomGlobalStatsStore.GetTotalPlaySeconds();
            var text = "PLAYTIME " + FormatDuration(totalSeconds);
            var padding = 4 * scale;
            var width = screen.MeasureText(text, scale);
            var boxWidth = width + padding * 2;
            var boxHeight = 10 * scale;
            var x = (screen.Width - boxWidth) / 2;
            var y = screen.Height - boxHeight - (4 * scale);

            screen.FillRect(x, y, boxWidth, boxHeight, 0);
            screen.DrawText(text, x + padding, y + boxHeight - (2 * scale), scale);
        }

        private static string FormatDuration(int totalSeconds)
        {
            var sec = Math.Max(0, totalSeconds);
            var hours = sec / 3600;
            var minutes = (sec % 3600) / 60;
            var seconds = sec % 60;

            if (hours > 0)
            {
                return hours + "H " + minutes.ToString("00") + "M";
            }

            if (minutes > 0)
            {
                return minutes + "M " + seconds.ToString("00") + "S";
            }

            return seconds + "S";
        }
    }
}
