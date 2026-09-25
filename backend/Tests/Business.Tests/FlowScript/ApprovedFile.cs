using System.Runtime.CompilerServices;

namespace Business.Tests.FlowScript
{
    /// <summary>
    /// Text compared with a file a person read once and approved, committed beside the test.
    ///
    /// On a difference the new text is written next to it as <c>.received</c> and the test fails,
    /// naming the first line that differs. Intended: replace the approved file with it. Not: fix the
    /// code. Line endings are not part of the comparison.
    /// </summary>
    public static class ApprovedFile
    {
        public static void ShouldMatch(string actual, string name, [CallerFilePath] string testFile = "")
        {
            string folder = Path.GetDirectoryName(testFile) ?? string.Empty;
            string approvedPath = Path.Combine(folder, $"{name}.approved.sflw");
            string receivedPath = Path.Combine(folder, $"{name}.received.sflw");

            string received = actual.ReplaceLineEndings("\n");

            if (!File.Exists(approvedPath))
            {
                File.WriteAllText(receivedPath, received);
                Assert.Fail($"There is no approved file yet. Read {receivedPath} and, if it is right, rename it to {Path.GetFileName(approvedPath)}.");
            }

            string approved = File.ReadAllText(approvedPath).ReplaceLineEndings("\n");
            if (approved == received)
            {
                File.Delete(receivedPath);
                return;
            }

            File.WriteAllText(receivedPath, received);
            Assert.Fail($"Differs from {Path.GetFileName(approvedPath)}. {FirstDifference(approved, received)} The new text is in {receivedPath}.");
        }

        private static string FirstDifference(string approved, string received)
        {
            string[] expected = approved.Split('\n');
            string[] actual = received.Split('\n');

            for (int i = 0; i < Math.Max(expected.Length, actual.Length); i++)
            {
                string was = i < expected.Length ? expected[i] : "(nothing)";
                string now = i < actual.Length ? actual[i] : "(nothing)";

                if (was != now)
                    return $"Line {i + 1} was \"{was}\" and is now \"{now}\".";
            }

            return string.Empty;
        }
    }
}
