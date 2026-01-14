using FluentAssertions;
using Moq;
using ShipSquire.Application.DTOs;
using ShipSquire.Infrastructure.Services;
using System.Text;
using Xunit;

namespace ShipSquire.Tests.Unit.Services;

public class GitHubRepoAnalyzerTests
{
    private readonly Mock<IGitHubApiClient> _mockGitHubClient;
    private readonly GitHubRepoAnalyzer _analyzer;
    private const string TestAccessToken = "test-token";
    private const string TestOwner = "testowner";
    private const string TestRepo = "testrepo";

    public GitHubRepoAnalyzerTests()
    {
        _mockGitHubClient = new Mock<IGitHubApiClient>();
        _analyzer = new GitHubRepoAnalyzer(_mockGitHubClient.Object);
    }

    [Fact]
    public async Task AnalyzeRepository_WithAspNetProject_ShouldDetectCorrectly()
    {
        // Arrange
        var tree = CreateMockTree(new[]
        {
            "src/MyApp/MyApp.csproj",
            "src/MyApp/Program.cs",
            "src/MyApp/Properties/launchSettings.json",
            "Dockerfile",
            "docker-compose.yml",
            ".github/workflows/ci.yml",
            "README.md"
        });

        var launchSettings = @"{
            ""profiles"": {
                ""http"": {
                    ""commandName"": ""Project"",
                    ""dotnetRunMessages"": true,
                    ""launchBrowser"": true,
                    ""applicationUrl"": ""http://localhost:5000"",
                    ""environmentVariables"": {
                        ""ASPNETCORE_ENVIRONMENT"": ""Development""
                    }
                },
                ""https"": {
                    ""commandName"": ""Project"",
                    ""dotnetRunMessages"": true,
                    ""launchBrowser"": true,
                    ""applicationUrl"": ""https://localhost:5001;http://localhost:5000"",
                    ""environmentVariables"": {
                        ""ASPNETCORE_ENVIRONMENT"": ""Development""
                    }
                }
            }
        }";

        var dockerfile = @"FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 80
EXPOSE 443
COPY . .
ENTRYPOINT [""dotnet"", ""MyApp.dll""]";

        _mockGitHubClient.Setup(x => x.GetRepositoryTreeAsync(TestAccessToken, TestOwner, TestRepo, null, default))
            .ReturnsAsync(tree);

        _mockGitHubClient.Setup(x => x.GetFileContentAsync(TestAccessToken, TestOwner, TestRepo, "Dockerfile", default))
            .ReturnsAsync(CreateMockFileContent("Dockerfile", dockerfile));

        _mockGitHubClient.Setup(x => x.GetFileContentAsync(TestAccessToken, TestOwner, TestRepo, "src/MyApp/Properties/launchSettings.json", default))
            .ReturnsAsync(CreateMockFileContent("launchSettings.json", launchSettings));

        // Act
        var result = await _analyzer.AnalyzeRepositoryAsync(TestAccessToken, TestOwner, TestRepo);

