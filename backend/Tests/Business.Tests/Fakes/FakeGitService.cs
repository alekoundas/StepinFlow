using Core.Enums;
using Core.Models.Business.Git;
using Core.Ports;

namespace Business.Tests.Fakes
{
    /// <summary>
    /// A repository that is only a folder. What changed is every file in it, unless a test says
    /// otherwise, and a commit is remembered rather than made.
    /// </summary>
    public sealed class FakeGitService : IGitService
    {
        public bool MakesRepository { get; set; } = true;

        /// <summary>What Changes answers. Null is every file in the folder, as new.</summary>
        public List<GitFileChange>? PendingChanges { get; set; }

        /// <summary>The last commit's files, by path.</summary>
        public Dictionary<string, byte[]> Committed { get; } = new Dictionary<string, byte[]>();

        public GitSignature? Configured { get; set; }

        public List<(IReadOnlyList<string> Paths, string Message, GitSignature Author)> Commits { get; } = [];

        public bool EnsureRepositoryFolderExists(string folderPath)
        {
            Directory.CreateDirectory(folderPath);

            return MakesRepository;
        }

        public string CurrentBranch(string folderPath)
        {
            return "main";
        }

        public int CommitCount(string folderPath)
        {
            return Commits.Count;
        }

        public List<GitFileChange> Changes(string folderPath)
        {
            if (PendingChanges != null)
                return PendingChanges;

            return Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories)
                .Select(x => Path.GetRelativePath(folderPath, x).Replace('\\', '/'))
                .Select(x => new GitFileChange { Path = x, Kind = GitChangeKindEnum.ADDED })
                .ToList();
        }

        public byte[]? ReadFile(string folderPath, string? commitSha, string path)
        {
            return Committed.GetValueOrDefault(path);
        }

        public GitCommit Commit(string folderPath, IReadOnlyList<string> paths, string message, GitSignature author)
        {
            Commits.Add((paths, message, author));

            return new GitCommit { Sha = new string('a', 40), Message = message, AuthorName = author.Name, AuthorEmail = author.Email, When = author.When };
        }

        public List<GitCommit> Log(string folderPath, IReadOnlyList<string> paths, int max)
        {
            return [];
        }

        public GitCommit? FindCommit(string folderPath, string commitSha)
        {
            return null;
        }

        public List<GitFileChange> CommitChanges(string folderPath, string commitSha, IReadOnlyList<string> paths)
        {
            return [];
        }

        public GitSignature? ConfiguredAuthor(string folderPath)
        {
            return Configured;
        }
    }
}
