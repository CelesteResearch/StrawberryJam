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
    public partial class InstantTeleportTrigger : SourceTriggerEntityState
    {
        private void ModifySpeed(RoomPlayerSession player)
        {
            if (vel2 == -1f && !(vel[0] == 0f && vel[1] == 0f))
            {
                player.Data.Motion.Normal.Speed = velMod
                    ? new Vector2(player.Data.Motion.Normal.Speed.X * vel[0],
                        player.Data.Motion.Normal.Speed.Y * vel[1])
                    : player.Data.Motion.Normal.Speed + new Vector2(vel[0], vel[1]);
            }

            if (vel2 != -1f && vel[0] == 0f && vel[1] == 0f)
            {
                player.Data.Motion.Normal.Speed = velMod
                    ? player.Data.Motion.Normal.Speed * vel2
                    : player.Data.Motion.Normal.Speed +
                      new Vector2(vel2 * (float)Math.Cos(Vector2Geometry.Angle(player.Data.Motion.Normal.Speed)),
                          vel2 * (float)Math.Sin(Vector2Geometry.Angle(player.Data.Motion.Normal.Speed)));
            }

            player.Data.Motion.Normal.Speed = rotMod
                ? VectorArithmetic.RotateTowards(player.Data.Motion.Normal.Speed, rot, 6.3f)
                : VectorArithmetic.Rotate(player.Data.Motion.Normal.Speed, rot);
        }

        private void ApplyLegacyCamera(Level level, RoomPlayerSession player, Vector2 v)
        {
            if (legacyCamera == 0)
            {
                level.Camera.Position = v;
            }
            else
            {
                Vector2 camPos = player.Position;
                Vector2 vector2 = new Vector2(player.X - 160f, player.Y - 90f);
                camPos.X = Clamp(vector2.X, (float)level.Bounds.Left, (float)(level.Bounds.Right - 320));
                camPos.Y = Clamp(vector2.Y, (float)level.Bounds.Top, (float)(level.Bounds.Bottom - 180));
                level.Camera.Position = camPos;
            }
        }

        private void NewCamera(Level level, RoomPlayerSession player, Vector2[] vals,
            Tuple<Vector2, Vector2, bool, bool> c)
        {
            level.CameraOffset = vals[3];
            Vector2 camPos = player.Position;
            Vector2 vector2 = new Vector2(player.X - 160f, player.Y - 90f) + vals[3];
            camPos.X = Clamp(vector2.X, (float)level.Bounds.Left, (float)(level.Bounds.Right - 320));
            camPos.Y = Clamp(vector2.Y, (float)level.Bounds.Top, (float)(level.Bounds.Bottom - 180));
            level.Camera.Position = camPos;


            switch (legacyCamera)
            {
                case 3:
                    player.CameraAnchor = c.Item1;
                    player.CameraAnchorLerp = Vector2.Zero;
                    player.CameraAnchorIgnoreX = c.Item3;
                    player.CameraAnchorIgnoreY = c.Item4;
                    break;
                default:
                    player.CameraAnchor = c.Item1;
                    player.CameraAnchorLerp = c.Item2;
                    player.CameraAnchorIgnoreX = c.Item3;
                    player.CameraAnchorIgnoreY = c.Item4;
                    break;
            }
        }
    }
}
