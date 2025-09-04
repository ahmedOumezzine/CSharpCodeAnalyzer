namespace CodeAnalyzer.Core.Models;

public class CategoryResult
{
    public string Name { get; set; } = string.Empty;
    public List<RuleResult> Issues { get; set; } = new();
}