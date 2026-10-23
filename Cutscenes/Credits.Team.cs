using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Runtime.Startup;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display.Colors;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Text;
using Celeste.Localization.Lookup;
using Celeste.Scene.Entities;

namespace Celeste.Mods.StrawberryJam.Cutscenes
{
    public partial class Credits
    {
        [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Cutscenes.Credits+Team")]
        public class Team : CreditNode
        {
            public const float TitleScale = 1.7f;
            public const float CreditsScale = 1.15f;
            public const float Spacing = 8f;
            public const float SubtitleScale = 0.9f;
            public const float SectionSpacing = 32f;

            public static readonly PackedColor TitleColor = PackedColor.White;
            public static readonly PackedColor CreditsColor = PackedColor.White * 0.8f;
            public static readonly PackedColor SubtitleColor = SourceHexColor.HexToColor("a8a694");

            public string Title;
            public Section[] Sections;

            public Team(Credits owner, string titleKey, params Section[] sections)
            {
                Title = owner.Dialog.Clean(titleKey);
                Sections = sections;
            }

            public Team(Credits owner, string titleKey, string creditsKey, bool split = false)
                : this(owner, titleKey, new Section(owner, null, creditsKey, split))
            {
            }

            public override void Render(ISourceRenderContext context, Credits owner, Vector2 position,
                float alignment = 0.5f, float scale = 1f)
            {
                Vector2 renderPos = Floor(position);
                Vector2 justify = new(alignment, 0f);
                Vector2 leftAlign = Vector2.Zero;
                Vector2 rightAlign = new(1f, 0f);
                Vector2 margin = new(owner.LineHeight * CreditsScale * scale / 3, 0f);

                owner.ActiveFont.DrawOutline(context, owner, Title, renderPos, justify,
                    Vector2.One * TitleScale * scale, TitleColor, 2f, BorderColor);
                renderPos.Y += (owner.LineHeight * TitleScale + Spacing) * scale;

                for (int i = 0; i < Sections.Length; i++)
                {
                    if (!string.IsNullOrEmpty(Sections[i].Subtitle))
                    {
                        owner.ActiveFont.DrawOutline(context, owner, Sections[i].Subtitle, renderPos, justify,
                            Vector2.One * SubtitleScale * scale, SubtitleColor, 2f, BorderColor);
                        renderPos.Y += owner.LineHeight * SubtitleScale * scale;
                    }

                    for (int j = 0; j < Sections[i].Credits.Length; j++)
                    {
                        if (!Sections[i].Split)
                        {
                            owner.ActiveFont.DrawOutline(context, owner, Sections[i].Credits[j], renderPos, justify,
                                Vector2.One * CreditsScale * scale, CreditsColor, 2f, BorderColor);
                        }
                        else
                        {
                            owner.ActiveFont.DrawOutline(context, owner, Sections[i].Credits[j], renderPos - margin,
                                rightAlign, Vector2.One * CreditsScale * scale, CreditsColor, 2f, BorderColor);
                            if (j < Sections[i].Credits.Length - 1)
                            {
                                owner.ActiveFont.DrawOutline(context, owner, Sections[i].Credits[j + 1],
                                    renderPos + margin, leftAlign, Vector2.One * CreditsScale * scale, CreditsColor, 2f,
                                    BorderColor);
                                j++;
                            }
                        }

                        renderPos.Y += owner.LineHeight * CreditsScale * scale;
                    }

                    renderPos.Y += SectionSpacing * scale;
                }
            }

            public override float Height(Credits owner, float scale = 1f)
            {
                float height = 0f;
                height += owner.LineHeight * TitleScale + Spacing;
                for (int i = 0; i < Sections.Length; i++)
                {
                    if (!string.IsNullOrEmpty(Sections[i].Subtitle))
                    {
                        height += owner.LineHeight * SubtitleScale;
                    }

                    int lineCount = Sections[i].Split
                        ? (Sections[i].Credits.Length / 2 + Sections[i].Credits.Length % 2)
                        : Sections[i].Credits.Length;
                    height += owner.LineHeight * CreditsScale * lineCount;
                }

                height += SectionSpacing * (Sections.Length - 1);
                return height * scale;
            }

            [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Cutscenes.Credits+Team+Section")]
            public struct Section
            {
                public string Subtitle;
                public string[] Credits;
                public bool Split;

                public Section(Credits owner, string subtitleKey, string creditsKey, bool split = false)
                {
                    Subtitle = !string.IsNullOrEmpty(subtitleKey) ? owner.Dialog.Clean(subtitleKey) : "";
                    Credits = owner.Dialog.Clean(creditsKey)
                        .Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                    Split = split;
                }
            }
        }
    }
}
