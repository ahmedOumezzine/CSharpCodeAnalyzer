using CodeAnalyzer.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Security.Cryptography;
using System.Text;

namespace CodeAnalyzer.Core.Analyzers;

public static class DuplicateCodeAnalyzer
{
    public static void Analyze(AnalysisResult result, SyntaxNode root)
    {
        var category = result.Categories.Find(c => c.Name == "Duplication")
            ?? new CategoryResult { Name = "Duplication" };

        var blocks = root.DescendantNodes().OfType<BlockSyntax>().ToList();
        var hashes = new Dictionary<string, List<BlockSyntax>>();

        foreach (var block in blocks)
        {
            string normalized = Normalize(block);
            string hash = ComputeHash(normalized);

            if (!hashes.ContainsKey(hash))
                hashes[hash] = new List<BlockSyntax>();

            hashes[hash].Add(block);
        }

        foreach (var kvp in hashes.Where(k => k.Value.Count > 1))
        {
            var duplicates = kvp.Value;
            string preview = duplicates.First().ToFullString().Trim();
            if (preview.Length > 200) preview = preview.Substring(0, 200) + "...";

            string prMessage =
            $@"📌 **Duplication de code détectée**
            Occurrences : {duplicates.Count}
            Extrait :
            ```csharp
            {preview}
            💡 Envisagez de factoriser ce code dans une méthode utilitaire.";
            foreach (var block in duplicates)
            {
                var location = block.GetLocation().GetLineSpan();
                category.Issues.Add(new RuleResult
                {
                    RuleName = "Code dupliqué",
                    Description = $"Bloc dupliqué trouvé ({duplicates.Count} occurrences)",
                    Passed = false,
                    Suggestion = "Refactorisez ce code commun en méthode partagée.",
                    CodeSnippet = preview,
                    LineNumber = location.StartLinePosition.Line + 1,
                    ColumnNumber = location.StartLinePosition.Character + 1,
                    Category = "DuplicateCode",
                    PrMessage = prMessage
                });
            }
        }

        if (!result.Categories.Contains(category))
            result.Categories.Add(category);
    }

    private static string Normalize(BlockSyntax block)
    {
        var text = block.ToFullString();

        // Supprimer commentaires et espaces
        var tree = CSharpSyntaxTree.ParseText(text);
        var root = tree.GetRoot();

        var withoutTrivia = root.NormalizeWhitespace().ToFullString();
        return withoutTrivia;
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}