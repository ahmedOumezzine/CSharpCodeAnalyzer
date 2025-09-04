namespace CodeAnalyzer.Core.Models;

public class RuleResult
{
    public string RuleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string? Suggestion { get; set; }
    public string? CodeSnippet { get; set; }
}