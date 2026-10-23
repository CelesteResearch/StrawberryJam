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
        [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Cutscenes.Credits+Special")]
        public class Special : CreditNode
        {
            public const float TitleScale = 1.7f;
            public const float CreditsScale = 1.15f;
            public const float Spacing = 8f;
            public const float RoleScale = 0.9f;

            public static readonly PackedColor TitleColor = PackedColor.White;
            public static readonly PackedColor CreditsColor = PackedColor.White * 0.8f;
            public static readonly PackedColor RoleColor = SourceHexColor.HexToColor("a8a694");

            public string Title;
            public string[] Credits;

            public Special(Credits owner, string titleKey, string creditsKey)
            {
                Title = owner.Dialog.Clean(titleKey);
                Credits = owner.Dialog.Clean(creditsKey)
                    .Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            }

            public override void Render(ISourceRenderContext context, Credits owner, Vector2 position,
                float alignment = 0.5f, float scale = 1f)
            {
                Vector2 renderPos = Floor(position);
                Vector2 justify = new(alignment, 0f);

                owner.ActiveFont.DrawOutline(context, owner, Title, renderPos, justify,
                    Vector2.One * TitleScale * scale, TitleColor, 2f, BorderColor);
                renderPos.Y += (owner.LineHeight * TitleScale + Spacing) * scale;

                for (int i = 0; i < Credits.Length; i++)
                {
                    owner.ActiveFont.DrawOutline(context, owner, Credits[i], renderPos, justify,
                        Vector2.One * CreditsScale * scale, CreditsColor, 2f, BorderColor);
                    renderPos.Y += owner.LineHeight * CreditsScale * scale;
                    if (++i < Credits.Length)
                    {
                        owner.ActiveFont.DrawOutline(context, owner, Credits[i], renderPos, justify,
                            Vector2.One * RoleScale * scale, RoleColor, 2f, BorderColor);
                        renderPos.Y += owner.LineHeight * RoleScale * scale;
                    }
                }
            }

            public override float Height(Credits owner, float scale = 1f)
            {
                float height = 0f;
                height += owner.LineHeight * TitleScale + Spacing;
                height += owner.LineHeight * CreditsScale * (Credits.Length / 2);
                height += owner.LineHeight * RoleScale * (Credits.Length / 2);
                return height * scale;
            }
        }
    }
}
