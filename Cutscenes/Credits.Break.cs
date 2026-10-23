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
        [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Cutscenes.Credits+Break")]
        private class Break : CreditNode
        {
            public float Size;

            public Break(float size = 64f)
            {
                Size = size;
            }

            public override void Render(ISourceRenderContext context, Credits owner, Vector2 position,
                float alignment = 0.5f, float scale = 1f)
            {
            }

            public override float Height(Credits owner, float scale = 1f)
            {
                return Size * scale;
            }
        }
    }
}
