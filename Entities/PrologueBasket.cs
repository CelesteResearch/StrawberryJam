using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Components.Talking;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering;
using Celeste.Mods.StrawberryJam.Cutscenes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;

namespace Celeste.Mods.StrawberryJam.Entities
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.PrologueBasket")]
    [CustomEntity("SJ2021/PrologueBasket")]
    public sealed class PrologueBasket : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly AtlasEntry sprite;
        private readonly Vector2 drawPosition;

        public PrologueBasket(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            Depth = 2000;
            Add(new OriginalTalkComponent((IOriginalTalkProcess)engine,
                    new IntRect(-24, -8, 48, 40), new Vector2(0f, -8f), Interact)
                { PlayerMustBeFacing = false });
            drawPosition = data.Position + offset;
            sprite = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas
                .Entries["objects/StrawberryJam2021/prologueBasket/basket"];
        }

        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);
            SourceAtlasDrawing.DrawCentered(context, this, sprite, drawPosition, PackedColor.White);
        }

        private void Interact(RoomPlayerSession player)
            => ((ISourceScene)Scene).SourceScene.AddEntity(new PrologueOutroCutscene(engine, player, this));
    }
}
