using Celeste.Scene.Tracking;
using System;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Hazards.Puffer;
using Celeste.Mods.StrawberryJam.Entities.Strawberries;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Strawberries
{
    public sealed class ExplodingStrawberryHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private Hook hook;

        public ExplodingStrawberryHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            hook = new Hook(
                typeof(OriginalPufferState).GetMethod("Explode", BindingFlags.Instance | BindingFlags.NonPublic),
                (Action<Action<OriginalPufferState>, OriginalPufferState>)OnExplode);
        }

        private void OnExplode(Action<OriginalPufferState> orig, OriginalPufferState self)
        {
            if (self.Scene is ISourceEngineContextScene owner && ReferenceEquals(owner.EngineContext, engine))
            {
                var scene = (ISourceScene)engine.Frame.Scenes.CurrentScene;
                foreach (ExplodingStrawberry entity in scene.SourceScene.Entities.GetTrackedEntities(
                             typeof(ExplodingStrawberry)))
                    entity.BeginExplosion();
            }

            orig(self);
        }

        public void Dispose()
        {
            hook?.Dispose();
            hook = null;
        }
    }
}
