using Core.Enums;
using Core.Models.Business.Git;
using Core.Ports;

namespace Business.Tests.Fakes
{
    /// <summary>
    /// A repository that is only a folder. What changed is every file in it, unless a test says
    /// otherwise, and a commit is remembered rather than made. Each branch is the files of its last
    /// commit, its name standing for that commit, and checking one out writes them to the folder.
    /// </summary>
    public sealed class FakeGitService : IGitService
    {
        private const string MAIN = "main";

        private readonly Dictionary<string, Dictionary<string, byte[]>> _branches = new Dictionary<string, Dictionary<string, byte[]>>
        {
            [MAIN] = new Dictionary<string, byte[]>(),
        };

        public bool MakesRepository { get; set; } = true;

        /// <summary>What Changes answers. Null is every file in the folder, as new.</summary>
        public List<GitFileChange>? PendingChanges { get; set; }

        public string Current { get; set; } = MAIN;

        /// <summary>The last commit's files on the branch checked out, by path.</summary>
        public Dictionary<string, byte[]> Committed
        {
            get { return _branches[Current]; }
        }

        public GitSignature? Configured { get; set; }

        public List<(IReadOnlyList<string> Paths, string Message, GitSignature Author)> Commits { get; } = [];

        /// <summary>Every branch checked out, in order.</summary>
        public List<string> Switches { get; } = [];

        /// <summary>A branch's files, the branch made when there is none.</summary>
        public Dictionary<string, byte[]> BranchFiles(string name)
        {
            if (!_branches.TryGetValue(name, out Dictionary<string, byte[]>? files))
            {
                files = new Dictionary<string, byte[]>();
                _branches[name] = files;
            }

            return files;
        }

        public bool EnsureRepositoryFolderExists(string folderPath)
        {
            Directory.CreateDirectory(folderPath);

            return MakesRepository;
        }

        public string CurrentBranch(string folderPath)
        {
            return Current;
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
            return FilesAt(commitSha).GetValueOrDefault(path);
        }

        public List<string> Files(string folderPath, string? commitSha)
        {
            return FilesAt(commitSha).Keys.ToList();
        }

        public GitCommit Commit(string folderPath, IReadOnlyList<string> paths, string message, GitSignature author)
        {
            Commits.Add((paths, message, author));

            return new GitCommit { Sha = new string('a', 40), Message = message, AuthorName = author.Name, AuthorEmail = author.Email, When = author.When };
        }

        // Every commit made, newest first, whatever the paths: none of them is ever about another flow here.
        public List<GitCommit> Log(string folderPath, IReadOnlyList<string> paths, int max)
        {
            return Commits
                .Select(x => new GitCommit { Sha = new string('a', 40), Message = x.Message, AuthorName = x.Author.Name, AuthorEmail = x.Author.Email, When = x.Author.When })
                .Reverse()
                .Take(max)
                .ToList();
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

        public List<GitBranch> Branches(string folderPath)
        {
            return _branches.Keys
                .Order(StringComparer.OrdinalIgnoreCase)
                .Select(x => new GitBranch { Name = x, Sha = x, IsCurrent = x == Current })
                .ToList();
        }

        public bool IsValidBranchName(string name)
        {
            return !name.Contains(' ', StringComparison.Ordinal);
        }

        public void CreateBranch(string folderPath, string name)
        {
            _branches[name] = new Dictionary<string, byte[]>(Committed);
        }

        // As a checkout would: what the branch lacks removed, the rest written.
        public void SwitchBranch(string folderPath, string name)
        {
            Dictionary<string, byte[]> files = _branches[name];

            foreach (string path in Committed.Keys.Except(files.Keys))
                File.Delete(Path.Combine(folderPath, path));

            foreach ((string path, byte[] bytes) in files)
            {
                string fullPath = Path.Combine(folderPath, path);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                File.WriteAllBytes(fullPath, bytes);
            }

            Current = name;
            Switches.Add(name);
        }

        public List<GitFileChange> Compare(string folderPath, string fromCommitSha, string toCommitSha)
        {
            Dictionary<string, byte[]> from = FilesAt(fromCommitSha);
            Dictionary<string, byte[]> to = FilesAt(toCommitSha);

            List<GitFileChange> changes = new List<GitFileChange>();
            foreach (string path in from.Keys.Union(to.Keys).Order(StringComparer.Ordinal))
            {
                from.TryGetValue(path, out byte[]? before);
                to.TryGetValue(path, out byte[]? after);

                if (after == null)
                    changes.Add(new GitFileChange { Path = path, Kind = GitChangeKindEnum.DELETED });
                else if (before == null)
                    changes.Add(new GitFileChange { Path = path, Kind = GitChangeKindEnum.ADDED });
                else if (!before.SequenceEqual(after))
                    changes.Add(new GitFileChange { Path = path, Kind = GitChangeKindEnum.MODIFIED });
            }

            return changes;
        }

        // A branch's name is its commit; any other commit is the last one on the branch checked out.
        private Dictionary<string, byte[]> FilesAt(string? commitSha)
        {
            if (commitSha != null && _branches.TryGetValue(commitSha, out Dictionary<string, byte[]>? files))
                return files;

            return Committed;
        }
    }
}
