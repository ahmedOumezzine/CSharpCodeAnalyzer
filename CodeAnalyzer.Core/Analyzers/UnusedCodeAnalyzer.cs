using CodeAnalyzer.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalyzer.Core.Analyzers;

public static class UnusedCodeAnalyzer
{
    public static void Analyze(AnalysisResult result, SyntaxNode root)
    {
        var category = result.Categories.Find(c => c.Name == "Code inutilisé")
            ?? new CategoryResult { Name = "Code inutilisé" };

        // === 1. Variables locales ===
        foreach (var local in root.DescendantNodes().OfType<LocalDeclarationStatementSyntax>())
        {
            foreach (var variable in local.Declaration.Variables)
            {
                var name = variable.Identifier.Text;
                bool isUsed = root.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Any(id => id.Identifier.Text == name && id.Identifier.SpanStart != variable.Identifier.SpanStart);

                if (!isUsed)
                {
                    AddIssue(category, "Variable locale inutilisée",
                        $"La variable '{name}' n'est jamais utilisée.",
                        "Supprimez cette variable ou utilisez-la.",
                        variable.Identifier);
                }
            }
        }

        // === 2. Paramètres de méthode ===
        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            foreach (var param in method.ParameterList.Parameters)
            {
                var name = param.Identifier.Text;
                bool isUsed = method.Body?.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Any(id => id.Identifier.Text == name) ?? false;

                if (!isUsed)
                {
                    AddIssue(category, "Paramètre inutilisé",
                        $"Le paramètre '{name}' de la méthode '{method.Identifier.Text}' n'est jamais utilisé.",
                        "Supprimez ce paramètre ou utilisez-le.",
                        param.Identifier);
                }
            }
        }

        // === 3. Champs privés ===
        foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            bool isPrivate = field.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword));
            if (!isPrivate) continue;

            foreach (var variable in field.Declaration.Variables)
            {
                var name = variable.Identifier.Text;
                bool isUsed = root.DescendantNodes()
               .OfType<IdentifierNameSyntax>()
               .Any(id => id.Identifier.Text == name && id.Identifier.SpanStart != variable.Identifier.SpanStart);

                if (!isUsed)
                {
                    AddIssue(category, "Champ privé inutilisé",
                        $"Le champ privé '{name}' n'est jamais utilisé.",
                        "Supprimez ce champ ou utilisez-le.",
                        variable.Identifier);
                }
            }
        }

        // === 4. Méthodes privées ===
        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            bool isPrivate = method.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword));
            if (!isPrivate) continue;

            var name = method.Identifier.Text;
            bool isUsed = root.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Any(inv => inv.Expression is IdentifierNameSyntax id && id.Identifier.Text == name);

            if (!isUsed)
            {
                AddIssue(category, "Méthode privée inutilisée",
                    $"La méthode privée '{name}' n'est jamais utilisée.",
                    "Supprimez cette méthode ou utilisez-la.",
                    method.Identifier);
            }
        }

        if (!result.Categories.Contains(category))
            result.Categories.Add(category);
    }

    private static void AddIssue(
        CategoryResult category,
        string ruleName,
        string description,
        string suggestion,
        SyntaxToken token)
    {
        var location = token.GetLocation().GetLineSpan();
        var line = location.StartLinePosition.Line + 1;
        var column = location.StartLinePosition.Character + 1;

        string prMessage =
$@"⚠️ **{ruleName}**
{description}
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
            Category = "UnusedCode",
            PrMessage = prMessage
        });
    }
}