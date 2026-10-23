using Celeste.Mods.StrawberryJam.Runtime.Fields.Dashes;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private DashBoostFieldRuntime dashBoostFields;
        private void LoadDashBoostFields() => dashBoostFields = new DashBoostFieldRuntime(SourceEngine);
        private void LoadDashBoostFieldParticles() => dashBoostFields.LoadParticles();

        private void UnloadDashBoostFields()
        {
            dashBoostFields?.Dispose();
            dashBoostFields = null;
        }
    }
}
