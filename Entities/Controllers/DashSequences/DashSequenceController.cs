// SPDX-License-Identifier: MIT

using System;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.LevelLoader;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Player.Dash;

namespace Celeste.Mods.StrawberryJam.Entities.Controllers.DashSequences
{
    [CustomEntity("SJ2021/DashSequenceController")]
    [SceneTracked]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.DashSequenceController")]
    public class DashSequenceController : SceneEntityState
    {
        public Vector2[] DashCode;
        public string FlagLabel;
        public string FailureFlag;
        public int Index;

        private int codePosition;

        private DashSequenceDisplay display;

        public DashSequenceController(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            DashCode = data.Attr("dashCode", "*").ToUpper()
                .Split(',').Select(s => s switch
                {
                    "U" => -Vector2.UnitY,
                    "D" => Vector2.UnitY,
                    "L" => -Vector2.UnitX,
                    "R" => Vector2.UnitX,
                    "UL" => new Vector2(-1, -1),
                    "UR" => new Vector2(1, -1),
                    "DL" => new Vector2(-1, 1),
                    "DR" => new Vector2(1, 1),
                    _ => Vector2.Zero,
                }).ToArray();

            FlagLabel = data.Attr("flagLabel", "");
            FailureFlag = data.Attr("flagOnFailure", "");
            Index = data.Int("index");

            //stole this code from maddie480's helping hand set flag on spawn trigger hope u don't mind
            OriginalLevelSceneRoot level = null;
            if (engine.Frame.Scenes.CurrentScene is OriginalLevelSceneRoot)
            {
                level = engine.Frame.Scenes.CurrentScene as OriginalLevelSceneRoot;
            }
            else if (engine.Frame.Scenes.CurrentScene is OriginalLevelLoaderScene)
            {
                level = (engine.Frame.Scenes.CurrentScene as OriginalLevelLoaderScene).Level;
            }

            if (level != null)
            {
                for (int i = 1; i <= DashCode.Length; i++)
                {
                    level.Session.SetFlag(FlagLabel + "-" + i, false);
                }

                if (!string.IsNullOrEmpty(FailureFlag))
                {
                    level.Session.SetFlag(FailureFlag, false);
                }
            }

            Add(new DashListenerState { OnDash = OnDash });
            codePosition = 0;
        }


        public override void Update()
        {
            base.Update();
            if (display == null)
                display = ((ISourceScene)Scene).SourceScene.Entities.FindFirst<DashSequenceDisplay>();
        }

        private void OnDash(Vector2 direction)
        {
            if (display != null && Index == display.Index)
            {
                Vector2 dir = new Vector2(Math.Sign(direction.X), Math.Sign(direction.Y));
                if (DashCode[codePosition] == Vector2.Zero || dir == DashCode[codePosition])
                {
                    ((OriginalLevelSceneRoot)Scene).Session.SetFlag(FlagLabel + "-" + (codePosition + 1), true);
                    codePosition++;
                    display.ValidateInput();
                    if (codePosition >= DashCode.Length)
                    {
                        RemoveSelf();
                    }
                }
                else
                {
                    for (int i = 1; i <= DashCode.Length; i++)
                    {
                        ((OriginalLevelSceneRoot)Scene).Session.SetFlag(FlagLabel + "-" + i, false);
                    }

                    if (codePosition != 0 && !string.IsNullOrEmpty(FailureFlag))
                    {
                        ((OriginalLevelSceneRoot)Scene).Session.SetFlag(FailureFlag, true);
                    }

                    codePosition = 0;
                    display.Fail();
                }
            }
        }
    }
}
