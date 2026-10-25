using Core.Models.Business.Git;

namespace Core.Ports
{
    /// <summary>
    /// A git repository in a folder: making one, what changed in it, committing, and its history.
    /// Every path inside the repository is relative to its folder, with forward slashes.
    /// </summary>
    public interface IGitService
    {
        /// <summary>Makes a repository in the folder when it has none, on the branch main. True when it made one.</summary>
        bool EnsureRepositoryFolderExists(string folderPath);

        /// <summary>The branch checked out.</summary>
        string CurrentBranch(string folderPath);

        /// <summary>How many commits the branch has.</summary>
        int CommitCount(string folderPath);

        /// <summary>Every file on disk that differs from the last commit.</summary>
        List<GitFileChange> Changes(string folderPath);

        /// <summary>A file's bytes in a commit, or null when it had no such file. A null commit is the last one.</summary>
        byte[]? ReadFile(string folderPath, string? commitSha, string path);

        /// <summary>Every file in a commit, or none when there is no such commit. A null commit is the last one.</summary>
        List<string> Files(string folderPath, string? commitSha);

        /// <summary>Records these paths as they are on disk, and only these. Returns the new commit.</summary>
        GitCommit Commit(string folderPath, IReadOnlyList<string> paths, string message, GitSignature author);

        /// <summary>Newest first, the commits that changed anything under these paths, each a file or a folder. No paths is every commit.</summary>
        List<GitCommit> Log(string folderPath, IReadOnlyList<string> paths, int max);

        /// <summary>One commit, or null when there is none with that id.</summary>
        GitCommit? FindCommit(string folderPath, string commitSha);

        /// <summary>The files a commit changed against the one before it, under these paths.</summary>
        List<GitFileChange> CommitChanges(string folderPath, string commitSha, IReadOnlyList<string> paths);

        /// <summary>The name and email the user's git config gives, or null when it gives neither.</summary>
        GitSignature? ConfiguredAuthor(string folderPath);

        /// <summary>The local branches, by name. None before the first commit.</summary>
        List<GitBranch> Branches(string folderPath);

        /// <summary>Whether git takes this as a branch's name.</summary>
        bool IsValidBranchName(string name);

        /// <summary>A new branch at the last commit. It is not checked out.</summary>
        void CreateBranch(string folderPath, string name);

        /// <summary>Checks the branch out. Files that differ from the last commit are kept when the branch has them as that commit does.</summary>
        void SwitchBranch(string folderPath, string name);

        /// <summary>The files that differ between two commits.</summary>
        List<GitFileChange> Compare(string folderPath, string fromCommitSha, string toCommitSha);
    }
}
