// Models/RuleResult.cs
namespace CodeAnalyzer.Core.Models;

public class RuleResult
{
    public string RuleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string? Suggestion { get; set; }
    public string? CodeSnippet { get; set; }
    public int LineNumber { get; set; }
    public int ColumnNumber { get; set; }
    public string Category { get; set; } = string.Empty;

    public string? PrMessage { get; set; }
}