using Core.Enums;
using Core.Models.Business.Git;
using Core.Ports;
using LibGit2Sharp;

namespace Platform.Shared.Git
{
    /// <summary>
    /// The repository through LibGit2Sharp, in process. Each call opens the repository and lets it
    /// go, because a Repository is not safe to share between threads; whoever calls keeps the calls
    /// one at a time.
    /// </summary>
    public sealed class GitService : IGitService
    {
        private const string BRANCH = "main";


        // ================================================================
        // Public methods
        // ================================================================

        public bool EnsureRepositoryFolderExists(string folderPath)
        {
            Directory.CreateDirectory(folderPath);

            if (Repository.IsValid(folderPath))
                return false;

            Repository.Init(folderPath);

            // main whatever the machine's git config says, set before the first commit makes the
            // branch real.
            string head = Path.Combine(folderPath, ".git", "HEAD");
            File.WriteAllText(head, $"ref: refs/heads/{BRANCH}\n");

            return true;
        }

        public string CurrentBranch(string folderPath)
        {
            using Repository repository = new Repository(folderPath);

            return repository.Head.FriendlyName;
        }

        public int CommitCount(string folderPath)
        {
            using Repository repository = new Repository(folderPath);

            if (repository.Info.IsHeadUnborn)
                return 0;

            return repository.Commits.Count();
        }

        public List<GitFileChange> Changes(string folderPath)
        {
            using Repository repository = new Repository(folderPath);

            StatusOptions options = new StatusOptions
            {
                IncludeUntracked = true,
                RecurseUntrackedDirs = true,
                IncludeIgnored = false,
            };

            List<GitFileChange> changes = new List<GitFileChange>();
            RepositoryStatus repositoryStatus = repository.RetrieveStatus(options); // Retrieve file changes based on the last commit. RepositoryStatus is IEnumerable<StatusEntry>

            foreach (StatusEntry entry in repositoryStatus)
            {
                GitChangeKindEnum? kind = ChangeKindOf(entry.State);
                if (kind == null) // Unaltered or Ignored.
                    continue;

                changes.Add(new GitFileChange { Path = entry.FilePath, Kind = kind.Value });
            }

            return changes;
        }

        public byte[]? ReadFile(string folderPath, string? commitSha, string path)
        {
            using Repository repository = new Repository(folderPath);

            Commit? commit;
            if (commitSha == null)
                commit = repository.Head.Tip;
            else
                commit = repository.Lookup<Commit>(commitSha);

            if (commit == null)
                return null;

            if (commit[path]?.Target is not Blob blob)
                return null;

            using Stream content = blob.GetContentStream();
            using MemoryStream bytes = new MemoryStream();
            content.CopyTo(bytes);

            return bytes.ToArray();
        }

        public List<string> Files(string folderPath, string? commitSha)
        {
            using Repository repository = new Repository(folderPath);

            Commit? commit;
            if (commitSha == null)
                commit = repository.Head.Tip;
            else
                commit = repository.Lookup<Commit>(commitSha);

            List<string> paths = new List<string>();
            if (commit != null)
                AddFiles(commit.Tree, paths);

            return paths;
        }

        public GitCommit Commit(string folderPath, IReadOnlyList<string> paths, string message, GitSignature author)
        {
            using Repository repository = new Repository(folderPath);

            // From the last commit, so nothing staged before comes along: only these paths change.
            if (repository.Head.Tip == null)
                repository.Index.Clear();
            else
                repository.Index.Replace(repository.Head.Tip.Tree);

            foreach (string path in paths)
            {
                string fullPath = Path.Combine(folderPath, path);

                if (File.Exists(fullPath))
                    repository.Index.Add(path);
                else
                    repository.Index.Remove(path);
            }

            repository.Index.Write();

            Signature signature = new Signature(author.Name, author.Email, author.When);
            Commit commit = repository.Commit(message, signature, signature);

            return ToModel(commit);
        }

        public List<GitCommit> Log(string folderPath, IReadOnlyList<string> paths, int max)
        {
            using Repository repository = new Repository(folderPath);

            List<GitCommit> commits = new List<GitCommit>();
            if (repository.Info.IsHeadUnborn)
                return commits;

            CommitFilter filter = new CommitFilter
            {
                IncludeReachableFrom = repository.Head,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time,
            };

            foreach (Commit commit in repository.Commits.QueryBy(filter))
            {
                Commit? parent = commit.Parents.FirstOrDefault();

                // A folder is a tree, whose id changes when anything inside it does. No paths is every commit.
                bool touched = paths.Count == 0 || paths.Any(x => EntryShaOf(commit.Tree, x) != EntryShaOf(parent?.Tree, x));
                if (!touched)
                    continue;

                commits.Add(ToModel(commit));
                if (commits.Count >= max)
                    break;
            }

            return commits;
        }

