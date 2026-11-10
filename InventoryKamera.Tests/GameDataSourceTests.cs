using System;
using Xunit;

namespace InventoryKamera.Tests
{
    public class GameDataSourceTests
    {
        [Fact]
        public void Default_IsAnimeGameData2OnMain()
        {
            var s = GameDataSource.Default;
            Assert.Equal(GameDataHost.GitLab, s.Host);
            Assert.Equal("Dimbreath/animegamedata2", s.RepoPath);
            Assert.Equal("main", s.Branch);
            Assert.Equal("https://gitlab.com/Dimbreath/animegamedata2/-/raw/main/", s.RawBaseUrl);
            // Path-encoded project instead of the numeric ID (verified live against gitlab.com)
            Assert.Equal("https://gitlab.com/api/v4/projects/Dimbreath%2Fanimegamedata2/repository/commits?ref_name=main", s.CommitsApiUrl);
        }

        [Theory]
        [InlineData("https://gitlab.com/Dimbreath/animegamedata2")]
        [InlineData("https://gitlab.com/Dimbreath/animegamedata2/")]
        [InlineData("https://gitlab.com/Dimbreath/animegamedata2.git")]
        [InlineData("gitlab.com/Dimbreath/animegamedata2")]
        [InlineData("  https://gitlab.com/Dimbreath/animegamedata2  ")]
        [InlineData("https://gitlab.com/Dimbreath/animegamedata2/-/commits/main")]
        public void Parse_GitLabUrlVariants_NormaliseToSameRepo(string url)
        {
            var s = GameDataSource.Parse(url, "main");
            Assert.Equal(GameDataHost.GitLab, s.Host);
            Assert.Equal("Dimbreath/animegamedata2", s.RepoPath);
            Assert.Equal("https://gitlab.com/Dimbreath/animegamedata2", s.RepoUrl);
        }

        [Fact]
        public void Parse_GitLabTreeUrl_TakesBranchFromUrlWhenBranchBlank()
        {
            var s = GameDataSource.Parse("https://gitlab.com/Dimbreath/AnimeGameData/-/tree/master", "");
            Assert.Equal("Dimbreath/AnimeGameData", s.RepoPath);
            Assert.Equal("master", s.Branch);
            Assert.Equal("https://gitlab.com/Dimbreath/AnimeGameData/-/raw/master/", s.RawBaseUrl);
        }

        [Fact]
        public void Parse_ExplicitBranch_WinsOverUrlBranch()
        {
            var s = GameDataSource.Parse("https://gitlab.com/Dimbreath/animegamedata2/-/tree/main", "dev");
            Assert.Equal("dev", s.Branch);
        }

        [Fact]
        public void Parse_GitHub_UsesRawGithubusercontentAndCommitsApi()
        {
            var s = GameDataSource.Parse("https://github.com/Owner/GameData/tree/main", null);
            Assert.Equal(GameDataHost.GitHub, s.Host);
            Assert.Equal("Owner/GameData", s.RepoPath);
            Assert.Equal("main", s.Branch);
            Assert.Equal("https://raw.githubusercontent.com/Owner/GameData/main/", s.RawBaseUrl);
            Assert.Equal("https://api.github.com/repos/Owner/GameData/commits?sha=main", s.CommitsApiUrl);
        }

        [Fact]
        public void Parse_GitLabSubgroup_IsAllowed()
        {
            var s = GameDataSource.Parse("https://gitlab.com/group/subgroup/project", "main");
            Assert.Equal("group/subgroup/project", s.RepoPath);
            Assert.Contains("group%2Fsubgroup%2Fproject", s.CommitsApiUrl);
        }

        [Theory]
        [InlineData("", "main", "Enter the game data repository URL.")]
        [InlineData("https://bitbucket.org/a/b", "main", "Only gitlab.com and github.com")]
        [InlineData("https://gitlab.com/onlyowner", "main", "GitLab URLs must look like")]
        [InlineData("https://github.com/a/b/c", "main", "GitHub URLs must look like")]
        [InlineData("https://gitlab.com/a/b", " ", "Enter the branch")]
        [InlineData("ftp://gitlab.com/a/b", "main", "is not a valid URL")]
        public void TryParse_Invalid_ReturnsReadableError(string url, string branch, string expectedText)
        {
            Assert.False(GameDataSource.TryParse(url, branch, out var source, out var error));
            Assert.Null(source);
            Assert.Contains(expectedText, error);
            Assert.Throws<FormatException>(() => GameDataSource.Parse(url, branch));
        }

        [Fact]
        public void ParseLatestVersion_GitLabCommits_ReadsTitle()
        {
            // Shape of gitlab.com's commits API (abridged); titles as in animegamedata2
            const string json = "[{\"id\":\"a\",\"title\":\"CNRELWin7.2.0_R49086080_S49291486_D49291486\"},{\"id\":\"b\",\"title\":\"CNRELWin7.1.0_R1\"}]";
            Assert.Equal(new Version(7, 2, 0), GameDataSource.Default.ParseLatestVersion(json));
        }

        [Fact]
        public void ParseLatestVersion_GitHubCommits_ReadsCommitMessage()
        {
            var gh = GameDataSource.Parse("https://github.com/Owner/GameData", "main");
            const string json = "[{\"sha\":\"a\",\"commit\":{\"message\":\"Update README\"}},{\"sha\":\"b\",\"commit\":{\"message\":\"OSRELWin7.3.0_R1\n\nbody\"}}]";
            Assert.Equal(new Version(7, 3, 0), gh.ParseLatestVersion(json));
        }

        [Fact]
        public void ParseLatestVersion_NoVersionedCommit_ReturnsNull()
        {
            Assert.Null(GameDataSource.Default.ParseLatestVersion("[{\"title\":\"Update README\"}]"));
        }

        [Fact]
        public void ParseLatestVersion_ErrorObject_ThrowsWithServerMessage()
        {
            var ex = Assert.Throws<FormatException>(() => GameDataSource.Default.ParseLatestVersion("{\"message\":\"404 Project Not Found\"}"));
            Assert.Contains("404 Project Not Found", ex.Message);
        }
    }
}
