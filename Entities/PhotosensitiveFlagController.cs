using Celeste.Core.Reflection.Runtime;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Settings;
using Celeste.Scene.Entities;

namespace Celeste.Mods.StrawberryJam.Entities
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.PhotosensitiveFlagController")]
    [CustomEntity("SJ2021/PhotosensitiveFlagController")]
    public sealed class PhotosensitiveFlagController : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        public readonly string FlagName;

        public PhotosensitiveFlagController(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            FlagName = data.Attr("flag");
        }

        public void UpdateFlag()
        {
            var session = ((OriginalLevelSceneRoot)Scene).Session;
            var disableFlashes = ((ISettingsContext)engine).Settings.DisableFlashes;
            if (FlagName.Length > 0 && session.GetFlag(FlagName) != disableFlashes)
                session.SetFlag(FlagName, disableFlashes);
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            UpdateFlag();
        }

        public override void Update()
        {
            base.Update();
            UpdateFlag();
        }
    }
}
