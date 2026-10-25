using Core.Enums;
using Core.Models.Business.Git;
using Core.Ports;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace Platform.Shared.Git
{
    /// <summary>
    /// The repository through LibGit2Sharp, in process. Each call opens the repository and lets it
    /// go, because a Repository is not safe to share between threads; whoever calls keeps the calls
    /// one at a time. The remote is reached over HTTPS only, with a token: LibGit2Sharp speaks
    /// neither SSH nor the Git Credential Manager's sign-in.
    /// </summary>
    public sealed class GitService : IGitService
    {
        private const string BRANCH = "main";
        private const string REMOTE = "origin";

        // Any user name with the token as the password: GitHub, GitLab and Azure DevOps all take that.
        private const string TOKEN_USER = "x-access-token";


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

            return ChangesOf(treeChanges).Where(x => IsUnder(x.Path, paths)).ToList();
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

        public List<GitBranch> Branches(string folderPath)
        {
            using Repository repository = new Repository(folderPath);

            List<GitBranch> branches = new List<GitBranch>();
            foreach (Branch branch in repository.Branches.Where(x => !x.IsRemote && x.Tip != null))
            {
                branches.Add(new GitBranch
                {
                    Name = branch.FriendlyName,
                    Sha = branch.Tip.Sha,
                    IsCurrent = branch.IsCurrentRepositoryHead,
                    When = branch.Tip.Author.When,
                });
            }

            return branches.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public bool IsValidBranchName(string name)
        {
            return Reference.IsValidName("refs/heads/" + name);
        }

        public void CreateBranch(string folderPath, string name, string commitSha)
        {
            using Repository repository = new Repository(folderPath);

            repository.CreateBranch(name, commitSha);
        }

        public void DeleteBranch(string folderPath, string name)
        {
            using Repository repository = new Repository(folderPath);

            repository.Branches.Remove(name);
        }

        public void SwitchBranch(string folderPath, string name)
        {
            using Repository repository = new Repository(folderPath);

            Branch branch = repository.Branches[name]
                ?? throw new InvalidOperationException($"There is no branch called \"{name}\".");

            Commands.Checkout(repository, branch);
        }

        public List<GitFileChange> Compare(string folderPath, string fromCommitSha, string toCommitSha)
        {
            using Repository repository = new Repository(folderPath);

            Commit from = repository.Lookup<Commit>(fromCommitSha);
            Commit to = repository.Lookup<Commit>(toCommitSha);
            TreeChanges treeChanges = repository.Diff.Compare<TreeChanges>(from.Tree, to.Tree);

            return ChangesOf(treeChanges);
        }

        public List<GitCommit> CommitsBetween(string folderPath, string commitSha, string? exceptCommitSha)
        {
            using Repository repository = new Repository(folderPath);

            CommitFilter filter = new CommitFilter
            {
                IncludeReachableFrom = commitSha,
                ExcludeReachableFrom = exceptCommitSha,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time,
            };

            return repository.Commits.QueryBy(filter).Select(ToModel).ToList();
        }

        public void Reset(string folderPath, string commitSha)
        {
            using Repository repository = new Repository(folderPath);

            Commit commit = repository.Lookup<Commit>(commitSha)
                ?? throw new InvalidOperationException($"There is no commit {commitSha}.");

            repository.Reset(ResetMode.Hard, commit);
        }

        public string? RemoteUrl(string folderPath)
        {
            using Repository repository = new Repository(folderPath);

            return repository.Network.Remotes[REMOTE]?.Url;
        }

        public void SetRemoteUrl(string folderPath, string? url)
        {
            using Repository repository = new Repository(folderPath);

            Remote? remote = repository.Network.Remotes[REMOTE];

            if (url == null)
            {
                if (remote != null)
                    repository.Network.Remotes.Remove(REMOTE);

                return;
            }

            if (remote == null)
                repository.Network.Remotes.Add(REMOTE, url);
            else
                repository.Network.Remotes.Update(REMOTE, x => x.Url = url);
        }

        public void Fetch(string folderPath, string token)
        {
            using Repository repository = new Repository(folderPath);

            Remote remote = RemoteOf(repository);
            List<string> refSpecs = remote.FetchRefSpecs.Select(x => x.Specification).ToList();
            FetchOptions options = new FetchOptions { CredentialsProvider = CredentialsFor(token), Prune = true };

            OverTheNetwork(() => Commands.Fetch(repository, REMOTE, refSpecs, options, null));
        }

        public void Push(string folderPath, IReadOnlyList<string> branches, string token)
        {
            using Repository repository = new Repository(folderPath);

            Remote remote = RemoteOf(repository);
            List<Branch> pushed = branches.Select(x => repository.Branches[x]).Where(x => x != null).ToList();

            // A branch the remote refused arrives here rather than as an exception.
            List<string> refused = new List<string>();
            PushOptions options = new PushOptions
            {
                CredentialsProvider = CredentialsFor(token),
                OnPushStatusError = x => refused.Add($"{x.Reference}: {x.Message}"),
            };

            List<string> refSpecs = pushed.Select(x => $"{x.CanonicalName}:{x.CanonicalName}").ToList();
            OverTheNetwork(() => repository.Network.Push(remote, refSpecs, options));

            if (refused.Count > 0)
                throw new InvalidOperationException(RemoteErrorOf(string.Join("; ", refused)));

            // So git itself, in the folder, says how far each is ahead or behind.
            foreach (Branch branch in pushed)
                repository.Branches.Update(branch, x => x.Remote = REMOTE, x => x.UpstreamBranch = branch.CanonicalName);
        }

        public List<GitBranch> RemoteBranches(string folderPath)
        {
            using Repository repository = new Repository(folderPath);

            string prefix = REMOTE + "/";
            List<GitBranch> branches = new List<GitBranch>();

            foreach (Branch branch in repository.Branches.Where(x => x.IsRemote && x.Tip != null && x.FriendlyName.StartsWith(prefix, StringComparison.Ordinal)))
            {
                // origin/HEAD names the remote's default branch, and is no branch of its own.
                string name = branch.FriendlyName[prefix.Length..];
                if (name == "HEAD")
                    continue;

                branches.Add(new GitBranch { Name = name, Sha = branch.Tip.Sha, When = branch.Tip.Author.When });
            }

            return branches.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
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

        private static Remote RemoteOf(Repository repository)
        {
            return repository.Network.Remotes[REMOTE]
                ?? throw new InvalidOperationException("There is no remote yet. Connect one in Settings, Repository.");
        }

        // The token is the password. A user name in the address, as Bitbucket's x-token-auth@ wants,
        // is kept. No token asks nothing, so a public remote still fetches.
        private static CredentialsHandler? CredentialsFor(string token)
        {
            if (token.Length == 0)
                return null;

            return (_, usernameFromUrl, _) => new UsernamePasswordCredentials
            {
                Username = string.IsNullOrEmpty(usernameFromUrl) ? TOKEN_USER : usernameFromUrl,
                Password = token,
            };
        }

        private static void OverTheNetwork(Action action)
        {
            try
            {
                action();
            }
            catch (LibGit2SharpException exception)
            {
                throw new InvalidOperationException(RemoteErrorOf(exception.Message), exception);
            }
        }

        // libgit2's words for what went wrong, said so the person knows what to change.
        private static string RemoteErrorOf(string message)
        {
            if (message.Contains("authentication", StringComparison.OrdinalIgnoreCase) || message.Contains("401", StringComparison.Ordinal) || message.Contains("403", StringComparison.Ordinal))
                return "The remote refused the token. It needs to be allowed to read and write this repository's contents.";

            if (message.Contains("404", StringComparison.Ordinal) || message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return "There is no repository at that address, or the token cannot see it.";

            if (message.Contains("fast-forward", StringComparison.OrdinalIgnoreCase) || message.Contains("fastforward", StringComparison.OrdinalIgnoreCase))
                return "The remote has commits this branch does not. Pull first.";

            return $"The remote could not be reached: {message}";
        }

        // A rename is the old file gone and the new one added, as a reviewer would read it.
        private static List<GitFileChange> ChangesOf(TreeChanges treeChanges)
        {
            List<GitFileChange> changes = new List<GitFileChange>();

            foreach (TreeEntryChanges change in treeChanges)
            {
                if (change.Status == ChangeKind.Renamed)
                {
                    changes.Add(new GitFileChange { Path = change.OldPath, Kind = GitChangeKindEnum.DELETED });
                    changes.Add(new GitFileChange { Path = change.Path, Kind = GitChangeKindEnum.ADDED });
                    continue;
                }

                GitChangeKindEnum kind = GitChangeKindEnum.MODIFIED;
                if (change.Status == ChangeKind.Added)
                    kind = GitChangeKindEnum.ADDED;
                else if (change.Status == ChangeKind.Deleted)
                    kind = GitChangeKindEnum.DELETED;

                changes.Add(new GitFileChange { Path = change.Path, Kind = kind });
            }

            return changes;
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