        // Assert
        result.HasDockerfile.Should().BeTrue();
        result.HasCompose.Should().BeTrue();
        result.HasGithubActions.Should().BeTrue();
        result.HasReadme.Should().BeTrue();
        result.HasLaunchSettings.Should().BeTrue();
        result.HasCsproj.Should().BeTrue();
        result.HasKubernetes.Should().BeFalse();
        result.AppType.Should().Be("aspnet");
        result.DetectedPorts.Should().Contain(new[] { 80, 443, 5000, 5001 });
        result.TechnologyStack.Should().Contain("aspnet");
    }

    [Fact]
    public async Task AnalyzeRepository_WithNodeProject_ShouldDetectCorrectly()
    {
        // Arrange
        var tree = CreateMockTree(new[]
        {
            "package.json",
            "src/index.js",
            "src/app.ts",
            "Dockerfile",
            "k8s/deployment.yaml",
            "README.md"
        });

        var dockerfile = @"FROM node:20-alpine
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
EXPOSE 3000
CMD [""npm"", ""start""]";

        _mockGitHubClient.Setup(x => x.GetRepositoryTreeAsync(TestAccessToken, TestOwner, TestRepo, null, default))
            .ReturnsAsync(tree);

        _mockGitHubClient.Setup(x => x.GetFileContentAsync(TestAccessToken, TestOwner, TestRepo, "Dockerfile", default))
            .ReturnsAsync(CreateMockFileContent("Dockerfile", dockerfile));

        // Act
        var result = await _analyzer.AnalyzeRepositoryAsync(TestAccessToken, TestOwner, TestRepo);

        // Assert
        result.HasDockerfile.Should().BeTrue();
        result.HasCompose.Should().BeFalse();
        result.HasKubernetes.Should().BeTrue();
        result.HasGithubActions.Should().BeFalse();
        result.HasReadme.Should().BeTrue();
        result.AppType.Should().Be("node");
        result.DetectedPorts.Should().Contain(3000);
        result.TechnologyStack.Should().Contain("node");
    }

    [Fact]
    public async Task AnalyzeRepository_WithMixedTechnologies_ShouldDetectMixed()
    {
        // Arrange
        var tree = CreateMockTree(new[]
        {
            "backend/MyApi.csproj",
            "backend/Program.cs",
            "frontend/package.json",
            "frontend/src/app.tsx",
            "Dockerfile.api",
            "Dockerfile.web",
            "docker-compose.yml"
        });

        _mockGitHubClient.Setup(x => x.GetRepositoryTreeAsync(TestAccessToken, TestOwner, TestRepo, null, default))
            .ReturnsAsync(tree);

        // Act
        var result = await _analyzer.AnalyzeRepositoryAsync(TestAccessToken, TestOwner, TestRepo);

        // Assert
        result.AppType.Should().Be("mixed");
        result.TechnologyStack.Should().Contain("aspnet");
        result.TechnologyStack.Should().Contain("node");
        result.HasCompose.Should().BeTrue();
    }

    [Fact]
    public async Task AnalyzeRepository_EmptyRepo_ShouldReturnUnknown()
    {
        // Arrange
        var tree = CreateMockTree(Array.Empty<string>());

        _mockGitHubClient.Setup(x => x.GetRepositoryTreeAsync(TestAccessToken, TestOwner, TestRepo, null, default))
            .ReturnsAsync(tree);

        // Act
        var result = await _analyzer.AnalyzeRepositoryAsync(TestAccessToken, TestOwner, TestRepo);

        // Assert
        result.HasDockerfile.Should().BeFalse();
        result.HasCompose.Should().BeFalse();
        result.HasKubernetes.Should().BeFalse();
        result.HasGithubActions.Should().BeFalse();
        result.HasReadme.Should().BeFalse();
        result.AppType.Should().Be("unknown");
        result.DetectedPorts.Should().BeEmpty();
        result.TechnologyStack.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeRepository_WithHelm_ShouldDetectKubernetes()
    {
        // Arrange
        var tree = CreateMockTree(new[]
        {
            "src/app.py",
            "helm/Chart.yaml",
            "helm/values.yaml",
            "helm/templates/deployment.yaml"
        });

        _mockGitHubClient.Setup(x => x.GetRepositoryTreeAsync(TestAccessToken, TestOwner, TestRepo, null, default))
            .ReturnsAsync(tree);

        // Act
        var result = await _analyzer.AnalyzeRepositoryAsync(TestAccessToken, TestOwner, TestRepo);

        // Assert
        result.HasKubernetes.Should().BeTrue();
        result.AppType.Should().Be("python");
        result.TechnologyStack.Should().Contain("python");
    }

    [Fact]
    public async Task AnalyzeRepository_WithMultipleDockerfiles_ShouldExtractAllPorts()
    {
        // Arrange
        var tree = CreateMockTree(new[]
        {
            "services/api/Dockerfile",
            "services/web/Dockerfile",
            "src/app.cs"
        });

        var dockerfile1 = @"FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 8080
EXPOSE 8443";

        var dockerfile2 = @"FROM nginx:alpine
EXPOSE 80 443";

        _mockGitHubClient.Setup(x => x.GetRepositoryTreeAsync(TestAccessToken, TestOwner, TestRepo, null, default))
            .ReturnsAsync(tree);

        _mockGitHubClient.Setup(x => x.GetFileContentAsync(TestAccessToken, TestOwner, TestRepo, "services/api/Dockerfile", default))
            .ReturnsAsync(CreateMockFileContent("Dockerfile", dockerfile1));

        _mockGitHubClient.Setup(x => x.GetFileContentAsync(TestAccessToken, TestOwner, TestRepo, "services/web/Dockerfile", default))
            .ReturnsAsync(CreateMockFileContent("Dockerfile", dockerfile2));

        // Act
        var result = await _analyzer.AnalyzeRepositoryAsync(TestAccessToken, TestOwner, TestRepo);

        // Assert
        result.HasDockerfile.Should().BeTrue();
        result.DetectedPorts.Should().Contain(new[] { 80, 443, 8080, 8443 });
    }

    [Fact]
    public async Task AnalyzeRepository_WithMultipleLanguages_ShouldDetectAll()
    {
        // Arrange
        var tree = CreateMockTree(new[]
        {
            "api/main.go",
            "worker/script.py",
            "frontend/app.rb",
            "README.md"
        });

        _mockGitHubClient.Setup(x => x.GetRepositoryTreeAsync(TestAccessToken, TestOwner, TestRepo, null, default))
            .ReturnsAsync(tree);

        // Act
        var result = await _analyzer.AnalyzeRepositoryAsync(TestAccessToken, TestOwner, TestRepo);

        // Assert
        result.AppType.Should().Be("mixed");
        result.TechnologyStack.Should().Contain("go");
        result.TechnologyStack.Should().Contain("python");
        result.TechnologyStack.Should().Contain("ruby");
        result.TechnologyStack.Should().HaveCount(3);
    }

    [Fact]
    public async Task AnalyzeRepository_WithWeirdLayout_ShouldStillDetect()
    {
        // Arrange - Dockerfile and compose in unusual locations
        var tree = CreateMockTree(new[]
        {
            "deployments/prod/Dockerfile",
            "deployments/dev/docker-compose.yaml",
            "very/deep/nested/path/to/code/app.cs",
            ".github/workflows/deploy.yml"
        });

        var dockerfile = "FROM mcr.microsoft.com/dotnet/aspnet:8.0\nEXPOSE 5000";

        _mockGitHubClient.Setup(x => x.GetRepositoryTreeAsync(TestAccessToken, TestOwner, TestRepo, null, default))
            .ReturnsAsync(tree);

        _mockGitHubClient.Setup(x => x.GetFileContentAsync(TestAccessToken, TestOwner, TestRepo, "deployments/prod/Dockerfile", default))
            .ReturnsAsync(CreateMockFileContent("Dockerfile", dockerfile));

        // Act
        var result = await _analyzer.AnalyzeRepositoryAsync(TestAccessToken, TestOwner, TestRepo);

        // Assert
        result.HasDockerfile.Should().BeTrue();
        result.HasCompose.Should().BeTrue();
        result.HasGithubActions.Should().BeTrue();
        result.AppType.Should().Be("aspnet");
        result.DetectedPorts.Should().Contain(5000);
    }

    // Helper methods
    private static GitHubTreeResponse CreateMockTree(string[] paths)
    {
        var items = paths.Select(path => new GitHubTreeItem(
            path: path,
            mode: "100644",
            type: "blob",
            sha: Guid.NewGuid().ToString(),
            size: 1024,
            url: $"https://api.github.com/repos/test/test/git/blobs/{Guid.NewGuid()}"
        )).ToList();

        return new GitHubTreeResponse(
            sha: Guid.NewGuid().ToString(),
            url: "https://api.github.com/repos/test/test/git/trees/main",
            tree: items,
            truncated: false
        );
    }

    private static GitHubFileContentResponse CreateMockFileContent(string name, string content)
    {
        var base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));

        return new GitHubFileContentResponse(
            name: name,
            path: name,
            sha: Guid.NewGuid().ToString(),
            size: content.Length,
            url: $"https://api.github.com/repos/test/test/contents/{name}",
            content: base64Content,
            encoding: "base64"
        );
    }
}
