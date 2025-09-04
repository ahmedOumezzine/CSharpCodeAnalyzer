using CodeAnalyzer.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalyzer.Core.Analyzers;

public static class ExceptionAnalyzer
{
    public static void Analyze(AnalysisResult result, SyntaxNode root)
    {
        var category = result.Categories.Find(c => c.Name == "Exceptions")
            ?? new CategoryResult { Name = "Exceptions" };

        // === 1. Catch trop générique ===
        foreach (var catchClause in root.DescendantNodes().OfType<CatchClauseSyntax>())
        {
            var catchType = catchClause.Declaration?.Type?.ToString();
            bool isGenericCatch = catchType == null || catchType == "Exception";

            // Vérifier si le catch est vide ou ne fait qu'un commentaire
            bool isEmpty = !catchClause.Block.Statements.Any();

            if (isGenericCatch)
            {
                AddIssue(category, "Catch trop générique",
                    $"Bloc catch générique détecté (type: {catchType ?? "non spécifié"}).",
                    "Précisez une exception plus spécifique (ex: IOException, InvalidOperationException).",
                    catchClause.CatchKeyword);
            }

            if (isEmpty)
            {
                AddIssue(category, "Catch silencieux",
                    "Bloc catch vide → les exceptions sont avalées sans traitement.",
                    "Ajoutez un log, un rethrow ou supprimez ce bloc.",
                    catchClause.CatchKeyword);
            }
        }

        // === 2. Throw ex (mauvaise pratique) ===
        foreach (var throwStmt in root.DescendantNodes().OfType<ThrowStatementSyntax>())
        {
            if (throwStmt.Expression is IdentifierNameSyntax id && id.Identifier.Text == "ex")
            {
                AddIssue(category, "Utilisation de 'throw ex;'",
                    "Utiliser 'throw;' pour conserver la stack trace d'origine.",
                    "Remplacez par 'throw;'",
                    throwStmt.ThrowKeyword);
            }
        }

        // === 3. Appels bloquants dans async ===
        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            if (!method.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword)))
                continue;

            var blockingCalls = method.Body?.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(inv =>
                {
                    var txt = inv.ToString();
                    return txt.Contains("Thread.Sleep") || txt.Contains(".Wait()") || txt.Contains(".Result");
                })
                .ToList();

            if (blockingCalls != null && blockingCalls.Any())
            {
                foreach (var call in blockingCalls)
                {
                    AddIssue(category, "Appel bloquant dans async",
                        $"L'appel '{call}' bloque le thread dans une méthode async.",
                        "Utilisez await et les versions asynchrones des méthodes (ex: Task.Delay).",
                        call.GetFirstToken());
                }
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
        var location = token.GetLocation();
        var lineSpan = location.GetLineSpan();
        var line = lineSpan.StartLinePosition.Line + 1;
        var column = lineSpan.StartLinePosition.Character + 1;

        string prMessage =
        $@"🚩 **{ruleName}**
        {description}
        💡 Suggéré : {suggestion}
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
            Category = "Exception",
            PrMessage = prMessage
        });
    }
}