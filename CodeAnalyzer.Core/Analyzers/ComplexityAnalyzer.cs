using CodeAnalyzer.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalyzer.Core.Analyzers;

public static class ComplexityAnalyzer
{
    private const int maxComplexity = 10;

    public static void Analyze(AnalysisResult result, SyntaxNode root)
    {
        var category = result.Categories.Find(c => c.Name == "Complexité")
            ?? new CategoryResult { Name = "Complexité" };

        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            if (method.Body == null && method.ExpressionBody == null) continue;

            int complexity = CalculateCyclomaticComplexity(method);

            string level = complexity switch
            {
                <= 5 => "🟢 Faible",
                <= maxComplexity => "🟡 Moyen",
                _ => "🔴 Élevé"
            };

            string suggestion = complexity > maxComplexity
                ? "💡 Découpez cette méthode en plusieurs sous-méthodes pour réduire la complexité."
                : "✅ Bon niveau de complexité.";

            string prMessage =
$@"📊 **Complexité cyclomatique**
Méthode : `{method.Identifier.Text}`
Complexité : **{complexity}** → {level}
{(complexity > maxComplexity ? "⚠️ Dépasse le seuil recommandé (" + maxComplexity + ")" : "✔️ Conforme")}";

            category.Issues.Add(new RuleResult
            {
                RuleName = "Complexité cyclomatique",
                Description = $"Méthode '{method.Identifier.Text}' : complexité = {complexity}",
                Passed = complexity <= maxComplexity,
                Suggestion = suggestion,
                CodeSnippet = method.Identifier.Text,
                Category = "Complexity",
                PrMessage = prMessage
            });
        }

        if (!result.Categories.Contains(category))
            result.Categories.Add(category);
    }

    private static int CalculateCyclomaticComplexity(MethodDeclarationSyntax method)
    {
        int complexity = 1; // base path

        var nodes = method.Body?.DescendantNodes() ?? method.ExpressionBody?.DescendantNodes() ?? Enumerable.Empty<SyntaxNode>();

        complexity += nodes.OfType<IfStatementSyntax>().Count();
        complexity += nodes.OfType<ForStatementSyntax>().Count();
        complexity += nodes.OfType<ForEachStatementSyntax>().Count();
        complexity += nodes.OfType<WhileStatementSyntax>().Count();
        complexity += nodes.OfType<DoStatementSyntax>().Count();
        complexity += nodes.OfType<SwitchStatementSyntax>().Sum(s => s.Sections.Count); // each case adds path
        complexity += nodes.OfType<CatchClauseSyntax>().Count();
        complexity += nodes.OfType<ConditionalExpressionSyntax>().Count();
        complexity += nodes.OfType<BinaryExpressionSyntax>()
            .Count(b => b.IsKind(SyntaxKind.LogicalAndExpression) || b.IsKind(SyntaxKind.LogicalOrExpression));

        return complexity;
    }
}