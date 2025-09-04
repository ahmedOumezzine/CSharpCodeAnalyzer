using Microsoft.CodeAnalysis.CSharp.Syntax;
using CodeAnalyzer.Core.Models;
using Microsoft.CodeAnalysis;

namespace CodeAnalyzer.Core.Analyzers;

public static class ComplexityAnalyzer
{
    public static void Analyze(AnalysisResult result, SyntaxNode root)
    {
        var category = new CategoryResult { Name = "Complexité" };

        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var body = method.Body;
            if (body == null) continue;

            int complexity = 1;
            complexity += body.DescendantNodes().OfType<IfStatementSyntax>().Count();
            complexity += body.DescendantNodes().OfType<ForStatementSyntax>().Count();
            complexity += body.DescendantNodes().OfType<WhileStatementSyntax>().Count();
            complexity += body.DescendantNodes().OfType<ConditionalExpressionSyntax>().Count();
            complexity += body.DescendantNodes().OfType<BinaryExpressionSyntax>()
                .Count(b => b.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.LogicalAndExpression) ||
                           b.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.LogicalOrExpression));

            category.Issues.Add(new RuleResult
            {
                RuleName = "Complexité cyclomatique",
                Description = $"Méthode '{method.Identifier.Text}' : complexité = {complexity}",
                Passed = complexity <= 10,
                Suggestion = complexity > 10 ? "Découpez cette méthode." : "Bon niveau.",
                CodeSnippet = method.Identifier.Text
            });
        }

        result.Categories.Add(category);
    }
}