using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Components.Timing;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Collectibles.Refills;
using Celeste.Game.Gameplay.Collectibles.Strawberries;
using Celeste.Graphics.Animation;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Entities;
using Celeste.Scene.Following;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.StrawberryJam.Entities.Refills.Shards
{
    public partial class RefillShard
    {
        public override void Render(ISourceRenderContext context)
        {
            if (sprite.Visible && renderShard)
                sprite.DrawOutline(context);

            outlineSprite.RenderPosition = start + new Vector2(0, sine.Value * 2f);
            outlineSprite.Render(context);

            if (renderShard)
                base.Render(context);
        }
    }
}
