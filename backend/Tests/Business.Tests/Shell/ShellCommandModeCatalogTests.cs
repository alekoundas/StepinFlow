using Business.Shell;
using Core.Enums;

namespace Business.Tests.Shell
{
    public sealed class ShellCommandModeCatalogTests
    {
        // Launch had none, and the executor threw the first time one executed.
        [Fact]
        public void Every_mode_has_an_entry()
        {
            Enum.GetValues<ShellCommandModeEnum>()
                .Where(x => ShellCommandModeCatalog.All.All(y => y.Mode != x))
                .ShouldBeEmpty();
        }
    }
}
