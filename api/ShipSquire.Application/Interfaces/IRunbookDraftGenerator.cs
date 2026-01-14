using ShipSquire.Application.DTOs;
using ShipSquire.Domain.Entities;

namespace ShipSquire.Application.Interfaces;

public interface IRunbookDraftGenerator
{
    /// <summary>
    /// Generates a draft runbook with populated sections based on repository analysis.
    /// </summary>
    /// <param name="service">The service to generate the runbook for</param>
    /// <param name="analysis">Repository analysis results</param>
    /// <returns>List of draft sections with pre-populated helpful content</returns>
    List<RunbookSectionDraft> GenerateDraft(Service service, RepoAnalysisResult analysis);
}
