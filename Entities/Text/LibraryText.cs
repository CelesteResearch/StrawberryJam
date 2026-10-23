using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Execution.Startup;
using Celeste.Graphics.Text;
using Celeste.Localization.Lookup;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.StrawberryJam.Entities.Text
{
    [SceneTracked]
    [CustomEntity("SJ2021/LibraryText")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.LibraryText")]
    public sealed class LibraryText : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly string[] text;
        private readonly bool outline;
        private ActiveBitmapFont ActiveFont => ((ISourceActiveFontScope)engine).ActiveFont;

        public LibraryText(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            Tags = ((IOriginalGameTagsEnvironment)engine).Tags.HUD.Value;
            text = ((ISourceDialogEnvironment)engine).Dialog.Clean(data.Attr("dialog", "app_ending"))
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            outline = data.Bool("outline");
        }

        public override void Render(ISourceRenderContext context)
        {
            Vector2 camera = ((OriginalLevelSceneRoot)Scene).Camera.Position;
            Vector2 position = (Position - camera) * 6f;
            var saveContext = ((IOriginalGameplayProcess)engine).SaveContext;
            if (saveContext.CurrentSave != null && saveContext.CurrentSave.Assists.MirrorMode)
                position.X = 1920f - position.X;
            position.Y -= (text.Length - 1) * ActiveFont.LineHeight / 2;
            foreach (string line in text)
            {
                if (outline)
                    ActiveFont.DrawOutline(context, this, line, position, new Vector2(.5f, .5f),
                        Vector2.One * 1.25f, PackedColor.White, 2f, PackedColor.Black);
                else
                    ActiveFont.Draw(context, this, line, position, new Vector2(.5f, .5f),
                        Vector2.One * 1.25f, PackedColor.White);
                position.Y += ActiveFont.LineHeight;
            }
        }
    }
}
