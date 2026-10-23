using Celeste.Core.Components;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Mods.StrawberryJam.Entities.Bubbles;

namespace Celeste.Mods.StrawberryJam.Components.Bubbles
{
    [SceneTracked]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.BubbleCollider")]
    public class BubbleCollider : SourceComponentState
    {
        private readonly ColliderGeometry collider;

        public BubbleCollider(ColliderGeometry collider = null) : base(true, false)
        {
            this.collider = collider;
        }

        public bool Check(FloatingBubble bubble)
        {
            var entity = (SceneEntityState)Entity;
            var original = entity.Collider;
            if (collider != null) entity.Collider = collider;
            bool result = SourceEntityCollision.Check(bubble, entity);
            entity.Collider = original;
            return result;
        }
    }
}
