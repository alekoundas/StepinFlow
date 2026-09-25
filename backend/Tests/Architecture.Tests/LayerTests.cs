using System.Reflection;

using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

using SolutionArchitecture = ArchUnitNET.Domain.Architecture;

namespace Architecture.Tests
{
    /// <summary>
    /// The project arrows in PROJECT.md section 2, as tests. A project reference already stops most
    /// of this from compiling; these catch the one-line csproj change that adds the reference.
    /// </summary>
    public sealed class LayerTests
    {
        private static readonly Assembly Core = Assembly.Load("Core");
        private static readonly Assembly DataAccess = Assembly.Load("DataAccess");
        private static readonly Assembly Business = Assembly.Load("Business");
        private static readonly Assembly Transport = Assembly.Load("Transport");
        private static readonly Assembly PlatformWindows = Assembly.Load("Platform.Windows");
        private static readonly Assembly App = Assembly.Load("App");

        private static readonly SolutionArchitecture Solution = new ArchLoader()
            .LoadAssemblies(Core, DataAccess, Business, Transport, PlatformWindows, App)
            .Build();

        [Fact]
        public void Core_depends_on_no_other_project()
        {
            Types().That().ResideInAssembly(Core)
                .Should().NotDependOnAny(Types().That().ResideInAssembly(DataAccess, Business, Transport, PlatformWindows, App))
                .Check(Solution);
        }

        [Fact]
        public void DataAccess_depends_on_Core_alone()
        {
            Types().That().ResideInAssembly(DataAccess)
                .Should().NotDependOnAny(Types().That().ResideInAssembly(Business, Transport, PlatformWindows, App))
                .Check(Solution);
        }

        [Fact]
        public void Business_never_touches_the_machine_or_the_transport()
        {
            Types().That().ResideInAssembly(Business)
                .Should().NotDependOnAny(Types().That().ResideInAssembly(Transport, PlatformWindows, App))
                .Check(Solution);
        }

        [Fact]
        public void Transport_never_touches_the_machine()
        {
            Types().That().ResideInAssembly(Transport)
                .Should().NotDependOnAny(Types().That().ResideInAssembly(PlatformWindows, App))
                .Check(Solution);
        }

        [Fact]
        public void Platform_Windows_depends_on_Core_alone()
        {
            Types().That().ResideInAssembly(PlatformWindows)
                .Should().NotDependOnAny(Types().That().ResideInAssembly(DataAccess, Business, Transport, App))
                .Check(Solution);
        }

        [Theory]
        [InlineData("OpenCvSharp")]
        [InlineData("SharpHook")]
        public void Only_Platform_Windows_uses_a_native_library(string library)
        {
            List<string> users = new[] { Core, DataAccess, Business, Transport, PlatformWindows, App }
                .Where(x => x.GetReferencedAssemblies().Any(r => r.Name != null && r.Name.StartsWith(library, StringComparison.Ordinal)))
                .Select(x => x.GetName().Name ?? string.Empty)
                .ToList();

            // Platform.Windows is in the list on purpose: a misspelt library would otherwise pass.
            users.ShouldBe(["Platform.Windows"]);
        }

        [Fact]
        public void Core_uses_nothing_but_the_framework()
        {
            string framework = Path.GetDirectoryName(typeof(object).Assembly.Location) ?? string.Empty;

            List<string> outside = Core.GetReferencedAssemblies()
                .Where(x => !Path.GetDirectoryName(Assembly.Load(x).Location)!.Equals(framework, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Name ?? string.Empty)
                .ToList();

            outside.ShouldBeEmpty();
        }

        // Phase 5.6 took the level out: it claimed everything below it was a service.
        [Fact]
        public void Business_has_no_Services_namespace()
        {
            List<string> namespaces = Business.GetTypes()
                .Select(x => x.Namespace ?? string.Empty)
                .Where(x => x.Split('.').Contains("Services"))
                .Distinct()
                .ToList();

            namespaces.ShouldBeEmpty();
        }
    }
}
