// Models/AnalysisResult.cs
namespace CodeAnalyzer.Core.Models;

public class AnalysisResult
{
    public string FileName { get; set; } = "Unknown";
    public string FormattedCode { get; set; } = ""; // ← Nouveau
    public List<CategoryResult> Categories { get; set; } = new();
    public int TotalIssues => Categories.Sum(c => c.Issues.Count(r => !r.Passed));
}