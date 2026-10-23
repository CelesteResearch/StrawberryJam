using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Input.Gameplay;
using Celeste.Scene.Scheduling.ExecutionTags;
using Celeste.Simulation;
using Celeste.Mods.StrawberryJam.Presentation.Boosters;

namespace Celeste.Mods.StrawberryJam.Entities.Boosters
{
    public partial class WormholeBooster
    {
        public IEnumerator TeleportCoroutine(RoomPlayerSession player)
        {
            var nearest = FindNearestBooster();
            if (nearest == null) yield break;
            Process.CanTeleport = false;
            var level = (OriginalLevelSceneRoot)Scene;
            level.SourceScene.QueueSourceEntity(new WormholeBoosterTrail(Position, nearest.Position, Process));
            ((IOriginalGameplayProcess)Engine).AudioSession.Play("event:/char/badeline/disappear", nearest.Position,
                ((IOriginalGameplayProcess)Engine).SaveContext);
            BoosterSprite.Visible = false;
            Collidable = false;
            Vector2 point = nearest.Center - player.Collider.Center;
            Vector2 target = new((float)Math.Floor(point.X), (float)Math.Floor(point.Y));
            player.CameraAnchorLerp = Vector2.Zero;
            Vector2 cameraTo = level.GetFullCameraTargetAt(player, target);
            player.Position = target;
            player.Boost(nearest);
            if (instantCamera) level.Camera.Position = cameraTo;
            else
            {
                Tags |= ((ISourceGlobalFrozenExecutionTagsEnvironment)Engine).GlobalFrozenExecutionTags.FrozenUpdate
                    .Value;
                level.Frozen = true;
                float distance =
                    ((ISourceVectorEnvironment)Engine).VectorArithmetic.Distance(level.Camera.Position, cameraTo);
                float catchup = ScalarMath.Clamp(distance / 5f, 8f, 600f);
                while (!level.InsideCamera(target, player.Height * 2))
                {
                    Vector2 current = level.Camera.Position;
                    level.Camera.Position += (cameraTo - current) *
                                             (1f - (float)Math.Pow((double)(.01f / catchup), (double)Engine.DeltaTime));
                    yield return null;
                }

                level.Frozen = false;
            }

            RemoveSelf();
        }

        public IEnumerator KillCoroutine(RoomPlayerSession player)
        {
            player.Machine.State = 11;
            player.Dummy.Gravity = false;
            player.Collidable = false;
            player.Data.Motion.Normal.VariableJumpTimer = 0f;
            player.Data.Motion.Normal.Speed = Vector2.Zero;
            Collidable = false;
            Vector2 target = Center - player.Collider.Center;
            for (float p = 0; p < 1f; p += Engine.DeltaTime / .75f)
            {
                BoosterSprite.Scale = Vector2Interpolation.Lerp(Vector2.One, Vector2.Zero,
                    ((ISourceEasingEnvironment)Engine).EasingArithmetic.CubeIn(p),
                    ((ISourceScalarEnvironment)Engine).ScalarArithmetic);
                if (player.Position != target)
                    player.Position =
                        ((ISourceVectorEnvironment)Engine).VectorArithmetic.Approach(player.Position, target,
                            80f * Engine.DeltaTime);
                else player.Visible = false;
                if (((ISourceGameInputEnvironment)Engine).Input.DashPressed)
                {
                    BoosterSprite.Scale = Vector2.Zero;
                    break;
                }

                yield return null;
            }

            ((IOriginalGameplayProcess)Engine).AudioSession.Play("event:/char/badeline/disappear", player.Position,
                ((IOriginalGameplayProcess)Engine).SaveContext);
            player.Die(Vector2.Zero);
            player.Machine.State = 0;
            player.Visible = true;
            player.Collidable = true;
            RemoveSelf();
        }
    }
}
