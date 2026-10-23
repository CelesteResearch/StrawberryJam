using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Geometry;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Components.Talking;
using Celeste.Graphics.Content.Gui;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;

namespace Celeste.Mods.StrawberryJam.Cutscenes
{
    [SceneTracked]
    [CustomEntity("SJ2021/CreditsTalker")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Cutscenes.CreditsTalker")]
    public sealed class CreditsTalker : SceneEntityState
    {
        private readonly ISourceEngineContext engine;

        public CreditsTalker(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            var display = new OriginalTalkComponent.HoverDisplay
            {
                InputPosition = new Vector2(0f, -75f),
                Texture = ((ISourceGuiAtlasResources)engine).GuiAtlas.Entries["SJ2021/Lobbies/credits_talker"]
            };
            Add(new OriginalTalkComponent((IOriginalTalkProcess)engine,
                new IntRect(0, 0, data.Width, data.Height), data.Nodes[0] + offset - Position, OnTalk, display)
            {
                PlayerMustBeFacing = false
            });
        }

        private void OnTalk(RoomPlayerSession player)
            => ((ISourceScene)Scene).SourceScene.QueueSourceEntity(new CreditsCutscene(engine));
    }
}
