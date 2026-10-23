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
        public override void Added(object scene)
        {
            base.Added(scene);
            level = (Level)Scene;
            currentRoom = level.Session.Level;
            List<LevelData> Levels = ((Celeste.Game.Content.Maps.OriginalChapterMapData)level.Session.MapData).Levels;
            bool b = false;
            foreach (LevelData l in Levels)
            {
                b |= l.Name == newRoom;
            }

            if (!b)
            {
                newRoom = currentRoom;
                newPos = new Vector2(-1, -1);
                vel = new float[] { velMod ? 1 : 0, velMod ? 1 : 0 };
                vel2 = -1;
                dreaming = true;
                cameraOffset = new float[] { 0, 0 };
                transition = TransitionType.None;
                flashColor = Color.White;
                rotMod = false;
                rot = 0f;
            }
        }

        public override void Update()
        {
            base.Update();
            if (doFlash)
            {
                flash = Approach(flash, 1f, engine.DeltaTime * 10f);
                if (flash >= 1f)
                {
                    doFlash = false;
                }
            }
            else if (flash > 0f)
            {
                flash = Approach(flash, 0f, engine.DeltaTime * 3f);
            }
        }

        private void Transition(TransitionType transition, RoomPlayerSession player)
        {
            if (transition == TransitionType.Lightning)
            {
                level.Flash(Color.White);
                level.Shake();
                level.Scheduler.QueueSourceEntity(new LightningStrikeState(new Vector2(player.X + 60f, player.Y - 180),
                    10, 200f, engine));
                level.Scheduler.QueueSourceEntity(new LightningStrikeState(new Vector2(player.X + 220f, player.Y - 180),
                    40, 200f, engine, 0.25f));
                ((Celeste.Game.Execution.Processes.IOriginalGameplayProcess)engine).AudioSession.Play(
                    "event:/new_content/game/10_farewell/lightning_strike");
            }
        }


        public override void OnEnter(RoomPlayerSession player)
        {
            if (onExit)
            {
                // Calculate entrance direction
                // [2 horizontal directions], [2 vertical directions]
                int[] sides = new int[4];
                // Set priority
                sides[0] = player.Data.Motion.Normal.Speed.X > 0 ? 1 : -1; //Left
                sides[1] = -sides[0]; //Right
                sides[2] = player.Data.Motion.Normal.Speed.Y > 0 ? 1 : -1; //Top
                sides[3] = -sides[2]; //Bottom

                Vector2 prevTopRight =
                    player.TopRight - player.Data.Motion.Normal.Speed; //TopLeft, TopRight, BottomLeft, BottomRight
                Vector2 prevBottomLeft = player.BottomLeft - player.Data.Motion.Normal.Speed;
                // Check if the segment prevLoc -> curLoc crosses any of the boundaries
                sides[0] *= (Left > prevTopRight.X && Left <= player.TopRight.X) ? 1 : 0;
                sides[1] *= (Right < prevBottomLeft.X && Right >= player.BottomLeft.X) ? 1 : 0;
                sides[2] *= (Top > prevBottomLeft.Y && Top <= player.BottomLeft.Y) ? 1 : 0;
                sides[3] *= (Bottom < prevTopRight.Y && Bottom >= player.TopRight.Y) ? 1 : 0;
                int maxValue = sides.Max();
                direction = Array.IndexOf(sides, maxValue);
            }
            else
            {
                if (delay > 0) Add(new SimulationCoroutine(engine, OnEnterSequence(player)));
                else if (!triggered && FlagsMatch() && player != null && !player.Dead) Teleport(player);
            }
        }

        public override void OnLeave(RoomPlayerSession player)
        {
            if (player?.Dead ?? true)
                return;
            if (onExit)
            {
                bool trigger = true;
                if (differentSide)
                {
                    // Calculate exit direction
                    // [2 horizontal directions], [2 vertical directions]
                    int[] sides = new int[4];
                    // Set priority
                    sides[0] = player.Data.Motion.Normal.Speed.X > 0 ? -1 : 1; //Left
                    sides[1] = -sides[0]; //Right
                    sides[2] = player.Data.Motion.Normal.Speed.Y > 0 ? -1 : 1; //Top
                    sides[3] = -sides[2]; //Bottom

                    Vector2 prevTopRight =
                        player.TopRight - player.Data.Motion.Normal.Speed; //TopLeft, TopRight, BottomLeft, BottomRight
                    Vector2 prevBottomLeft = player.BottomLeft - player.Data.Motion.Normal.Speed;
                    // Check if the segment prevLoc -> curLoc crosses any of the boundaries
                    sides[0] *= (Left < prevTopRight.X && Left >= player.TopRight.X) ? 1 : 0;
                    sides[1] *= (Right > prevBottomLeft.X && Right <= player.BottomLeft.X) ? 1 : 0;
                    sides[2] *= (Top < prevBottomLeft.Y && Top >= player.BottomLeft.Y) ? 1 : 0;
                    sides[3] *= (Bottom > prevTopRight.Y && Bottom <= player.TopRight.Y) ? 1 : 0;
                    int maxValue = sides.Max();
                    int exitDirection = Array.IndexOf(sides, maxValue);
                    trigger = exitDirection != direction;
                }

                if (trigger)
                {
                    if (delay > 0) Add(new SimulationCoroutine(engine, OnEnterSequence(player)));
                    else if (!triggered && FlagsMatch() && player != null && !player.Dead) Teleport(player);
                }
            }

            direction = -1;
        }

        public IEnumerator OnEnterSequence(RoomPlayerSession player)
        {
            float timer = delay;
            while (timer > 0)
            {
                if (player?.Dead ?? true)
                {
                    yield break;
                }

                timer -= engine.DeltaTime;
                yield return null;
            }

            if (!triggered && FlagsMatch() && player != null && !player.Dead)
            {
                Teleport(player);
            }
        }

        public static bool CompareEntityIDs(MapEntityId a, MapEntityId b)
        {
            return a.Level == b.Level && a.ID == b.ID;
        }

        public static bool MatchDashState(int state)
        {
            return state == 2 || state == 5;
        }
    }
}
