using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Rendering;
using Celeste.Scene.Entities;

namespace Celeste.Mods.StrawberryJam.Entities.Ascension
{
    public partial class CustomAscendManager
    {
        [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.CustomAscendManager+Fader")]
        public class Fader : SceneEntityState
        {
            public float Fade;
            private readonly CustomAscendManager manager;

            public Fader(ISourceEngineContext engine, CustomAscendManager manager)
                : base(((Celeste.Core.Components.ISourceEntitySourceDataEnvironment)engine)
                    .EntitySourceDataRegistration)
            {
                this.manager = manager;
                Depth = -1000010;
            }

            public override void Render(ISourceRenderContext context)
            {
                if (Fade > 0f)
                {
                    var position = ((OriginalLevelSceneRoot)Scene).Camera.Position;
                    SourcePrimitiveDrawing.Rect(context, this, position.X - 10f, position.Y - 10f, 340f, 200f,
                        (manager.Dark ? PackedColor.Black : PackedColor.White) * Fade);
                }
            }
        }
    }
}
