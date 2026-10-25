using Core.Enums;
using Core.Models.Business.Git;
using Core.Ports;

namespace Business.Tests.Fakes
{
    /// <summary>
    /// A repository that is only a folder. What changed is every file in it, unless a test says
    /// otherwise, and a commit is remembered rather than made. A commit here is the files it holds;
    /// each branch, here and on the remote, points at one, and checking one out writes its files to
    /// the folder. Which commits one has that another does not is whatever a test says.
    /// </summary>
    public sealed class FakeGitService : IGitService
    {
        private const string MAIN = "main";

        private readonly Dictionary<string, Dictionary<string, byte[]>> _commits = new Dictionary<string, Dictionary<string, byte[]>>();
        private readonly Dictionary<string, string> _branches = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _remoteBranches = new Dictionary<string, string>();
        private int _nextCommit;

        public FakeGitService()
        {
            _branches[MAIN] = NewCommit([]);
        }

        public bool MakesRepository { get; set; } = true;

        /// <summary>What Changes answers. Null is every file in the folder, as new.</summary>
        public List<GitFileChange>? PendingChanges { get; set; }

        public string Current { get; set; } = MAIN;

        /// <summary>The last commit's files on the branch checked out, by path.</summary>
        public Dictionary<string, byte[]> Committed
        {
            get { return _commits[_branches[Current]]; }
        }

        public GitSignature? Configured { get; set; }

        public List<(IReadOnlyList<string> Paths, string Message, GitSignature Author)> Commits { get; } = [];

        /// <summary>Every branch checked out, in order.</summary>
        public List<string> Switches { get; } = [];

        public string? Remote { get; set; }

        /// <summary>Thrown by a fetch, as the network would.</summary>
        public string? FetchError { get; set; }

        public List<string> Tokens { get; } = [];

        public List<string> Pushed { get; } = [];

        /// <summary>The commits one commit has that another does not.</summary>
        public Func<string, string?, List<GitCommit>> Between { get; set; } = (_, _) => [];

        /// <summary>A branch's files, the branch made when there is none.</summary>
        public Dictionary<string, byte[]> BranchFiles(string name)
        {
            if (!_branches.TryGetValue(name, out string? sha))
            {
                sha = NewCommit([]);
                _branches[name] = sha;
            }

            return _commits[sha];
        }

        /// <summary>The same, for a branch on the remote.</summary>
        public Dictionary<string, byte[]> RemoteFiles(string name)
        {
            if (!_remoteBranches.TryGetValue(name, out string? sha))
            {
                sha = NewCommit([]);
                _remoteBranches[name] = sha;
            }

            return _commits[sha];
        }

        public string ShaOf(string branch)
        {
            return _branches[branch];
        }

        public string RemoteShaOf(string branch)
        {
            return _remoteBranches[branch];
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
            return _branches
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x => new GitBranch { Name = x.Key, Sha = x.Value, IsCurrent = x.Key == Current })
                .ToList();
        }

        public bool IsValidBranchName(string name)
        {
            return !name.Contains(' ', StringComparison.Ordinal);
        }

        public void CreateBranch(string folderPath, string name, string commitSha)
        {
            _branches[name] = commitSha;
        }

        public void DeleteBranch(string folderPath, string name)
        {
            _branches.Remove(name);
        }

        public void SwitchBranch(string folderPath, string name)
        {
            WriteFolder(folderPath, _branches[name]);

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

        public List<GitCommit> CommitsBetween(string folderPath, string commitSha, string? exceptCommitSha)
        {
            return Between(commitSha, exceptCommitSha);
        }

        public void Reset(string folderPath, string commitSha)
        {
            WriteFolder(folderPath, commitSha);

            _branches[Current] = commitSha;
        }

        public string? RemoteUrl(string folderPath)
        {
            return Remote;
        }

        public void SetRemoteUrl(string folderPath, string? url)
        {
            Remote = url;
        }

        public void Fetch(string folderPath, string token)
        {
            Tokens.Add(token);

            if (FetchError != null)
                throw new InvalidOperationException(FetchError);
        }

        public void Push(string folderPath, IReadOnlyList<string> branches, string token)
        {
            Tokens.Add(token);

            foreach (string branch in branches)
            {
                _remoteBranches[branch] = _branches[branch];
                Pushed.Add(branch);
            }
        }

        public List<GitBranch> RemoteBranches(string folderPath)
        {
            return _remoteBranches
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x => new GitBranch { Name = x.Key, Sha = x.Value })
                .ToList();
        }

        private string NewCommit(Dictionary<string, byte[]> files)
        {
            _nextCommit++;
            string sha = "commit-" + _nextCommit;
            _commits[sha] = files;

            return sha;
        }

        // Any commit not known here is the last one on the branch checked out.
        private Dictionary<string, byte[]> FilesAt(string? commitSha)
        {
            if (commitSha != null && _commits.TryGetValue(commitSha, out Dictionary<string, byte[]>? files))
                return files;

            return Committed;
        }

        // As a checkout would: what the commit lacks removed, the rest written.
        private void WriteFolder(string folderPath, string commitSha)
        {
            Dictionary<string, byte[]> files = _commits[commitSha];

            foreach (string path in Committed.Keys.Except(files.Keys))
                File.Delete(Path.Combine(folderPath, path));

            foreach ((string path, byte[] bytes) in files)
            {
                string fullPath = Path.Combine(folderPath, path);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                File.WriteAllBytes(fullPath, bytes);
            }
        }
    }
}
