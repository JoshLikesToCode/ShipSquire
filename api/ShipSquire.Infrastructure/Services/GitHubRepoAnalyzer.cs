using System.Text;
using System.Text.RegularExpressions;
using ShipSquire.Application.DTOs;
using ShipSquire.Application.Interfaces;

namespace ShipSquire.Infrastructure.Services;

public class GitHubRepoAnalyzer : IRepoAnalyzer
{
    private readonly IGitHubApiClient _githubClient;

    public GitHubRepoAnalyzer(IGitHubApiClient githubClient)
    {
        _githubClient = githubClient;
    }

    public async Task<RepoAnalysisResult> AnalyzeRepositoryAsync(
        string accessToken,
        string owner,
        string repo,
        string? branch = null,
        CancellationToken cancellationToken = default)
    {
        // Fetch repository tree
        var tree = await _githubClient.GetRepositoryTreeAsync(accessToken, owner, repo, branch, cancellationToken);

        // Initialize detection flags
        bool hasDockerfile = false;
        bool hasCompose = false;
        bool hasKubernetes = false;
        bool hasGithubActions = false;
        bool hasReadme = false;
        bool hasLaunchSettings = false;
        bool hasCsproj = false;

        List<string> dockerfilePaths = new();
        List<string> launchSettingsPaths = new();
        HashSet<string> detectedTechnologies = new();

        // Analyze tree
        foreach (var item in tree.tree)
        {
            if (item.type != "blob") continue;

            var path = item.path.ToLowerInvariant();
            var fileName = Path.GetFileName(path);

            // Detect specific files
            if (fileName == "dockerfile" || path.EndsWith("/dockerfile"))
            {
                hasDockerfile = true;
                dockerfilePaths.Add(item.path);
            }
            else if (fileName == "docker-compose.yml" || fileName == "docker-compose.yaml")
            {
                hasCompose = true;
            }
            else if (path.StartsWith(".github/workflows/") && (path.EndsWith(".yml") || path.EndsWith(".yaml")))
            {
                hasGithubActions = true;
            }
            else if ((path.Contains("/k8s/") || path.StartsWith("k8s/")) && (path.EndsWith(".yaml") || path.EndsWith(".yml")))
            {
                hasKubernetes = true;
            }
            else if (path.Contains("/helm/") || path.StartsWith("helm/"))
            {
                hasKubernetes = true;
            }
            else if (fileName == "readme.md")
            {
                hasReadme = true;
            }
            else if (fileName == "launchsettings.json")
            {
                hasLaunchSettings = true;
                launchSettingsPaths.Add(item.path);
            }
            else if (path.EndsWith(".csproj"))
            {
                hasCsproj = true;
            }

            // Detect technologies by file extensions
            if (path.EndsWith(".csproj") || path.EndsWith(".cs"))
            {
                detectedTechnologies.Add("aspnet");
            }
            else if (path.EndsWith(".js") || path.EndsWith(".ts") || fileName == "package.json")
            {
                detectedTechnologies.Add("node");
            }
            else if (path.EndsWith(".py"))
            {
                detectedTechnologies.Add("python");
            }
            else if (path.EndsWith(".go"))
            {
                detectedTechnologies.Add("go");
            }
            else if (path.EndsWith(".rb"))
            {
                detectedTechnologies.Add("ruby");
            }
        }

        // Extract ports from Dockerfile and launchSettings.json
        var detectedPorts = new HashSet<int>();

        // Parse Dockerfiles for EXPOSE directives
        foreach (var dockerfilePath in dockerfilePaths.Take(3)) // Limit to first 3 to avoid excessive API calls
        {
            try
            {
                var dockerfile = await _githubClient.GetFileContentAsync(accessToken, owner, repo, dockerfilePath, cancellationToken);
                var content = DecodeBase64Content(dockerfile.content);
                var ports = ExtractPortsFromDockerfile(content);
                foreach (var port in ports)
                {
                    detectedPorts.Add(port);
                }
            }
            catch
            {
                // Ignore errors fetching individual files
            }
        }

        // Parse launchSettings.json for applicationUrl ports
        foreach (var launchSettingsPath in launchSettingsPaths.Take(3))
        {
            try
            {
                var launchSettings = await _githubClient.GetFileContentAsync(accessToken, owner, repo, launchSettingsPath, cancellationToken);
                var content = DecodeBase64Content(launchSettings.content);
                var ports = ExtractPortsFromLaunchSettings(content);
                foreach (var port in ports)
                {
                    detectedPorts.Add(port);
                }
            }
            catch
            {
                // Ignore errors fetching individual files
            }
        }

        // Determine app type
        string appType = DetermineAppType(detectedTechnologies);

        return new RepoAnalysisResult(
            HasDockerfile: hasDockerfile,
            HasCompose: hasCompose,
            HasKubernetes: hasKubernetes,
            HasGithubActions: hasGithubActions,
            DetectedPorts: detectedPorts.OrderBy(p => p).ToList(),
            AppType: appType,
            HasReadme: hasReadme,
            HasLaunchSettings: hasLaunchSettings,
            HasCsproj: hasCsproj,
            TechnologyStack: detectedTechnologies.OrderBy(t => t).ToList()
        );
    }

    private static string DecodeBase64Content(string? base64Content)
    {
        if (string.IsNullOrEmpty(base64Content))
        {
            return string.Empty;
        }

        try
        {
            var bytes = Convert.FromBase64String(base64Content);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static List<int> ExtractPortsFromDockerfile(string content)
    {
        var ports = new List<int>();

        // Match EXPOSE directives: EXPOSE 80 or EXPOSE 80 443 or EXPOSE 80/tcp
        var exposeRegex = new Regex(@"EXPOSE\s+(\d+(?:/tcp|/udp)?(?:\s+\d+(?:/tcp|/udp)?)*)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        var matches = exposeRegex.Matches(content);

        foreach (Match match in matches)
        {
            var portString = match.Groups[1].Value;
            // Extract individual port numbers
            var portNumbers = Regex.Matches(portString, @"\d+");
            foreach (Match portMatch in portNumbers)
            {
                if (int.TryParse(portMatch.Value, out int port))
                {
                    ports.Add(port);
                }
            }
        }

        return ports;
    }

    private static List<int> ExtractPortsFromLaunchSettings(string content)
    {
        var ports = new List<int>();

        // Match applicationUrl with ports like "http://localhost:5000" or "https://localhost:5001"
        var urlRegex = new Regex(@"https?://[^:]+:(\d+)", RegexOptions.IgnoreCase);
        var matches = urlRegex.Matches(content);

        foreach (Match match in matches)
        {
            if (int.TryParse(match.Groups[1].Value, out int port))
            {
                ports.Add(port);
            }
        }

        return ports;
    }

    private static string DetermineAppType(HashSet<string> technologies)
    {
        if (technologies.Count == 0)
        {
            return "unknown";
        }

        if (technologies.Count > 1)
        {
            return "mixed";
        }

        // Return the single detected technology
        return technologies.First();
    }
}
