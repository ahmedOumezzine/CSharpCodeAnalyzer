using Microsoft.CodeAnalysis.CSharp.Syntax;
using CodeAnalyzer.Core.Models;
using Microsoft.CodeAnalysis;

namespace CodeAnalyzer.Core.Analyzers;

public static class DocumentationAnalyzer
{
    public static void Analyze(AnalysisResult result, SyntaxNode root)
    {
        var category = new CategoryResult { Name = "Documentation" };

        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var hasDocs = method.HasLeadingTrivia &&
                          method.GetLeadingTrivia().Any(t => t.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SingleLineDocumentationCommentTrivia));

            category.Issues.Add(new RuleResult
            {
                RuleName = "Documentation XML",
                Description = $"La méthode '{method.Identifier.Text}' devrait être documentée.",
                Passed = hasDocs,
                Suggestion = "Ajoutez /// <summary>...</summary>",
                CodeSnippet = method.Identifier.Text
            });
        }

        result.Categories.Add(category);
    }
}