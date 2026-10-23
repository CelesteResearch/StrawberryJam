using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Presentation.Animation;
using Celeste.Simulation.Gameplay.Player.Presentation.Hair;
using Celeste.Simulation.Gameplay.Player.Presentation.Sprites;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Skateboards
{
    public sealed class SkateboardHooks : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, SkateboardHooks> owners = new();
        private static readonly Vector2 PlayerSpriteOffset = new(0, -3);
        private readonly ISourceEngineContext engine;
        private readonly Func<bool> enabled;
        private readonly Action<bool> setEnabled;
        private readonly Hook playerRender, hairRender;
        private readonly ILHook animation;
        private AtlasEntry skateboardSprite;
        public bool Enabled => enabled();
        public void SetEnabled(bool value) => setEnabled(value);

        public static SkateboardHooks For(ISourceEngineContext engine) =>
            owners.TryGetValue(engine, out var hooks) ? hooks : null;

        public SkateboardHooks(ISourceEngineContext engine, Func<bool> enabled, Action<bool> setEnabled)
        {
            this.engine = engine;
            this.enabled = enabled;
            this.setEnabled = setEnabled;
            playerRender = new Hook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.Render)),
                (Action<Action<RoomPlayerSession, ISourceRenderContext>, RoomPlayerSession, ISourceRenderContext>)
                RenderPlayer);
            hairRender = new Hook(typeof(PlayerHair).GetMethod(nameof(PlayerHair.Render)),
                (Action<Action<PlayerHair, ISourceRenderContext>, PlayerHair, ISourceRenderContext>)RenderHair);
            animation = new ILHook(typeof(PlayerAnimation).GetMethod(nameof(PlayerAnimation.Update)), PatchAnimation);
            owners.Add(engine, this);
        }

        public void InitializeTextures() => skateboardSprite =
            ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas[
                "objects/StrawberryJam2021/skateboard/skateboard"];

        private void PatchAnimation(ILContext il)
        {
            var c = new ILCursor(il);
            int count = 0;
            while (c.TryGotoNext(MoveType.After,
                       i => (i.MatchLdstr("runSlow") || i.MatchLdstr("runFast") || i.MatchLdstr("runSlow_carry")) &&
                            IsPlayArgument(i)))
            {
                c.Emit(OpCodes.Ldarg_2);
                c.EmitDelegate<Func<string, PlayerSprite, string>>(AnimationName);
                count++;
            }

            if (count != 3) throw new InvalidOperationException("Player animation run Play arguments are not present.");
        }

        private static bool IsPlayArgument(Instruction i)
        {
            for (var n = i.Next; n != null; n = n.Next)
            {
                if (n.OpCode == OpCodes.Br || n.OpCode == OpCodes.Br_S)
                    n = n.Operand is ILLabel label ? label.Target : (Instruction)n.Operand;
                if (n.Operand is MethodReference m) return m.Name == "Play";
                if (n.OpCode != OpCodes.Ldc_I4_0) return false;
            }

            return false;
        }

        private string AnimationName(string original, PlayerSprite sprite) =>
            sprite.Entity is RoomPlayerSession p && ReferenceEquals(p.EngineContext, engine) && Enabled
                ? original == "runSlow_carry" ? "idle_carry" : "idle"
                : original;

        private void RenderPlayer(Action<RoomPlayerSession, ISourceRenderContext> original, RoomPlayerSession player,
            ISourceRenderContext context)
        {
            if (!ReferenceEquals(player.EngineContext, engine))
            {
                original(player, context);
                return;
            }

            if (Enabled) player.Sprite.RenderPosition += PlayerSpriteOffset;
            original(player, context);
            if (Enabled)
            {
                player.Sprite.RenderPosition -= PlayerSpriteOffset;
                var draw = new AtlasTextureDraw(skateboardSprite,
                    player.Sprite.RenderPosition + new Vector2(player.Facing == -1 ? 8 : -8, -4), Vector2.Zero,
                    PackedColor.White, new Vector2(player.Facing != -1 ? 1 : -1, 1));
                ((ISourceRenderCommandSink<AtlasTextureDraw>)context).Draw(in draw, player);
            }
        }

        private void RenderHair(Action<PlayerHair, ISourceRenderContext> original, PlayerHair hair,
            ISourceRenderContext context)
        {
            if (!ReferenceEquals(hair.EngineContext, engine))
            {
                original(hair, context);
                return;
            }

            if (Enabled)
                for (int i = 0; i < hair.Nodes.Count; i++)
                    hair.Nodes[i] += PlayerSpriteOffset;
            original(hair, context);
            if (Enabled)
                for (int i = 0; i < hair.Nodes.Count; i++)
                    hair.Nodes[i] -= PlayerSpriteOffset;
        }

        public void Dispose()
        {
            owners.Remove(engine);
            animation.Dispose();
            hairRender.Dispose();
            playerRender.Dispose();
        }
    }
}
