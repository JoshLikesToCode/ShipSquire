using FluentAssertions;
using ShipSquire.Application.DTOs;
using ShipSquire.Application.Services;
using ShipSquire.Domain.Entities;
using Xunit;

namespace ShipSquire.Tests.Unit.Services;

public class RunbookDraftGeneratorTests
{
    private readonly RunbookDraftGenerator _generator;

    public RunbookDraftGeneratorTests()
    {
        _generator = new RunbookDraftGenerator();
    }

    [Fact]
    public void GenerateDraft_WithAspNetDockerCompose_GeneratesCorrectSections()
    {
        // Arrange
        var service = CreateService("MyAspNetApp", "my-aspnet-app");
        var analysis = new RepoAnalysisResult(
            HasDockerfile: true,
            HasCompose: true,
            HasKubernetes: false,
            HasGithubActions: true,
            DetectedPorts: new List<int> { 5000, 5001 },
            AppType: "aspnet",
            HasReadme: true,
            HasLaunchSettings: true,
            HasCsproj: true,
            TechnologyStack: new List<string> { "aspnet" }
        );

        // Act
        var result = _generator.GenerateDraft(service, analysis);

        // Assert
        result.Should().HaveCount(6);
        result.Should().ContainSingle(s => s.Key == "overview");
        result.Should().ContainSingle(s => s.Key == "deployment");
        result.Should().ContainSingle(s => s.Key == "rollback");
        result.Should().ContainSingle(s => s.Key == "health_checks");
        result.Should().ContainSingle(s => s.Key == "environment_variables");
        result.Should().ContainSingle(s => s.Key == "troubleshooting");

        var overview = result.First(s => s.Key == "overview");
        overview.Title.Should().Be("Overview");
        overview.Order.Should().Be(1);
        overview.BodyMarkdown.Should().Contain("MyAspNetApp");
        overview.BodyMarkdown.Should().Contain("ASP.NET Core");
        overview.BodyMarkdown.Should().Contain("Docker Compose");

        var deployment = result.First(s => s.Key == "deployment");
        deployment.Title.Should().Be("Deployment");
        deployment.Order.Should().Be(2);
        deployment.BodyMarkdown.Should().Contain("docker compose up");
        deployment.BodyMarkdown.Should().Contain("http://localhost:5000");
        deployment.BodyMarkdown.Should().Contain("http://localhost:5001");

        var envVars = result.First(s => s.Key == "environment_variables");
        envVars.BodyMarkdown.Should().Contain("ASPNETCORE_ENVIRONMENT");
        envVars.BodyMarkdown.Should().Contain("ASPNETCORE_URLS");
    }

    [Fact]
    public void GenerateDraft_WithNodeDockerfile_GeneratesNodeSpecificContent()
    {
        // Arrange
        var service = CreateService("NodeAPI", "node-api");
        var analysis = new RepoAnalysisResult(
            HasDockerfile: true,
            HasCompose: false,
            HasKubernetes: false,
            HasGithubActions: false,
            DetectedPorts: new List<int> { 3000 },
            AppType: "node",
            HasReadme: true,
            HasLaunchSettings: false,
            HasCsproj: false,
            TechnologyStack: new List<string> { "node" }
        );

        // Act
        var result = _generator.GenerateDraft(service, analysis);

        // Assert
        var deployment = result.First(s => s.Key == "deployment");
        deployment.BodyMarkdown.Should().Contain("docker build");
        deployment.BodyMarkdown.Should().Contain("NODE_ENV=production");
        deployment.BodyMarkdown.Should().Contain("http://localhost:3000");

        var envVars = result.First(s => s.Key == "environment_variables");
        envVars.BodyMarkdown.Should().Contain("NODE_ENV");
        envVars.BodyMarkdown.Should().Contain("PORT");

        var troubleshooting = result.First(s => s.Key == "troubleshooting");
        troubleshooting.BodyMarkdown.Should().Contain("npm install");
    }

