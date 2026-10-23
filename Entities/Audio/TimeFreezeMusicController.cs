// SPDX-License-Identifier: MIT

using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Audio;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Startup;
using Celeste.Game.Runtime;
using Celeste.Mods.Crystalline.Runtime;
using Celeste.Mods.StrawberryJam.Runtime.Audio;
using Celeste.Scene.Entities;

namespace Celeste.Mods.StrawberryJam.Entities.Audio
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.CrystallineHelperTimeFreezeMusicController")]
    public class TimeFreezeMusicController : SceneEntityState
    {
        private readonly GameApplication engine;
        private readonly TimeFreezeMusicBinding binding;
        public float paramOff, paramOn;
        public string paramName;
        public bool prevValue;

        public TimeFreezeMusicController(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = (GameApplication)engine;
            binding = TimeFreezeMusicBinding.For(engine);
            paramName = data.Attr("paramName");
            paramOff = data.Float("paramOff");
            paramOn = data.Float("paramOn");
            var tags = ((IOriginalGameTagsEnvironment)engine).Tags;
            Tags = tags.Global.Value | tags.Persistent.Value;
        }

        public override void Update()
        {
            base.Update();
            bool value = binding.State.StopStage == 1;
            if (value != prevValue)
            {
                var audio = ((OriginalLevelSceneRoot)Scene).Session.Audio;
                audio.Music.Param(paramName, value ? paramOn : paramOff);
                OriginalAudioSession.Apply(audio, engine.AudioSession.ChannelOutput, engine.AudioApplyPolicy);
            }

            prevValue = value;
        }
    }
}
