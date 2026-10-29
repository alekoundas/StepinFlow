using System.Reflection;
using System.Runtime.InteropServices;
using Platform.Windows.Windowing;

namespace Platform.Windows.Tests.Native
{
    /// <summary>
    /// Every P/Invoke in Platform.Windows, bound to its library and never called, so it needs no
    /// desktop.
    /// </summary>
    public sealed class NativeImportTests
    {
        // A LibraryImport is a managed wrapper around a DllImport the generator writes, so the
        // generated ones are here as well as the hand-written ones.
        private static List<MethodInfo> NativeImports()
        {
            return typeof(WindowService).Assembly.GetTypes()
                .SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
                .Where(x => x.Attributes.HasFlag(MethodAttributes.PinvokeImpl))
                .ToList();
        }

        // LibraryImport spells the entry point exactly and DllImport does not: user32 has
        // PostMessageW and no PostMessage, so a conversion compiles and throws the first time a flow
        // closes a window. Prelink loads the library and finds the export.
        [Fact]
        public void Every_native_import_resolves_against_its_library()
        {
            List<MethodInfo> imports = NativeImports();
            List<string> unresolved = new List<string>();

            foreach (MethodInfo import in imports)
            {
                try
                {
                    Marshal.Prelink(import);
                }
                catch (Exception ex) when (ex is EntryPointNotFoundException or DllNotFoundException)
                {
                    unresolved.Add($"{import.DeclaringType?.Name}.{import.Name}: {ex.Message}");
                }
            }

            imports.ShouldContain(x => x.Name.Contains("PostMessage"));
            unresolved.ShouldBeEmpty();
        }
    }
}
