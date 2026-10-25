using System.Text;

using Core.Enums;
using Core.Models.Business.Git;
using Platform.Shared.Git;

namespace Platform.Shared.Tests.Git
{
    /// <summary>
    /// A real repository in a folder of its own, through the real libgit2.
    /// </summary>
    public sealed class GitServiceTests : IDisposable
    {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "stepinflow-git-" + Guid.NewGuid().ToString("N"));
        private readonly GitService _git = new GitService();

        public GitServiceTests()
        {
            _git.EnsureRepositoryFolderExists(_folder);
        }

        public void Dispose()
        {
            // git marks its objects read only, and Windows will not delete a read only file.
            foreach (string file in Directory.EnumerateFiles(_folder, "*", SearchOption.AllDirectories))
                File.SetAttributes(file, FileAttributes.Normal);

            Directory.Delete(_folder, recursive: true);
        }

        private void Write(string path, string text)
        {
            string fullPath = Path.Combine(_folder, path);
            string folder = Path.GetDirectoryName(fullPath) ?? _folder;
            Directory.CreateDirectory(folder);

            File.WriteAllText(fullPath, text);
        }

        private GitCommit Commit(string message, params string[] paths)
        {
            GitSignature author = new GitSignature { Name = "Tester", Email = "tester@example.com", When = new DateTimeOffset(2026, 10, 24, 12, 0, 0, TimeSpan.Zero) };

            return _git.Commit(_folder, paths, message, author);
        }

        private string? Read(string? commitSha, string path)
        {
            byte[]? bytes = _git.ReadFile(_folder, commitSha, path);
            if (bytes == null)
                return null;

            return Encoding.UTF8.GetString(bytes);
        }


        [Fact]
        public void A_folder_becomes_a_repository_on_main_once()
        {
            _git.EnsureRepositoryFolderExists(_folder).ShouldBeFalse();

            _git.CurrentBranch(_folder).ShouldBe("main");
            _git.CommitCount(_folder).ShouldBe(0);
            _git.Log(_folder, ["a.txt"], 10).ShouldBeEmpty();
        }

        [Fact]
        public void Changes_name_what_is_new_changed_and_gone()
        {
            Write("a.txt", "one");
            Write("b.txt", "one");
            Commit("First", "a.txt", "b.txt");

            Write("a.txt", "two");
            File.Delete(Path.Combine(_folder, "b.txt"));
            Write("folder/c.txt", "new");

            _git.Changes(_folder).Select(x => (x.Path, x.Kind)).OrderBy(x => x.Path).ShouldBe(
            [
                ("a.txt", GitChangeKindEnum.MODIFIED),
                ("b.txt", GitChangeKindEnum.DELETED),
                ("folder/c.txt", GitChangeKindEnum.ADDED),
            ]);
        }

        [Fact]
        public void A_commit_records_only_the_paths_it_is_given()
        {
            Write("a.txt", "one");
            Write("b.txt", "one");

            Commit("Only a", "a.txt");

            _git.Changes(_folder).ShouldHaveSingleItem().Path.ShouldBe("b.txt");
            _git.CommitCount(_folder).ShouldBe(1);
            Read(null, "b.txt").ShouldBeNull();
        }

        [Fact]
        public void A_deleted_file_is_committed_as_gone()
        {
            Write("a.txt", "one");
            Commit("First", "a.txt");
            File.Delete(Path.Combine(_folder, "a.txt"));

            Commit("Remove a", "a.txt");

            _git.Changes(_folder).ShouldBeEmpty();
            Read(null, "a.txt").ShouldBeNull();
        }

        [Fact]
        public void A_file_reads_back_as_it_was_in_each_commit()
        {
            Write("a.txt", "one");
            GitCommit first = Commit("First", "a.txt");
            Write("a.txt", "two");

            Read(null, "a.txt").ShouldBe("one");

            GitCommit second = Commit("Second", "a.txt");

            Read(first.Sha, "a.txt").ShouldBe("one");
            Read(null, "a.txt").ShouldBe("two");
            second.ParentSha.ShouldBe(first.Sha);
            first.ParentSha.ShouldBeNull();
            second.Message.ShouldBe("Second");
            second.AuthorName.ShouldBe("Tester");
        }

        [Fact]
        public void The_log_holds_only_the_commits_that_touched_a_flow_and_its_folder()
        {
            Write("Login.sflw", "one");
            Commit("Login", "Login.sflw");
            Write("Other.sflw", "one");
            Commit("Other", "Other.sflw");
            Write("Login/template-a.png", "picture");
            Commit("Login picture", "Login/template-a.png");

            _git.Log(_folder, ["Login.sflw", "Login"], 10).Select(x => x.Message).ShouldBe(["Login picture", "Login"]);
            _git.Log(_folder, ["Login.sflw", "Login"], 1).Select(x => x.Message).ShouldBe(["Login picture"]);
        }

        [Fact]
        public void A_commit_lists_what_it_changed_under_the_paths_asked_about()
        {
            Write("Login.sflw", "one");
            Write("Login/template-a.png", "a");
            Write("Login/template-b.png", "b");
            Write("Other.sflw", "one");
            GitCommit first = Commit("First", "Login.sflw", "Login/template-a.png", "Login/template-b.png", "Other.sflw");

            Write("Login/template-a.png", "a, captured again");
            File.Delete(Path.Combine(_folder, "Login", "template-b.png"));
            Write("Login/template-c.png", "c");
            Write("Other.sflw", "two");
            GitCommit second = Commit("Second", "Login/template-a.png", "Login/template-b.png", "Login/template-c.png", "Other.sflw");

            _git.CommitChanges(_folder, first.Sha, ["Login"]).Select(x => (x.Path, x.Kind)).OrderBy(x => x.Path).ShouldBe(
            [
                ("Login/template-a.png", GitChangeKindEnum.ADDED),
                ("Login/template-b.png", GitChangeKindEnum.ADDED),
            ]);

            _git.CommitChanges(_folder, second.Sha, ["Login.sflw", "Login"]).Select(x => (x.Path, x.Kind)).OrderBy(x => x.Path).ShouldBe(
            [
                ("Login/template-a.png", GitChangeKindEnum.MODIFIED),
                ("Login/template-b.png", GitChangeKindEnum.DELETED),
                ("Login/template-c.png", GitChangeKindEnum.ADDED),
            ]);
        }

        // The repository's .gitignore: everything out, then back in only what the app writes.
        [Fact]
        public void A_gitignore_that_lets_back_only_some_files_keeps_the_rest_out_of_the_changes()
        {
            Write(".gitignore", "*\n!*/\n!*.sflw\n!*.csv\n!*.png\n!.gitattributes\n!.gitignore\n");
            Write("Login.sflw", "script");
            Write("Login.csv", "values");
            Write("Login/template-a.png", "picture");
            Write("Login/notes.txt", "hand written");
            Write("Login/photo.jpg", "picture");
            Write("Thumbs.db", "clutter");
            Write(".DS_Store", "clutter");

            _git.Changes(_folder).Select(x => x.Path).ShouldBe([".gitignore", "Login.csv", "Login.sflw", "Login/template-a.png"], ignoreOrder: true);
        }

        [Fact]
        public void An_unknown_commit_is_nothing_rather_than_an_error()
        {
            _git.FindCommit(_folder, "0123456789abcdef0123456789abcdef01234567").ShouldBeNull();
            _git.ReadFile(_folder, "0123456789abcdef0123456789abcdef01234567", "a.txt").ShouldBeNull();
        }
    }
}