        public GitCommit? FindCommit(string folderPath, string commitSha)
        {
            using Repository repository = new Repository(folderPath);

            Commit? commit = repository.Lookup<Commit>(commitSha);
            if (commit == null)
                return null;

            return ToModel(commit);
        }

        public List<GitFileChange> CommitChanges(string folderPath, string commitSha, IReadOnlyList<string> paths)
        {
            using Repository repository = new Repository(folderPath);

            List<GitFileChange> changes = new List<GitFileChange>();

            Commit? commit = repository.Lookup<Commit>(commitSha);
            if (commit == null)
                return changes;

            Commit? parent = commit.Parents.FirstOrDefault();
            TreeChanges treeChanges = repository.Diff.Compare<TreeChanges>(parent?.Tree, commit.Tree);

            foreach (TreeEntryChanges change in treeChanges)
            {
                // A rename is the old file gone and the new one added, as a reviewer would read it.
                if (change.Status == ChangeKind.Renamed)
                {
                    if (IsUnder(change.OldPath, paths))
                        changes.Add(new GitFileChange { Path = change.OldPath, Kind = GitChangeKindEnum.DELETED });

                    if (IsUnder(change.Path, paths))
                        changes.Add(new GitFileChange { Path = change.Path, Kind = GitChangeKindEnum.ADDED });

                    continue;
                }

                if (!IsUnder(change.Path, paths))
                    continue;

                GitChangeKindEnum kind = GitChangeKindEnum.MODIFIED;
                if (change.Status == ChangeKind.Added)
                    kind = GitChangeKindEnum.ADDED;
                else if (change.Status == ChangeKind.Deleted)
                    kind = GitChangeKindEnum.DELETED;

                changes.Add(new GitFileChange { Path = change.Path, Kind = kind });
            }

            return changes;
        }

        public GitSignature? ConfiguredAuthor(string folderPath)
        {
            using Repository repository = new Repository(folderPath);

            string name = repository.Config.Get<string>("user.name")?.Value ?? string.Empty;
            string email = repository.Config.Get<string>("user.email")?.Value ?? string.Empty;

            if (name.Length == 0 && email.Length == 0)
                return null;

            return new GitSignature { Name = name, Email = email };
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Every file under the tree, folders walked into. A tree entry's path uses the system's
        // separator, and every path in the port uses forward slashes.
        private static void AddFiles(Tree tree, List<string> paths)
        {
            foreach (TreeEntry entry in tree)
            {
                if (entry.TargetType == TreeEntryTargetType.Tree)
                    AddFiles((Tree)entry.Target, paths);
                else if (entry.TargetType == TreeEntryTargetType.Blob)
                    paths.Add(entry.Path.Replace('\\', '/'));
            }
        }

        // Against the last commit: new, gone or changed, whether staged or not.
        private static GitChangeKindEnum? ChangeKindOf(FileStatus state)
        {
            if (state.HasFlag(FileStatus.Ignored) || state == FileStatus.Unaltered)
                return null;

            if (state.HasFlag(FileStatus.NewInWorkdir) || state.HasFlag(FileStatus.NewInIndex))
                return GitChangeKindEnum.ADDED;

            if (state.HasFlag(FileStatus.DeletedFromWorkdir) || state.HasFlag(FileStatus.DeletedFromIndex))
                return GitChangeKindEnum.DELETED;

            return GitChangeKindEnum.MODIFIED;
        }

        private static string? EntryShaOf(Tree? tree, string path)
        {
            if (tree == null)
                return null;

            return tree[path]?.Target.Sha;
        }

        private static bool IsUnder(string path, IReadOnlyList<string> paths)
        {
            return paths.Any(x => path == x || path.StartsWith(x + "/", StringComparison.Ordinal));
        }

        private static GitCommit ToModel(Commit commit)
        {
            return new GitCommit
            {
                Sha = commit.Sha,
                ParentSha = commit.Parents.FirstOrDefault()?.Sha,
                Message = commit.Message.TrimEnd(),
                AuthorName = commit.Author.Name,
                AuthorEmail = commit.Author.Email,
                When = commit.Author.When,
            };
        }
    }
}
