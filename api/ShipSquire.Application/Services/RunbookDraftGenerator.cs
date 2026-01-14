using System.Text;
using ShipSquire.Application.DTOs;
using ShipSquire.Application.Interfaces;
using ShipSquire.Domain.Entities;

namespace ShipSquire.Application.Services;

public class RunbookDraftGenerator : IRunbookDraftGenerator
{
    public List<RunbookSectionDraft> GenerateDraft(Service service, RepoAnalysisResult analysis)
    {
        var sections = new List<RunbookSectionDraft>();
        int order = 1;

        // Always generate Overview
        sections.Add(GenerateOverview(service, analysis, order++));

        // Generate Deployment section
        sections.Add(GenerateDeployment(service, analysis, order++));

        // Generate Rollback section
        sections.Add(GenerateRollback(service, analysis, order++));

        // Generate Health Checks section
        sections.Add(GenerateHealthChecks(service, analysis, order++));

        // Generate Environment Variables section
        sections.Add(GenerateEnvironmentVariables(service, analysis, order++));

        // Generate Troubleshooting section
        sections.Add(GenerateTroubleshooting(service, analysis, order++));

        return sections;
    }

    private RunbookSectionDraft GenerateOverview(Service service, RepoAnalysisResult analysis, int order)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# {service.Name}");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(service.Description))
        {
            sb.AppendLine(service.Description);
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(service.RepoUrl))
        {
            sb.AppendLine($"**Repository:** {service.RepoUrl}");
            sb.AppendLine();
        }

        sb.AppendLine("## Technology Stack");
        sb.AppendLine();

        if (analysis.TechnologyStack != null && analysis.TechnologyStack.Any())
        {
            foreach (var tech in analysis.TechnologyStack)
            {
                sb.AppendLine($"- {FormatTechnology(tech)}");
            }
        }
        else
        {
            sb.AppendLine($"- {FormatTechnology(analysis.AppType)}");
        }

        sb.AppendLine();

        sb.AppendLine("## Infrastructure");
        sb.AppendLine();
        var infra = new List<string>();
        if (analysis.HasDockerfile) infra.Add("Docker");
        if (analysis.HasCompose) infra.Add("Docker Compose");
        if (analysis.HasKubernetes) infra.Add("Kubernetes/Helm");
        if (analysis.HasGithubActions) infra.Add("GitHub Actions CI/CD");

        if (infra.Any())
        {
            foreach (var item in infra)
            {
                sb.AppendLine($"- {item}");
            }
        }
        else
        {
            sb.AppendLine("- Infrastructure configuration not detected");
        }

