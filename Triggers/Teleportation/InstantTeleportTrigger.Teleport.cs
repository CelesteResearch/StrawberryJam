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
        private void Teleport(RoomPlayerSession player)
        {
            triggered = true;
            float temp4 = transition == TransitionType.GlitchEffect ? 0.5f : 0f;
            SourceTweenState tween = SourceTweenState.Create(engine.TweenPool, SourceTweenState.TweenMode.Oneshot, null,
                engine.DeltaTime, start: true);
            tween.OnComplete = delegate
            {
                level.Scheduler.OnEndOfFrame += delegate
                {
                    if (player?.Dead ?? true)
                        return;
                    SourceTweenState tween1 = SourceTweenState.Create(engine.TweenPool,
                        SourceTweenState.TweenMode.Oneshot, null, 0.3f, start: true);
                    tween1.OnUpdate = delegate(SourceTweenState t) { engine.Effects.Glitch = temp4 * (t.Eased); };
                    player.Add(tween1);
                    //Instantiates all the variables of the previous player object into something we can use later.
                    int facing = player.Data.Facing;
                    Vector2 levelOffset = level.LevelOffset;
                    Vector2[] vals = new Vector2[5];
                    vals[0] = player.Position - levelOffset;
                    vals[1] = level.Camera.Position - levelOffset;
                    vals[2] = player.Position.X < Position.X
                        ? player.Position - Position + new Vector2(8, 0)
                        : player.Position - Position;
                    vals[3] = level.CameraOffset;
                    Tuple<Vector2, Vector2, bool, bool> c = new Tuple<Vector2, Vector2, bool, bool>(player.CameraAnchor,
                        player.CameraAnchorLerp, player.CameraAnchorIgnoreX, player.CameraAnchorIgnoreY);

                    LeaderState leader = player.Leader;
                    foreach (FollowerState follower in leader.Followers)
                    {
                        if (follower.Entity != null)
                        {
                            ((SceneEntityState)follower.Entity).Position -= player.Position;
                            ((SceneEntityState)follower.Entity).Tags |= GlobalTag;
                            if (!CompareEntityIDs(follower.ParentEntityID,
                                    Celeste.Formats.Map.Identifiers.EntityIdCodec.None))
                                level.Session.DoNotLoad.Add(follower.ParentEntityID);
                        }
                    }

                    for (int i = 0; i < leader.PastPoints.Count; i++)
                    {
                        leader.PastPoints[i] -= player.Position;
                    }

                    int state = player.Machine.State;
                    player.CleanUpPlayerTriggers();
                    player.CameraAnchorIgnoreX = true;
                    player.CameraAnchorIgnoreY = true;
                    int pDashes =
                        0; //This should be player.Data.Dashes if resetDashes is true, if it's false it's unused.
                    Vector2 pDashDir = Vector2.Zero;
                    if (!resetDashes)
                    {
                        pDashes = player.Data.Dashes;
                    }

                    if (MatchDashState(player.Machine.State))
                    {
                        pDashDir = player.Data.Direction;
                    }

                    // For use in GMHS, we don't want to reload the room if teleporting to the same room to avoid loading lag when loading entities
                    if (newRoom != currentRoom)
                    {
                        //Removes the current level we are in without breaking everything
                        level.RemoveEntity(player);
                        level.UnloadLevel();
                        foreach (var bird in level.Scheduler.Entities
                                     .FindAll<Celeste.Game.Entities.Birds.Fling.OriginalFlingBirdState>())
                            level.RemoveEntity(
                                bird); //Apparently Awake is called in a FlingBird to save it to the EntityList and we don't want that.

                        //Sets the future values of the player. This code is modified from the Teleport Trigger to accommodate more things.
                        level.Session.Level = newRoom;
                        level.Session.Dreaming = dreaming;
                        level.Session.FirstLevel = false;

                        level.Scheduler.QueueSourceEntity(player);
                        level.LoadLevel(PlayerIntroType.Transition);
                    }
                    else
                    {
                        // triggered is set to false on load, because we're skipping reloading entities, we need to manually reset triggered
                        triggered = false;
                        level.Session.Dreaming = dreaming;
                    }

                    if (!resetDashes)
                    {
                        player.Data.Dashes = pDashes;
                    }

                    if (newPos.X >= 0 && newPos.X <= level.Bounds.X + level.Bounds.Width - level.LevelOffset.X &&
                        newPos.Y >= 0 && newPos.Y <= level.Bounds.Y + level.Bounds.Height - level.LevelOffset.Y)
                    {
                        player.Position = level.LevelOffset + newPos;
                        if (addTriggerOffset)
                        {
                            player.Position += vals[2];
                        }

                        ModifySpeed(player);
                        if (legacyCamera == 2) NewCamera(level, player, vals, c);
                        else ApplyLegacyCamera(level, player, level.LevelOffset + vals[1]);
                    }
                    else if (vals[0].X >= 0 && vals[0].X <= level.Bounds.X + level.Bounds.Width - level.LevelOffset.X &&
                             vals[0].Y >= 0 && vals[0].Y <= level.Bounds.Y + level.Bounds.Height - level.LevelOffset.Y)
                    {
                        player.Position = level.LevelOffset + vals[0];
                        ModifySpeed(player);
                        if (legacyCamera == 2) NewCamera(level, player, vals, c);
                        else ApplyLegacyCamera(level, player, level.LevelOffset + vals[1]);
                    }
                    else
                    {
                        player.Position = new Vector2(1, 1);
                        ModifySpeed(player);
                        if (legacyCamera > 2) NewCamera(level, player, vals, c);
                        else ApplyLegacyCamera(level, player, level.LevelOffset + vals[1]);
                    }

                    try
                    {
                        level.Session.RespawnPoint = level.GetSpawnPoint(player.Position);
                    }
                    catch
                    {
                        level.Session.RespawnPoint = player.Position;
                    }

                    if (MatchDashState(player.Machine.State))
                    {
                        player.Data.Direction = VectorArithmetic.RotateTowards(pDashDir,
                            Vector2Geometry.Angle(player.Data.Motion.Normal.Speed), 6.3f);
                    }

                    if (state == 10)
                    {
                        player.SummitLaunch(player.Position.X);
                    }

                    foreach (FollowerState follower in leader.Followers)
                    {
                        if (follower.Entity != null)
                        {
                            ((SceneEntityState)follower.Entity).Position += player.Position;
                            ((SceneEntityState)follower.Entity).Tags &= ~GlobalTag;
                            if (follower.Entity is not Celeste.Game.Gameplay.Collectibles.Keys.OriginalKeyState)
                                level.Session.DoNotLoad.Remove(follower.ParentEntityID);
                        }
                    }

                    for (int i = 0; i < leader.PastPoints.Count; i++)
                    {
                        leader.PastPoints[i] += player.Position;
                    }

                    leader.TransferFollowers();

                    player.Data.Facing = facing;
                    player.Hair.MoveHairBy(level.LevelOffset - levelOffset);


                    SourceTweenState tween2 = SourceTweenState.Create(engine.TweenPool,
                        SourceTweenState.TweenMode.Oneshot, null, 0.3f, start: true);
                    tween2.OnUpdate = delegate(SourceTweenState t) { engine.Effects.Glitch = temp4 * (1f - t.Eased); };
                    player.Add(tween2);
                    if (transition == TransitionType.ColorFlash)
                    {
                        level.Flash(flashColor, false);
                    }

                    Transition(transition, player);
                    if (timeSlowDown != 0f)
                    {
                        Add(new SimulationCoroutine(engine, TimeSlowDown(timeSlowDown, .1f)));
                    }
                };
            };
            player.Add(tween);
        }

        public IEnumerator TimeSlowDown(float duration = .25f, float speedInit = 0.5f)
        {
            for (float i = 0; i < duration; i += .05f)
            {
                engine.Effects.TimeRate = speedInit + (1 - speedInit) * i;
                yield return .05f;
            }

            engine.Effects.TimeRate = 1f;
        }
    }
}
