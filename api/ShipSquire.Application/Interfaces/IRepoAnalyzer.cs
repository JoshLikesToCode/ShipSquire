using ShipSquire.Application.DTOs;

namespace ShipSquire.Application.Interfaces;

public interface IRepoAnalyzer
{
    /// <summary>
    /// Analyzes a GitHub repository without cloning it.
    /// Extracts metadata about the repository structure and technology stack.
    /// </summary>
    /// <param name="accessToken">Encrypted GitHub access token</param>
    /// <param name="owner">Repository owner (username or org)</param>
    /// <param name="repo">Repository name</param>
    /// <param name="branch">Optional branch name (defaults to repo's default branch)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analysis result containing detected files, ports, and app type</returns>
    Task<RepoAnalysisResult> AnalyzeRepositoryAsync(
        string accessToken,
        string owner,
        string repo,
        string? branch = null,
        CancellationToken cancellationToken = default);
}