    [Fact]
    public void GenerateDraft_WithKubernetes_IncludesK8sInstructions()
    {
        // Arrange
        var service = CreateService("K8sService", "k8s-service");
        var analysis = new RepoAnalysisResult(
            HasDockerfile: true,
            HasCompose: false,
            HasKubernetes: true,
            HasGithubActions: true,
            DetectedPorts: new List<int> { 8080 },
            AppType: "aspnet",
            HasReadme: true,
            HasLaunchSettings: false,
            HasCsproj: true,
            TechnologyStack: new List<string> { "aspnet" }
        );

        // Act
        var result = _generator.GenerateDraft(service, analysis);

        // Assert
        var overview = result.First(s => s.Key == "overview");
        overview.BodyMarkdown.Should().Contain("Kubernetes/Helm");

        var deployment = result.First(s => s.Key == "deployment");
        deployment.BodyMarkdown.Should().Contain("kubectl apply");
        deployment.BodyMarkdown.Should().Contain("helm upgrade");

        var rollback = result.First(s => s.Key == "rollback");
        rollback.BodyMarkdown.Should().Contain("kubectl rollout undo");

        var envVars = result.First(s => s.Key == "environment_variables");
        envVars.BodyMarkdown.Should().Contain("kubectl create configmap");
    }

    [Fact]
    public void GenerateDraft_WithAspNetNoDocker_GeneratesDotnetCommands()
    {
        // Arrange
        var service = CreateService("DotNetApp", "dotnet-app");
        var analysis = new RepoAnalysisResult(
            HasDockerfile: false,
            HasCompose: false,
            HasKubernetes: false,
            HasGithubActions: true,
            DetectedPorts: new List<int> { 5000 },
            AppType: "aspnet",
            HasReadme: true,
            HasLaunchSettings: true,
            HasCsproj: true,
            TechnologyStack: new List<string> { "aspnet" }
        );

        // Act
        var result = _generator.GenerateDraft(service, analysis);

        // Assert
        var deployment = result.First(s => s.Key == "deployment");
        deployment.BodyMarkdown.Should().Contain("dotnet restore");
        deployment.BodyMarkdown.Should().Contain("dotnet build");
        deployment.BodyMarkdown.Should().Contain("dotnet publish");
        deployment.BodyMarkdown.Should().Contain("DotNetApp.dll");

        var rollback = result.First(s => s.Key == "rollback");
        rollback.BodyMarkdown.Should().Contain("git revert");
    }

    [Fact]
    public void GenerateDraft_WithNoPorts_GeneratesGenericHealthChecks()
    {
        // Arrange
        var service = CreateService("NoPortsApp", "no-ports-app");
        var analysis = new RepoAnalysisResult(
            HasDockerfile: false,
            HasCompose: false,
            HasKubernetes: false,
            HasGithubActions: false,
            DetectedPorts: new List<int>(),
            AppType: "python",
            HasReadme: false,
            HasLaunchSettings: false,
            HasCsproj: false,
            TechnologyStack: new List<string> { "python" }
        );

        // Act
        var result = _generator.GenerateDraft(service, analysis);

        // Assert
        var healthChecks = result.First(s => s.Key == "health_checks");
        healthChecks.BodyMarkdown.Should().Contain("ps aux");
        healthChecks.BodyMarkdown.Should().Contain("tail -f");
    }

    [Fact]
    public void GenerateDraft_VerifyMarkdownStructure_Overview()
    {
        // Arrange
        var service = CreateService("TestService", "test-service", "A test service for unit testing");
        service.RepoUrl = "https://github.com/testorg/testrepo";

        var analysis = new RepoAnalysisResult(
            HasDockerfile: true,
            HasCompose: true,
            HasKubernetes: true,
            HasGithubActions: true,
            DetectedPorts: new List<int> { 8080 },
            AppType: "aspnet",
            HasReadme: true,
            HasLaunchSettings: true,
            HasCsproj: true,
            TechnologyStack: new List<string> { "aspnet" }
        );

        // Act
        var result = _generator.GenerateDraft(service, analysis);

        // Assert - Verify Overview structure
        var overview = result.First(s => s.Key == "overview");

        // Should contain service name as H1
        overview.BodyMarkdown.Should().Contain("# TestService");

        // Should contain description
        overview.BodyMarkdown.Should().Contain("A test service for unit testing");

        // Should contain repository URL
        overview.BodyMarkdown.Should().Contain("**Repository:** https://github.com/testorg/testrepo");

        // Should contain Technology Stack section
        overview.BodyMarkdown.Should().Contain("## Technology Stack");
        overview.BodyMarkdown.Should().Contain("- ASP.NET Core");

        // Should contain Infrastructure section
        overview.BodyMarkdown.Should().Contain("## Infrastructure");
        overview.BodyMarkdown.Should().Contain("- Docker");
        overview.BodyMarkdown.Should().Contain("- Docker Compose");
        overview.BodyMarkdown.Should().Contain("- Kubernetes/Helm");
        overview.BodyMarkdown.Should().Contain("- GitHub Actions CI/CD");
    }

