using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace InventoryKamera
{
    /// <summary>Hosting service of a game data repository.</summary>
    public enum GameDataHost
    {
        GitLab,
        GitHub,
    }

    /// <summary>
    /// Where the updater downloads game data from: a GitLab or GitHub repository and branch laid
    /// out like Dimbreath's (ExcelBinOutput/, TextMap/). Configurable in Settings so a repository
    /// move (as when AnimeGameData was replaced by animegamedata2 at 6.6) needs no code change.
    /// </summary>
    public sealed class GameDataSource
    {
        public const string DefaultRepoUrl = "https://gitlab.com/Dimbreath/animegamedata2";
        public const string DefaultBranch = "main";

        public static GameDataSource Default { get; } = Parse(DefaultRepoUrl, DefaultBranch);

        public GameDataHost Host { get; }

        /// <summary>Repository path, e.g. "Dimbreath/animegamedata2".</summary>
        public string RepoPath { get; }

        public string Branch { get; }

        /// <summary>Normalised repository URL, e.g. "https://gitlab.com/Dimbreath/animegamedata2".</summary>
        public string RepoUrl { get; }

        /// <summary>Base URL for raw files, ending in "/". Append e.g. "TextMap/TextMapEN.json".</summary>
        public string RawBaseUrl { get; }

        /// <summary>API URL listing the branch's latest commits, used to find the game version.</summary>
        public string CommitsApiUrl { get; }

        private GameDataSource(GameDataHost host, string repoPath, string branch)
        {
            Host = host;
            RepoPath = repoPath;
            Branch = branch;

            if (host == GameDataHost.GitLab)
            {
                RepoUrl = $"https://gitlab.com/{repoPath}";
                RawBaseUrl = $"{RepoUrl}/-/raw/{Uri.EscapeDataString(branch)}/";
                // GitLab's API accepts the URL-encoded "namespace/project" path in place of the numeric ID
                CommitsApiUrl = $"https://gitlab.com/api/v4/projects/{Uri.EscapeDataString(repoPath)}/repository/commits?ref_name={Uri.EscapeDataString(branch)}";
            }
            else
            {
                RepoUrl = $"https://github.com/{repoPath}";
                RawBaseUrl = $"https://raw.githubusercontent.com/{repoPath}/{Uri.EscapeDataString(branch)}/";
                CommitsApiUrl = $"https://api.github.com/repos/{repoPath}/commits?sha={Uri.EscapeDataString(branch)}";
            }
        }

        /// <summary>Parses user input, throwing <see cref="FormatException"/> with a readable message if invalid.</summary>
        public static GameDataSource Parse(string repoUrl, string branch)
        {
            if (!TryParse(repoUrl, branch, out var source, out var error)) throw new FormatException(error);
            return source;
        }

        /// <summary>
        /// Accepts repository URLs as copied from a browser, e.g. "https://gitlab.com/Owner/Repo",
        /// ".../Repo.git", ".../-/tree/main" (GitLab) or ".../tree/main" (GitHub). A branch in the
        /// URL is used when <paramref name="branch"/> is blank.
        /// </summary>
        public static bool TryParse(string repoUrl, string branch, out GameDataSource source, out string error)
        {
            source = null;
            error = null;

            if (string.IsNullOrWhiteSpace(repoUrl))
            {
                error = "Enter the game data repository URL.";
                return false;
            }

            string text = repoUrl.Trim();
            if (!text.Contains("://")) text = "https://" + text;

            if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http"))
            {
                error = $"'{repoUrl.Trim()}' is not a valid URL.";
                return false;
            }

            GameDataHost host;
            string hostName = uri.Host.ToLowerInvariant();
            if (hostName == "gitlab.com" || hostName == "www.gitlab.com") host = GameDataHost.GitLab;
            else if (hostName == "github.com" || hostName == "www.github.com") host = GameDataHost.GitHub;
            else
            {
                error = $"Only gitlab.com and github.com repositories are supported (got '{uri.Host}').";
                return false;
            }

            string path = uri.AbsolutePath.Trim('/');
            string branchFromUrl = null;

            // Strip a browser "tree" suffix and remember its branch
            var tree = host == GameDataHost.GitLab
                ? Regex.Match(path, @"^(?<repo>.+?)/-/(?:tree|blob|raw)/(?<branch>[^/]+)")
                : Regex.Match(path, @"^(?<repo>[^/]+/[^/]+)/(?:tree|blob)/(?<branch>[^/]+)");
            if (tree.Success)
            {
                path = tree.Groups["repo"].Value;
                branchFromUrl = Uri.UnescapeDataString(tree.Groups["branch"].Value);
            }
            else if (host == GameDataHost.GitLab && path.Contains("/-/"))
            {
                path = path.Substring(0, path.IndexOf("/-/", StringComparison.Ordinal));
            }

            if (path.EndsWith(".git", StringComparison.OrdinalIgnoreCase)) path = path.Substring(0, path.Length - 4);

            var segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            bool validPath = host == GameDataHost.GitHub ? segments.Length == 2 : segments.Length >= 2;
            if (!validPath)
            {
                error = host == GameDataHost.GitHub
                    ? "GitHub URLs must look like https://github.com/owner/repository."
                    : "GitLab URLs must look like https://gitlab.com/owner/repository.";
                return false;
            }

            string finalBranch = string.IsNullOrWhiteSpace(branch) ? branchFromUrl : branch.Trim();
            if (string.IsNullOrWhiteSpace(finalBranch))
            {
                error = "Enter the branch to use (for example 'main' or 'master').";
                return false;
            }

            source = new GameDataSource(host, string.Join("/", segments), finalBranch);
            return true;
        }

        /// <summary>
        /// Finds the newest game version (e.g. "7.2.0") in the commits API response. Dimbreath's
        /// commit titles start with the version; the first commit mentioning one wins.
        /// </summary>
        public Version ParseLatestVersion(string commitsJson)
        {
            foreach (var message in CommitMessages(commitsJson))
            {
                var match = Regex.Match(message ?? "", @"(\d+\.\d+\.\d+)");
                if (match.Success) return new Version(match.Groups[1].Value);
            }
            return null;
        }

        private IEnumerable<string> CommitMessages(string commitsJson)
        {
            var token = JToken.Parse(commitsJson);
            if (!(token is JArray commits))
            {
                // Error responses are objects, e.g. {"message":"404 Project Not Found"}
                string message = (string)token["message"] ?? token.ToString();
                throw new FormatException($"Unexpected response from {Host}: {message}");
            }

            foreach (var commit in commits)
            {
                yield return Host == GameDataHost.GitLab
                    ? (string)commit["title"]
                    : (string)commit["commit"]?["message"];
            }
        }

        public override string ToString() => $"{RepoUrl} ({Branch})";
    }
}
