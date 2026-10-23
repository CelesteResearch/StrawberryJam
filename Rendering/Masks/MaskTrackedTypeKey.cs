namespace Celeste.Mods.StrawberryJam.Rendering.Masks
{
    internal static class MaskTrackedTypeKey<T>
    {
        internal static readonly string QualifiedName;

        static MaskTrackedTypeKey()
        {
            QualifiedName = typeof(T).AssemblyQualifiedName;
        }
    }
}
