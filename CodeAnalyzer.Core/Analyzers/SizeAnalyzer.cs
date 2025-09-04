using CodeAnalyzer.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalyzer.Core.Analyzers;

public static class SizeAnalyzer
{
    private const int DefaultMaxLines = 50;
    private const int DefaultMaxParameters = 5;

    public static void Analyze(AnalysisResult result, SyntaxNode root, int maxLines = DefaultMaxLines, int maxParameters = DefaultMaxParameters)
    {
        var category = result.Categories.Find(c => c.Name == "Taille des méthodes")
            ?? new CategoryResult { Name = "Taille des méthodes" };

        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            int lines = GetMethodLines(method);
            int paramCount = method.ParameterList.Parameters.Count;

            if (lines > maxLines)
            {
                AddIssue(category, "Méthode trop longue",
                    $"La méthode '{method.Identifier.Text}' contient {lines} lignes (> {maxLines}).",
                    "Découpez cette méthode en plusieurs sous-méthodes.",
                    method.Identifier, lines, paramCount);
            }

            if (paramCount > maxParameters)
            {
                AddIssue(category, "Trop de paramètres",
                    $"La méthode '{method.Identifier.Text}' a {paramCount} paramètres (> {maxParameters}).",
                    "Utilisez un objet DTO ou record pour réduire le nombre de paramètres.",
                    method.Identifier, lines, paramCount);
            }
        }

        if (!result.Categories.Contains(category))
            result.Categories.Add(category);
    }

    private static int GetMethodLines(MethodDeclarationSyntax method)
    {
        var start = method.GetLocation().GetLineSpan().StartLinePosition.Line;
        var end = method.Body != null
            ? method.Body.GetLocation().GetLineSpan().EndLinePosition.Line
            : method.ExpressionBody?.GetLocation().GetLineSpan().EndLinePosition.Line ?? start;
        return end - start + 1;
    }

    private static void AddIssue(CategoryResult category, string ruleName, string description, string suggestion, SyntaxToken token, int lines, int parameters)
    {
        var location = token.GetLocation();
        var lineSpan = location.GetLineSpan();
        var line = lineSpan.StartLinePosition.Line + 1;
        var column = lineSpan.StartLinePosition.Character + 1;

        string prMessage =
$@"📏 **{ruleName}**
Méthode : `{token.Text}`
Lignes : {lines}, Paramètres : {parameters}
💡 Suggestion : {suggestion}
📌 Ligne {line}, Colonne {column}";

        category.Issues.Add(new RuleResult
        {
            RuleName = ruleName,
            Description = description,
            Suggestion = suggestion,
            Passed = false,
            CodeSnippet = token.Text,
            LineNumber = line,
            ColumnNumber = column,
            Category = "Size",
            PrMessage = prMessage
        });
    }
}