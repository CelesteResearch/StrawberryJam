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
        [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Cutscenes.Credits+Thanks")]
        public class Thanks : CreditNode
        {
            public const float ThanksScale = 1.5f;

            private readonly AtlasEntry texture;
            private readonly string message;

            public Thanks(Credits owner, string dialogKey, AtlasEntry texture)
            {
                message = owner.Dialog.Clean(dialogKey);
                this.texture = texture;
            }

            public override void Render(ISourceRenderContext context, Credits owner, Vector2 position,
                float alignment = 0.5f, float scale = 1f)
            {
                SourceAtlasDrawing.DrawCentered(context, owner, texture, new Vector2(960f, position.Y + 540f),
                    PackedColor.White);
                owner.ActiveFont.DrawOutline(context, owner, message,
                    new Vector2(960f, position.Y + 540f + texture.FrameHeight / 2f + CreditSpacing),
                    new Vector2(0.5f, 0.5f),
                    Vector2.One * ThanksScale, PackedColor.White, 2f, BorderColor);
            }

            public override float Height(Credits owner, float scale = 1f)
            {
                return 1080f;
            }
        }
    }
}