    [Fact]
    public void GenerateDraft_VerifyMarkdownStructure_Deployment()
    {
        // Arrange
        var service = CreateService("DeployTest", "deploy-test");
        var analysis = new RepoAnalysisResult(
            HasDockerfile: true,
            HasCompose: true,
            HasKubernetes: false,
            HasGithubActions: false,
            DetectedPorts: new List<int> { 8080 },
            AppType: "aspnet",
            HasReadme: false,
            HasLaunchSettings: false,
            HasCsproj: true,
            TechnologyStack: new List<string> { "aspnet" }
        );

        // Act
        var result = _generator.GenerateDraft(service, analysis);

        // Assert - Verify Deployment markdown structure
        var deployment = result.First(s => s.Key == "deployment");

        deployment.BodyMarkdown.Should().Contain("# Deployment");
        deployment.BodyMarkdown.Should().Contain("## Using Docker Compose");
        deployment.BodyMarkdown.Should().Contain("```bash");
        deployment.BodyMarkdown.Should().Contain("docker compose pull");
        deployment.BodyMarkdown.Should().Contain("docker compose up -d");
        deployment.BodyMarkdown.Should().Contain("docker compose logs -f");
        deployment.BodyMarkdown.Should().Contain("```");
    }

    [Fact]
    public void GenerateDraft_VerifyOrder_AllSectionsInCorrectSequence()
    {
        // Arrange
        var service = CreateService("OrderTest", "order-test");
        var analysis = new RepoAnalysisResult(
            HasDockerfile: true,
            HasCompose: false,
            HasKubernetes: false,
            HasGithubActions: false,
            DetectedPorts: new List<int> { 3000 },
            AppType: "node",
            HasReadme: true,
            HasLaunchSettings: false,
            HasCsproj: false,
            TechnologyStack: new List<string> { "node" }
        );

        // Act
        var result = _generator.GenerateDraft(service, analysis);

        // Assert - Verify order
        result[0].Key.Should().Be("overview");
        result[0].Order.Should().Be(1);

        result[1].Key.Should().Be("deployment");
        result[1].Order.Should().Be(2);

        result[2].Key.Should().Be("rollback");
        result[2].Order.Should().Be(3);

        result[3].Key.Should().Be("health_checks");
        result[3].Order.Should().Be(4);

        result[4].Key.Should().Be("environment_variables");
        result[4].Order.Should().Be(5);

        result[5].Key.Should().Be("troubleshooting");
        result[5].Order.Should().Be(6);
    }

    [Fact]
    public void GenerateDraft_MixedTechStack_ShowsAllTechnologies()
    {
        // Arrange
        var service = CreateService("MixedApp", "mixed-app");
        var analysis = new RepoAnalysisResult(
            HasDockerfile: true,
            HasCompose: false,
            HasKubernetes: false,
            HasGithubActions: false,
            DetectedPorts: new List<int> { 3000, 5000 },
            AppType: "mixed",
            HasReadme: true,
            HasLaunchSettings: false,
            HasCsproj: false,
            TechnologyStack: new List<string> { "aspnet", "node", "python" }
        );

        // Act
        var result = _generator.GenerateDraft(service, analysis);

        // Assert
        var overview = result.First(s => s.Key == "overview");
        overview.BodyMarkdown.Should().Contain("ASP.NET Core");
        overview.BodyMarkdown.Should().Contain("Node.js");
        overview.BodyMarkdown.Should().Contain("Python");
    }

    [Fact]
    public void GenerateDraft_UnknownAppType_GeneratesGenericContent()
    {
        // Arrange
        var service = CreateService("UnknownApp", "unknown-app");
        var analysis = new RepoAnalysisResult(
            HasDockerfile: false,
            HasCompose: false,
            HasKubernetes: false,
            HasGithubActions: false,
            DetectedPorts: new List<int>(),
            AppType: "unknown",
            HasReadme: true,
            HasLaunchSettings: false,
            HasCsproj: false,
            TechnologyStack: new List<string>()
        );

        // Act
        var result = _generator.GenerateDraft(service, analysis);

        // Assert
        var deployment = result.First(s => s.Key == "deployment");
        deployment.BodyMarkdown.Should().Contain("TODO: Add deployment instructions");
        deployment.BodyMarkdown.Should().Contain("Follow README.md");
    }

    private static Service CreateService(string name, string slug, string? description = null)
    {
        return new Service
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            Description = description,
            UserId = Guid.NewGuid()
        };
    }
}