        return new RunbookSectionDraft("overview", "Overview", order, sb.ToString().TrimEnd());
    }

    private RunbookSectionDraft GenerateDeployment(Service service, RepoAnalysisResult analysis, int order)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Deployment");
        sb.AppendLine();

        if (analysis.HasCompose)
        {
            GenerateDockerComposeDeployment(sb, service, analysis);
        }
        else if (analysis.HasDockerfile)
        {
            GenerateDockerDeployment(sb, service, analysis);
        }
        else if (analysis.AppType == "aspnet" || analysis.HasCsproj)
        {
            GenerateAspNetDeployment(sb, service, analysis);
        }
        else if (analysis.AppType == "node")
        {
            GenerateNodeDeployment(sb, service, analysis);
        }
        else
        {
            GenerateGenericDeployment(sb, service, analysis);
        }

        if (analysis.HasKubernetes)
        {
            sb.AppendLine();
            sb.AppendLine("## Kubernetes Deployment");
            sb.AppendLine();
            sb.AppendLine("```bash");
            sb.AppendLine("# Apply Kubernetes manifests");
            sb.AppendLine("kubectl apply -f k8s/");
            sb.AppendLine();
            sb.AppendLine("# Or deploy with Helm");
            sb.AppendLine("helm upgrade --install " + GetServiceSlug(service) + " ./helm");
            sb.AppendLine("```");
        }

        return new RunbookSectionDraft("deployment", "Deployment", order, sb.ToString().TrimEnd());
    }

    private void GenerateDockerComposeDeployment(StringBuilder sb, Service service, RepoAnalysisResult analysis)
    {
        sb.AppendLine("## Using Docker Compose");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("# Pull latest images");
        sb.AppendLine("docker compose pull");
        sb.AppendLine();
        sb.AppendLine("# Start all services");
        sb.AppendLine("docker compose up -d");
        sb.AppendLine();
        sb.AppendLine("# View logs");
        sb.AppendLine("docker compose logs -f");
        sb.AppendLine();
        sb.AppendLine("# Check status");
        sb.AppendLine("docker compose ps");
        sb.AppendLine("```");

        if (analysis.DetectedPorts.Any())
        {
            sb.AppendLine();
            sb.AppendLine("The application will be available on:");
            foreach (var port in analysis.DetectedPorts.Take(3))
            {
                sb.AppendLine($"- http://localhost:{port}");
            }
        }
    }

    private void GenerateDockerDeployment(StringBuilder sb, Service service, RepoAnalysisResult analysis)
    {
        var imageName = GetServiceSlug(service);

        sb.AppendLine("## Using Docker");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("# Build the Docker image");
        sb.AppendLine($"docker build -t {imageName}:latest .");
        sb.AppendLine();
        sb.AppendLine("# Run the container");

        if (analysis.DetectedPorts.Any())
        {
            var primaryPort = analysis.DetectedPorts.First();
            sb.Append($"docker run -d -p {primaryPort}:{primaryPort}");
            if (analysis.AppType == "aspnet")
            {
                sb.Append(" -e ASPNETCORE_ENVIRONMENT=Production");
            }
            else if (analysis.AppType == "node")
            {
                sb.Append(" -e NODE_ENV=production");
            }
            sb.AppendLine($" --name {imageName} {imageName}:latest");
        }
        else
        {
            sb.AppendLine($"docker run -d --name {imageName} {imageName}:latest");
        }

        sb.AppendLine();
        sb.AppendLine("# View logs");
        sb.AppendLine($"docker logs -f {imageName}");
        sb.AppendLine();
        sb.AppendLine("# Stop the container");
        sb.AppendLine($"docker stop {imageName}");
        sb.AppendLine("```");

        if (analysis.DetectedPorts.Any())
        {
            sb.AppendLine();
            sb.AppendLine("The application will be available on:");
            foreach (var port in analysis.DetectedPorts.Take(3))
            {
                sb.AppendLine($"- http://localhost:{port}");
            }
        }
    }

    private void GenerateAspNetDeployment(StringBuilder sb, Service service, RepoAnalysisResult analysis)
    {
        sb.AppendLine("## ASP.NET Deployment");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("# Restore dependencies");
        sb.AppendLine("dotnet restore");
        sb.AppendLine();
        sb.AppendLine("# Build the application");
        sb.AppendLine("dotnet build -c Release");
        sb.AppendLine();
        sb.AppendLine("# Publish");
        sb.AppendLine("dotnet publish -c Release -o ./publish");
        sb.AppendLine();
        sb.AppendLine("# Run the published application");
        sb.AppendLine("cd publish");
        sb.AppendLine("dotnet " + service.Name + ".dll");
        sb.AppendLine("```");

        if (analysis.DetectedPorts.Any())
        {
            sb.AppendLine();
            sb.AppendLine("The application will listen on:");
            foreach (var port in analysis.DetectedPorts.Take(3))
            {
                sb.AppendLine($"- http://localhost:{port}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("### Production Deployment");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("# Set environment");
        sb.AppendLine("export ASPNETCORE_ENVIRONMENT=Production");
        sb.AppendLine();
        sb.AppendLine("# Run with systemd or supervisor");
        sb.AppendLine("# Configure reverse proxy (nginx/Apache) for HTTPS");
        sb.AppendLine("```");
    }

    private void GenerateNodeDeployment(StringBuilder sb, Service service, RepoAnalysisResult analysis)
    {
        sb.AppendLine("## Node.js Deployment");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("# Install dependencies");
        sb.AppendLine("npm install");
        sb.AppendLine();
        sb.AppendLine("# Build (if TypeScript or build step exists)");
        sb.AppendLine("npm run build");
        sb.AppendLine();
        sb.AppendLine("# Start the application");
        sb.AppendLine("npm start");
        sb.AppendLine();
        sb.AppendLine("# Or for production with PM2:");
        sb.AppendLine("pm2 start npm --name \"" + service.Name + "\" -- start");
        sb.AppendLine("```");

        if (analysis.DetectedPorts.Any())
        {
            sb.AppendLine();
            sb.AppendLine("The application will listen on:");
            foreach (var port in analysis.DetectedPorts.Take(3))
            {
                sb.AppendLine($"- http://localhost:{port}");
            }
        }
    }

    private void GenerateGenericDeployment(StringBuilder sb, Service service, RepoAnalysisResult analysis)
    {
        sb.AppendLine("## Deployment Instructions");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("# TODO: Add deployment instructions specific to this application");
        sb.AppendLine("# Clone the repository");
        sb.AppendLine($"git clone {service.RepoUrl ?? "[REPO_URL]"}");
        sb.AppendLine();
        sb.AppendLine("# Follow README.md for build and deployment steps");
        sb.AppendLine("```");
    }

    private RunbookSectionDraft GenerateRollback(Service service, RepoAnalysisResult analysis, int order)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Rollback");
        sb.AppendLine();

        if (analysis.HasCompose)
        {
            sb.AppendLine("## Docker Compose Rollback");
            sb.AppendLine();
            sb.AppendLine("```bash");
            sb.AppendLine("# Stop current deployment");
            sb.AppendLine("docker compose down");
            sb.AppendLine();
            sb.AppendLine("# Pull specific version");
            sb.AppendLine("docker compose pull");
            sb.AppendLine();
            sb.AppendLine("# Or manually specify image tags in docker-compose.yml");
            sb.AppendLine("# Then start with the previous version");
            sb.AppendLine("docker compose up -d");
            sb.AppendLine("```");
        }
        else if (analysis.HasDockerfile)
        {
            sb.AppendLine("## Docker Rollback");
            sb.AppendLine();
            sb.AppendLine("```bash");
            sb.AppendLine("# Stop and remove current container");
            sb.AppendLine($"docker stop {GetServiceSlug(service)}");
            sb.AppendLine($"docker rm {GetServiceSlug(service)}");
            sb.AppendLine();
            sb.AppendLine("# Run previous image version");
            sb.AppendLine($"docker run -d --name {GetServiceSlug(service)} {GetServiceSlug(service)}:{{PREVIOUS_TAG}}");
            sb.AppendLine();
            sb.AppendLine("# Or rebuild from previous commit");
            sb.AppendLine("git checkout {{PREVIOUS_COMMIT}}");
            sb.AppendLine($"docker build -t {GetServiceSlug(service)}:rollback .");
            sb.AppendLine($"docker run -d --name {GetServiceSlug(service)} {GetServiceSlug(service)}:rollback");
            sb.AppendLine("```");
        }
        else
        {
            sb.AppendLine("## Git Rollback");
            sb.AppendLine();
            sb.AppendLine("```bash");
            sb.AppendLine("# Revert to previous commit");
            sb.AppendLine("git log --oneline -10  # Find the commit to revert to");
            sb.AppendLine("git revert {{COMMIT_HASH}}");
            sb.AppendLine();
            sb.AppendLine("# Or hard reset (use with caution)");
            sb.AppendLine("git reset --hard {{PREVIOUS_COMMIT}}");
            sb.AppendLine();
            sb.AppendLine("# Redeploy the application");
            sb.AppendLine("# Follow deployment steps above");
            sb.AppendLine("```");
        }

        if (analysis.HasKubernetes)
        {
            sb.AppendLine();
            sb.AppendLine("## Kubernetes Rollback");
            sb.AppendLine();
            sb.AppendLine("```bash");
            sb.AppendLine("# View rollout history");
            sb.AppendLine($"kubectl rollout history deployment/{GetServiceSlug(service)}");
            sb.AppendLine();
            sb.AppendLine("# Rollback to previous revision");
            sb.AppendLine($"kubectl rollout undo deployment/{GetServiceSlug(service)}");
            sb.AppendLine();
            sb.AppendLine("# Or rollback to specific revision");
            sb.AppendLine($"kubectl rollout undo deployment/{GetServiceSlug(service)} --to-revision=2");
            sb.AppendLine("```");
        }

        return new RunbookSectionDraft("rollback", "Rollback", order, sb.ToString().TrimEnd());
    }

    private RunbookSectionDraft GenerateHealthChecks(Service service, RepoAnalysisResult analysis, int order)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Health Checks");
        sb.AppendLine();

        if (analysis.DetectedPorts.Any())
        {
            var primaryPort = analysis.DetectedPorts.First();

            if (analysis.AppType == "aspnet")
            {
                sb.AppendLine("## Application Health");
                sb.AppendLine();
                sb.AppendLine("ASP.NET applications typically expose health check endpoints:");
                sb.AppendLine();
                sb.AppendLine("```bash");
                sb.AppendLine($"# Check application health");
                sb.AppendLine($"curl http://localhost:{primaryPort}/health");
                sb.AppendLine();
                sb.AppendLine($"# Check readiness");
                sb.AppendLine($"curl http://localhost:{primaryPort}/health/ready");
                sb.AppendLine();
                sb.AppendLine($"# Check liveness");
                sb.AppendLine($"curl http://localhost:{primaryPort}/health/live");
                sb.AppendLine("```");
            }
            else if (analysis.AppType == "node")
            {
                sb.AppendLine("## Application Health");
                sb.AppendLine();
                sb.AppendLine("Check if the application is responding:");
                sb.AppendLine();
                sb.AppendLine("```bash");
                sb.AppendLine($"# Check application health");
                sb.AppendLine($"curl http://localhost:{primaryPort}/health");
                sb.AppendLine();
                sb.AppendLine($"# Or check the root endpoint");
                sb.AppendLine($"curl http://localhost:{primaryPort}/");
                sb.AppendLine("```");
            }
            else
            {
                sb.AppendLine("## Application Health");
                sb.AppendLine();
                sb.AppendLine("```bash");
                sb.AppendLine($"# Check if the application is responding");
                sb.AppendLine($"curl http://localhost:{primaryPort}/health");
                sb.AppendLine();
                sb.AppendLine($"# Or check the main endpoint");
                sb.AppendLine($"curl http://localhost:{primaryPort}/");
                sb.AppendLine("```");
            }

            sb.AppendLine();
            sb.AppendLine("## Expected Response");
            sb.AppendLine();
            sb.AppendLine("A healthy application should return:");
            sb.AppendLine("- HTTP 200 OK status");
            sb.AppendLine("- Response within 5 seconds");
            if (analysis.AppType == "aspnet")
            {
                sb.AppendLine("- JSON response with `status: \"Healthy\"`");
            }
        }
        else
        {
            sb.AppendLine("## Health Check Configuration");
            sb.AppendLine();
            sb.AppendLine("```bash");
            sb.AppendLine("# Check if the application is running");
            sb.AppendLine("ps aux | grep " + GetServiceSlug(service));
            sb.AppendLine();
            sb.AppendLine("# Check application logs");
            sb.AppendLine("tail -f /var/log/" + GetServiceSlug(service) + ".log");
            sb.AppendLine("```");
        }

        if (analysis.HasDockerfile || analysis.HasCompose)
        {
            sb.AppendLine();
            sb.AppendLine("## Container Health");
            sb.AppendLine();
            sb.AppendLine("```bash");
            if (analysis.HasCompose)
            {
                sb.AppendLine("# Check container status");
                sb.AppendLine("docker compose ps");
                sb.AppendLine();
                sb.AppendLine("# View container logs");
                sb.AppendLine("docker compose logs -f");
            }
            else
            {
                sb.AppendLine("# Check container status");
                sb.AppendLine($"docker ps | grep {GetServiceSlug(service)}");
                sb.AppendLine();
                sb.AppendLine("# View container logs");
                sb.AppendLine($"docker logs -f {GetServiceSlug(service)}");
            }
            sb.AppendLine("```");
        }

        return new RunbookSectionDraft("health_checks", "Health Checks", order, sb.ToString().TrimEnd());
    }

    private RunbookSectionDraft GenerateEnvironmentVariables(Service service, RepoAnalysisResult analysis, int order)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Environment Variables");
        sb.AppendLine();

        if (analysis.AppType == "aspnet")
        {
            sb.AppendLine("## ASP.NET Configuration");
            sb.AppendLine();
            sb.AppendLine("| Variable | Description | Example |");
            sb.AppendLine("|----------|-------------|---------|");
            sb.AppendLine("| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Development`, `Staging`, `Production` |");
            sb.AppendLine("| `ASPNETCORE_URLS` | URLs to listen on | `http://+:5000` |");
            sb.AppendLine("| `ConnectionStrings__DefaultConnection` | Database connection string | `Host=localhost;Database=mydb;Username=user;Password=pass` |");
            sb.AppendLine("| `Logging__LogLevel__Default` | Log level | `Information`, `Warning`, `Error` |");
        }
        else if (analysis.AppType == "node")
        {
            sb.AppendLine("## Node.js Configuration");
            sb.AppendLine();
            sb.AppendLine("| Variable | Description | Example |");
            sb.AppendLine("|----------|-------------|---------|");
            sb.AppendLine("| `NODE_ENV` | Runtime environment | `development`, `production` |");
            sb.AppendLine("| `PORT` | Port to listen on | `3000` |");
            sb.AppendLine("| `DATABASE_URL` | Database connection string | `postgres://user:pass@localhost:5432/mydb` |");
            sb.AppendLine("| `LOG_LEVEL` | Logging level | `info`, `debug`, `error` |");
        }
        else
        {
            sb.AppendLine("## Application Configuration");
            sb.AppendLine();
            sb.AppendLine("| Variable | Description | Example |");
            sb.AppendLine("|----------|-------------|---------|");
            sb.AppendLine("| `APP_ENV` | Runtime environment | `development`, `production` |");
            sb.AppendLine("| `LOG_LEVEL` | Logging level | `info`, `debug`, `error` |");
            sb.AppendLine("| `DATABASE_URL` | Database connection string | `[CONNECTION_STRING]` |");
        }

        sb.AppendLine();
        sb.AppendLine("## Setting Environment Variables");
        sb.AppendLine();

        if (analysis.HasCompose)
        {
            sb.AppendLine("### Docker Compose");
            sb.AppendLine();
            sb.AppendLine("Add to `.env` file or `docker-compose.yml`:");
            sb.AppendLine();
            sb.AppendLine("```yaml");
            sb.AppendLine("services:");
            sb.AppendLine("  app:");
            sb.AppendLine("    environment:");
            if (analysis.AppType == "aspnet")
            {
                sb.AppendLine("      - ASPNETCORE_ENVIRONMENT=Production");
            }
            else if (analysis.AppType == "node")
            {
                sb.AppendLine("      - NODE_ENV=production");
            }
            sb.AppendLine("      - DATABASE_URL=${DATABASE_URL}");
            sb.AppendLine("```");
        }
        else if (analysis.HasDockerfile)
        {
            sb.AppendLine("### Docker");
            sb.AppendLine();
            sb.AppendLine("```bash");
            sb.AppendLine("docker run -e VAR_NAME=value -e VAR2=value2 ...");
            sb.AppendLine("```");
        }
        else
        {
            sb.AppendLine("### Shell");
            sb.AppendLine();
            sb.AppendLine("```bash");
            sb.AppendLine("export VAR_NAME=value");
            sb.AppendLine("export VAR2=value2");
            sb.AppendLine("```");
        }

        if (analysis.HasKubernetes)
        {
            sb.AppendLine();
            sb.AppendLine("### Kubernetes");
            sb.AppendLine();
            sb.AppendLine("Create a ConfigMap or Secret:");
            sb.AppendLine();
            sb.AppendLine("```bash");
            sb.AppendLine($"kubectl create configmap {GetServiceSlug(service)}-config \\");
            if (analysis.AppType == "aspnet")
            {
                sb.AppendLine("  --from-literal=ASPNETCORE_ENVIRONMENT=Production");
            }
            else if (analysis.AppType == "node")
            {
                sb.AppendLine("  --from-literal=NODE_ENV=production");
            }
            sb.AppendLine("```");
        }

        return new RunbookSectionDraft("environment_variables", "Environment Variables", order, sb.ToString().TrimEnd());
    }

    private RunbookSectionDraft GenerateTroubleshooting(Service service, RepoAnalysisResult analysis, int order)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Troubleshooting");
        sb.AppendLine();

        sb.AppendLine("## Common Issues");
        sb.AppendLine();

        if (analysis.HasDockerfile || analysis.HasCompose)
        {
            sb.AppendLine("### Container Won't Start");
            sb.AppendLine();
            sb.AppendLine("```bash");
            sb.AppendLine("# Check container logs");
            if (analysis.HasCompose)
            {
                sb.AppendLine("docker compose logs");
            }
            else
            {
                sb.AppendLine($"docker logs {GetServiceSlug(service)}");
            }
            sb.AppendLine();
            sb.AppendLine("# Check container status");
            if (analysis.HasCompose)
            {
                sb.AppendLine("docker compose ps");
            }
            else
            {
                sb.AppendLine($"docker ps -a | grep {GetServiceSlug(service)}");
            }
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("**Possible causes:**");
            sb.AppendLine("- Missing environment variables");
            sb.AppendLine("- Port already in use");
            sb.AppendLine("- Insufficient permissions");
            sb.AppendLine("- Missing dependencies");
            sb.AppendLine();
        }

        if (analysis.DetectedPorts.Any())
        {
            sb.AppendLine("### Port Already in Use");
            sb.AppendLine();
            sb.AppendLine("```bash");
            var primaryPort = analysis.DetectedPorts.First();
            sb.AppendLine($"# Check what's using port {primaryPort}");
            sb.AppendLine($"lsof -i :{primaryPort}");
            sb.AppendLine("# Or");
            sb.AppendLine($"netstat -tulpn | grep {primaryPort}");
            sb.AppendLine();
            sb.AppendLine("# Kill the process if needed");
            sb.AppendLine("kill -9 <PID>");
            sb.AppendLine("```");
            sb.AppendLine();
        }

        if (analysis.AppType == "aspnet")
        {
            sb.AppendLine("### Application Crashes on Startup");
            sb.AppendLine();
            sb.AppendLine("```bash");
            sb.AppendLine("# Run with detailed logging");
            sb.AppendLine("export ASPNETCORE_ENVIRONMENT=Development");
            sb.AppendLine("export Logging__LogLevel__Default=Debug");
            sb.AppendLine("dotnet run");
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("**Common ASP.NET issues:**");
            sb.AppendLine("- Missing database connection string");
            sb.AppendLine("- Database not accessible");
            sb.AppendLine("- Missing configuration values");
            sb.AppendLine("- Certificate errors (HTTPS)");
            sb.AppendLine();
        }
        else if (analysis.AppType == "node")
        {
            sb.AppendLine("### Application Crashes on Startup");
            sb.AppendLine();
            sb.AppendLine("```bash");
            sb.AppendLine("# Run with debug output");
            sb.AppendLine("NODE_ENV=development npm start");
            sb.AppendLine();
            sb.AppendLine("# Check for missing dependencies");
            sb.AppendLine("npm install");
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("**Common Node.js issues:**");
            sb.AppendLine("- Missing dependencies (run `npm install`)");
            sb.AppendLine("- Wrong Node.js version");
            sb.AppendLine("- Missing environment variables");
            sb.AppendLine("- Port conflicts");
            sb.AppendLine();
        }

        sb.AppendLine("### Connectivity Issues");
        sb.AppendLine();
        sb.AppendLine("```bash");
        if (analysis.DetectedPorts.Any())
        {
            var primaryPort = analysis.DetectedPorts.First();
            sb.AppendLine($"# Test local connectivity");
            sb.AppendLine($"curl -v http://localhost:{primaryPort}/health");
        }
        else
        {
            sb.AppendLine("# Check network connectivity");
            sb.AppendLine("ping [hostname]");
        }
        sb.AppendLine();
        sb.AppendLine("# Check firewall rules");
        sb.AppendLine("sudo iptables -L");
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("## Getting Help");
        sb.AppendLine();
        sb.AppendLine("1. Check application logs");
        if (analysis.HasReadme)
        {
            sb.AppendLine("2. Review the README.md for known issues");
        }
        if (analysis.HasGithubActions)
        {
            sb.AppendLine("3. Check CI/CD pipeline for recent failures");
        }
        sb.AppendLine("4. Contact the development team");

        return new RunbookSectionDraft("troubleshooting", "Troubleshooting", order, sb.ToString().TrimEnd());
    }

    private static string FormatTechnology(string tech)
    {
        return tech switch
        {
            "aspnet" => "ASP.NET Core",
            "node" => "Node.js",
            "python" => "Python",
            "go" => "Go",
            "ruby" => "Ruby",
            "mixed" => "Multiple Technologies",
            "unknown" => "Unknown",
            _ => tech
        };
    }

    private static string GetServiceSlug(Service service)
    {
        return !string.IsNullOrEmpty(service.Slug) ? service.Slug : service.Name.ToLowerInvariant().Replace(" ", "-");
    }
}
