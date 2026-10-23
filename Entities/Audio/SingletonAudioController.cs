using System.Numerics;
using Celeste.Audio.Fmod.Events;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Core.Mathematics;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Execution.Startup;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;

namespace Celeste.Mods.StrawberryJam.Entities.Audio
{
    [SceneTracked]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias(
        "Celeste.Mod.StrawberryJam2021.Entities.SingletonAudioController")]
    public class SingletonAudioController : SceneEntityState
    {
        private static readonly string sourcePlayerRegistryKey =
            typeof(global::Celeste.Simulation.RoomPlayerSession).FullName;

        private readonly ISourceEngineContext engine;

        public static SingletonAudioController Ensure(object scene)
        {
            var scheduler = ((ISourceScene)scene).SourceScene;
            foreach (var existing in scheduler.Entities.Entities)
                if (existing is SingletonAudioController controller)
                    return controller;
            foreach (var pending in scheduler.GetToAdd())
                if (pending is SingletonAudioController controller)
                    return controller;
            var created = new SingletonAudioController(((ISourceEngineContextScene)scene).EngineContext);
            scheduler.QueueSourceEntity(created);
            return created;
        }

        public SingletonAudioController(ISourceEngineContext engine) : base(((ISourceEntitySourceDataEnvironment)engine)
            .EntitySourceDataRegistration)
        {
            this.engine = engine;
            Collidable = Visible = false;
            Active = true;
            Tags = ((IOriginalGameTagsEnvironment)engine).Tags.Persistent.Value;
        }

        public void Play(string path, Vector2 origin, float muffleTime = .1f, float playerRange = 320) =>
            Play(path, origin, null, muffleTime, playerRange);

        public void Play(string path, SceneEntityState entity, float muffleTime = .1f, float playerRange = 320) =>
            Play(path, Vector2.Zero, entity, muffleTime, playerRange);

        private SingletonComponent Find(string path)
        {
            foreach (var component in OwnedComponents)
                if (component is SingletonComponent singleton && singleton.AudioPath == path)
                    return singleton;
            return null;
        }

        private void Play(string path, Vector2 origin, SceneEntityState entity, float muffleTime, float playerRange)
        {
            var component = Find(path);
            if (component == null) Add(component = new SingletonComponent(engine, path));
            if (!component.Active && Scene is OriginalLevelSceneRoot level &&
                (entity != null && entity.Right >= level.Camera.Left - 32f && entity.Left <= level.Camera.Right + 32f &&
                 entity.Bottom >= level.Camera.Top - 32f && entity.Top <= level.Camera.Bottom + 32f ||
                 entity == null && origin.X >= level.Camera.Left - 32f && origin.X <= level.Camera.Right + 32f &&
                 origin.Y >= level.Camera.Top - 32f &&
                 origin.Y <= level.Camera.Bottom + 32f)) component.Play(origin, muffleTime);
            if (component.Active) component.Move(entity?.Center ?? origin);
        }

        public void SetParam(string path, string parameter, float value) => Find(path)?.SetParam(parameter, value);
        public void Stop(string path) => Find(path)?.Stop();

        private class SingletonComponent : SourceComponentState
        {
            private readonly ISourceEngineContext engine;
            private readonly ISourceVectorArithmetic vectors;
            public string AudioPath { get; }
            private float muffleTimeRemaining;
            private FmodEventReference instance;
            private Vector2 lastOrigin;

            public SingletonComponent(ISourceEngineContext engine, string path) : base(false, false)
            {
                this.engine = engine;
                vectors = ((ISourceVectorEnvironment)engine).VectorArithmetic;
                AudioPath = path;
            }

            public override void Update()
            {
                muffleTimeRemaining -= engine.DeltaTime;
                if (muffleTimeRemaining <= 0) Active = false;
            }

            public void Play(Vector2 origin, float muffleTime)
            {
                muffleTimeRemaining = muffleTime;
                lastOrigin = origin;
                Active = true;
                instance = ((IOriginalGameplayProcess)engine).AudioSession.Play(AudioPath, origin,
                    ((IOriginalGameplayProcess)engine).SaveContext);
            }

            public void SetParam(string parameter, float value)
            {
                if (instance != null)
                    ((IOriginalGameplayProcess)engine).AudioSession.SetParameter(instance, parameter, value);
            }

            public void Stop()
            {
                if (instance != null) ((IOriginalGameplayProcess)engine).AudioSession.Stop(instance);
            }

            public void Move(Vector2 target)
            {
                if (instance == null || Scene is not ISourceScene scene ||
                    scene.SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>(sourcePlayerRegistryKey) is not
                        { } player) return;
                if (vectors.LengthSquared(lastOrigin - player.Center) > vectors.LengthSquared(target - player.Center))
                {
                    ((IOriginalGameplayProcess)engine).AudioSession.Position(instance, target,
                        ((IOriginalGameplayProcess)engine).SaveContext);
                    lastOrigin = target;
                }
            }
        }
    }
}
