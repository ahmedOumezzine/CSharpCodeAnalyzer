using CodeAnalyzer.Core.Analyzers;
using CodeAnalyzer.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeAnalyzer.Core;

public class DefaultCodeAnalyzer : ICodeAnalyzer
{
    public AnalysisResult Analyze(string code, string fileName = "Unknown")
    {
        var result = new AnalysisResult { FileName = fileName };

        if (string.IsNullOrWhiteSpace(code))
            return result;

        try
        {
            // ✅ 1. Formater le code
            var formattedCode = FormatCode(code);
            result.FormattedCode = formattedCode; // ← Sauvegarde le code formaté

            // ✅ 2. Parser le code formaté
            var tree = CSharpSyntaxTree.ParseText(formattedCode);
            var root = tree.GetRoot();

            // ✅ 3. Créer la compilation
            var compilation = CSharpCompilation.Create(
                "TemporaryAssembly",
                new[] { tree },
                GetMetadataReferences(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            var model = compilation.GetSemanticModel(tree);

            // ✅ 4. Appliquer les analyseurs
            NamingAnalyzer.Analyze(result, root);
            ComplexityAnalyzer.Analyze(result, root);
            DocumentationAnalyzer.Analyze(result, root);
            DuplicateCodeAnalyzer.Analyze(result, root);
            ExceptionAnalyzer.Analyze(result, root);
            SizeAnalyzer.Analyze(result, root);
            UnusedCodeAnalyzer.Analyze(result, root);
        }
        catch (Exception ex)
        {
            result.Categories.Add(new CategoryResult
            {
                Name = "Erreur",
                Issues = {
                    new RuleResult
                    {
                        RuleName = "Analyse échouée",
                        Description = "Impossible d'analyser le code.",
                        Suggestion = ex.Message,
                        CodeSnippet = "Parsing error",
                        Passed = false
                    }
                }
            });
        }

        return result;
    }

    /// <summary>
    /// Reformate le code C# : supprime les espaces excessifs, mais préserve la lisibilité
    /// </summary>
    private static string FormatCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return code;

        var lines = code.Split('\n');
        var sb = new System.Text.StringBuilder();
        var indentLevel = 0;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Sauter les lignes vides
            if (string.IsNullOrEmpty(trimmed))
                continue;

            // Gérer l'indentation avant }
            if (trimmed == "}" || trimmed.StartsWith("}"))
            {
                indentLevel = Math.Max(0, indentLevel - 1);
            }

            // Ajouter la ligne avec indentation
            var indentedLine = new string(' ', indentLevel * 4) + trimmed;

            sb.AppendLine(indentedLine);

            // Augmenter l'indentation après {
            if (trimmed.EndsWith("{") || trimmed.Contains(" { "))
            {
                indentLevel++;
            }
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Références pour la compilation
    /// </summary>
    private static MetadataReference[] GetMetadataReferences()
    {
        return new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.IO.Stream).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
        };
    }
}