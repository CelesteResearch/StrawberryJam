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
        [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Cutscenes.Credits+Heading")]
        public class Heading : CreditNode
        {
            public const float HeadingScale = 2.5f;

            public readonly PackedColor HeadingColor;
            public string HeadingString;

            public Heading(Credits owner, string headingKey)
            {
                HeadingColor = PackedColor.Gray;
                HeadingString = owner.Dialog.Clean(headingKey);
            }

            public override void Render(ISourceRenderContext context, Credits owner, Vector2 position,
                float alignment = 0.5F, float scale = 1)
            {
                owner.ActiveFont.DrawEdgeOutline(context, owner, HeadingString, Floor(position),
                    new Vector2(alignment, 0f), Vector2.One * HeadingScale * scale, PackedColor.Gray, 4f,
                    PackedColor.DarkSlateBlue, 2f, BorderColor);
            }

            public override float Height(Credits owner, float scale = 1f)
            {
                return owner.LineHeight * HeadingScale * scale;
            }
        }
    }
}
