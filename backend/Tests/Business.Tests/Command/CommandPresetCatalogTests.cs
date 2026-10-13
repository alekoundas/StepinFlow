using Business.Command;
using Core.Enums;

namespace Business.Tests.Command
{
    public sealed class CommandPresetCatalogTests
    {
        // Launch had none, and the runner threw the first time one executed.
        [Fact]
        public void Every_preset_has_an_entry()
        {
            Enum.GetValues<RunCommandPresetEnum>()
                .Where(x => CommandPresetCatalog.All.All(y => y.Preset != x))
                .ShouldBeEmpty();
        }
    }
}
