using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display;
using Celeste.Graphics.Rendering;
using Celeste.Simulation.Presentation.Backdrops.Runtime;

namespace Celeste.Mods.StrawberryJam.Backdrops.Meteors
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Effects.MeteorShower")]
    public class MeteorShower : BackdropPresentation
    {
        private struct Meteor
        {
            public Vector2 Position;
            public int TextureSet;
            public float Timer;
            public float RateFPS;
            public float Rotation;
            public float ResetTime;
        }

        private readonly ISourceEngineContext engine;
        private readonly ISourceRandom random;
        private readonly List<Meteor> meteors = [];
        private readonly List<AtlasEntry>[] textures;
        public int MeteorCount;

        public MeteorShower(ISourceEngineContext engine, int numMeteors)
        {
            this.engine = engine;
            random = ((ISourceRandomEnvironment)engine).Random;
            MeteorCount = numMeteors;
            var atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
            textures =
            [
                atlas.GetAtlasSubtextures("bgs/StrawberryJam2021/meteors/arcComet"),
                atlas.GetAtlasSubtextures("bgs/StrawberryJam2021/meteors/slantComet")
            ];
            for (var i = 0; i < MeteorCount; i++) meteors.Add(NewMeteor());
        }

        private Meteor NewMeteor() => new()
        {
            Position = new Vector2(SourceRandomMath.NextFloat(random, 320f),
                SourceRandomMath.NextFloat(random, 180f) - 20f),
            Timer = 0f,
            RateFPS = 12.5f + SourceRandomMath.NextFloat(random, 7.5f),
            TextureSet = random.Next(textures.Length),
            Rotation = SourceRandomMath.NextFloat(random, (float)Math.PI / 3f),
            ResetTime = 3f + SourceRandomMath.NextFloat(random, 2f)
        };

        public override void Update(ISourceBackdropScene scene)
        {
            base.Update(scene);
            if (!Visible) return;
            for (var i = 0; i < meteors.Count; i++)
            {
                var meteor = meteors[i];
                var timer = meteor.Timer + engine.Frame.DeltaTime;
                if (timer > meteor.ResetTime) meteors[i] = NewMeteor();
                else
                {
                    meteor.Timer = timer;
                    meteors[i] = meteor;
                }
            }

            for (var i = meteors.Count; i < MeteorCount; i++) meteors.Add(NewMeteor());
            for (var i = meteors.Count - 1; i >= MeteorCount; i--)
                if ((int)(meteors[i].Timer * meteors[i].RateFPS) >= textures[meteors[i].TextureSet].Count)
                    meteors.RemoveAt(i);
        }

        public override void Render(ISourceRenderContext context, ISourceBackdropScene scene)
        {
            base.Render(context, scene);
            foreach (var meteor in meteors)
            {
                var frame = (int)(meteor.Timer * meteor.RateFPS);
                if (frame >= textures[meteor.TextureSet].Count) continue;
                var texture = textures[meteor.TextureSet][frame];
                var draw = new AtlasTextureDraw(texture, meteor.Position, texture.Center, PackedColor.White,
                    Vector2.One, meteor.Rotation);
                ((ISourceRenderCommandSink<AtlasTextureDraw>)context).Draw(in draw, null);
            }
        }
    }
}
