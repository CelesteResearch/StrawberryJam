using System;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Entities.Decoration.Decals;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.StrawberryJam.Entities.Decals
{
    [SceneTracked(false)]
    [CustomEntity("SJ2021/MaskedDecal")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.MaskedDecal")]
    public class MaskedDecal : SceneEntityState
    {
        private static readonly Func<OriginalLevelSceneRoot, OriginalDecalFactory> ReadFactory = CreateFactoryReader();
        public OriginalDecalEntity Decal;

        private static Func<OriginalLevelSceneRoot, OriginalDecalFactory> CreateFactoryReader()
        {
            var method = new DynamicMethod("ReadMaskedDecalFactory", typeof(OriginalDecalFactory),
                new[] { typeof(OriginalLevelSceneRoot) }, typeof(MaskedDecal), true);
            var generator = method.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld,
                typeof(OriginalLevelSceneRoot).GetField("sourceDecalFactory",
                    BindingFlags.Instance | BindingFlags.NonPublic));
            generator.Emit(OpCodes.Ret);
            return (Func<OriginalLevelSceneRoot, OriginalDecalFactory>)method.CreateDelegate(
                typeof(Func<OriginalLevelSceneRoot, OriginalDecalFactory>));
        }

        public MaskedDecal(OriginalLevelSceneRoot level, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)level.EngineContext).EntitySourceDataRegistration,
                data.Position + offset)
        {
            Decal = ReadFactory(level).Create(new OriginalDecalPlacement
            {
                Texture = data.Attr("texture", ""), Position = Position,
                Scale = new Vector2(data.Float("scaleX", 1f), data.Float("scaleY", 1f))
            }, Vector2.Zero, -10500);
            Decal.Visible = false;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            ((ISourceScene)scene).SourceScene.QueueSourceEntity(Decal);
        }

        public static MaskedDecal Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new MaskedDecal(level, data, offset);
    }
}
