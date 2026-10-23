using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Celeste.Core.Components;
using Celeste.Core.Components.Animation;
using Celeste.Core.Display;
using Celeste.Core.Maps;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Game.Execution.Startup;
using Celeste.Game.Gameplay.Hazards.Lightning;
using Celeste.Scene.Entities;
using Celeste.Scene.Following;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;
using Celeste.Simulation.Gameplay.Player.Intro;
using Level = Celeste.Game.Execution.Levels.OriginalLevelSceneRoot;
using LevelData = Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData;
using EntityData = Celeste.Formats.Map.Entities.Structure.EntityData;
using Color = Celeste.Core.Display.PackedColor;

namespace Celeste.Mods.StrawberryJam.Triggers.Teleportation
{
    [Celeste.Everest.Tracking.Attributes.EverestTrackedAs(typeof(SourceTriggerEntityState))]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Triggers.InstantTeleportTrigger")]
    [CustomEntity("SJ2021/BasicInstantTeleportTrigger = Basic", "SJ2021/MainInstantTeleportTrigger = Main",
        "SJ2021/CustomInstantTeleportTrigger = Custom")]
    public partial class InstantTeleportTrigger : SourceTriggerEntityState
    {
        private readonly ISourceEngineContext engine;
        private static readonly PropertyInfo[] colorProperties = typeof(Color).GetProperties();
        private ISourceVectorArithmetic VectorArithmetic => ((ISourceVectorEnvironment)engine).VectorArithmetic;

        private Celeste.Scene.Scheduling.SceneTagSet GlobalTag =>
            ((IOriginalGameTagsEnvironment)engine).Tags.Global.Value;

        private float Approach(float value, float target, float amount) => ScalarMath.Approach(value, target, amount,
            ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);

        private float Clamp(float value, float min, float max) => ScalarMath.Clamp(value, min, max,
            ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);

        private bool FlagsMatch()
        {
            if (level == null) return false;
            bool b = true;
            if (flags.Length == 1 && flags[0] == "") return true;
            foreach (string f in flags)
                b &= f[0] != '!' ? level.Session.GetFlag(f) : !level.Session.GetFlag(f.TrimStart('!'));
            return b;
        }

        public enum TransitionType
        {
            None,
            Lightning,
            GlitchEffect,
            ColorFlash
        }

        public Level level;
        public string[] flags;
        public string currentRoom, newRoom;
        public Vector2 currentPos, newPos;
        public float[] vel, cameraOffset;
        public float rot, vel2;
        public TransitionType transition;
        public bool velMod, rotMod;
        public bool dreaming;
        public bool addTriggerOffset;

        //new stuff
        public bool onExit;
        public bool differentSide;

        private bool triggered = false;
        private float timeSlowDown;
        private Color flashColor;
        private float delay;
        private int legacyCamera;
        private bool resetDashes;

        //new stuff
        private int direction = -1;

        private bool doFlash;
        private float flash;

        public static SceneEntityState Basic(Level level, LevelData levelData, Vector2 offset,
            Celeste.Game.Content.Maps.Entities.OriginalLevelEntityData entityData) =>
            new InstantTeleportTrigger(level.EngineContext, entityData, offset);

        public static SceneEntityState Main(Level level, LevelData levelData, Vector2 offset,
            Celeste.Game.Content.Maps.Entities.OriginalLevelEntityData entityData) =>
            new InstantTeleportTrigger(level.EngineContext, entityData, offset);

        public static SceneEntityState Custom(Level level, LevelData levelData, Vector2 offset,
            Celeste.Game.Content.Maps.Entities.OriginalLevelEntityData entityData) =>
            new InstantTeleportTrigger(level.EngineContext, entityData, offset);

        public InstantTeleportTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
            data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
            newRoom = data.Attr("WarpRoom", currentRoom).Trim();
            newPos = new Vector2(data.Float("newPosX", -1f), data.Float("newPosY", -1f));
            cameraOffset = new float[] { data.Float("CameraOffsetX", 0f), data.Float("CameraOffsetY", 0f) };
            velMod = data.Bool("VelocityModifier", false);
            vel = new float[]
                { data.Float("ExitVelocityX", velMod ? 1 : 0), data.Float("ExitVelocityY", velMod ? 1 : 0) };
            vel2 = data.Float("ExitVelocityS", -1);
            rotMod = data.Bool("RotationType", false);
            rot = 0 - ((float)Math.PI * data.Float("RotationActor", 0) / 180);
            dreaming = data.Bool("Dreaming", true);
            transition = data.Enum("TransitionType", TransitionType.None);
            flashColor = ColorFix(data.Attr("Color", "White"));
            flags = data.Attr("ZFlagsData", "").Split(',');
            addTriggerOffset = data.Bool("AddTriggerOffset", true);
            timeSlowDown = data.Float("TimeSlowDown", 0f);
            delay = data.Float("TimeBeforeTeleport", 0f);
            legacyCamera = data.Int("CameraType", -1);
            resetDashes = false;
            onExit = data.Bool("OnExit", false);
            differentSide = data.Bool("DifferentSide", false);
            if (legacyCamera == -1) legacyCamera = 1;
        }

        public static Color ColorFix(string s, float alpha = 1f)
        {
            PropertyInfo[] prop = colorProperties;
            if (s == "Transparent")
                return Color.Transparent;
            foreach (PropertyInfo p in prop)
            {
                if (s == p.Name)
                {
                    return ColorCopy((Color)p.GetValue(default(Color)), alpha);
                }
            }

            return ColorCopy(Celeste.Graphics.Display.Colors.SourceHexColor.HexToColor(s), alpha);
        }

        public static Color ColorCopy(Color color, float alpha)
        {
            return new Color(color.R, color.G, color.B, (byte)alpha * 255);
        }
    }
}
