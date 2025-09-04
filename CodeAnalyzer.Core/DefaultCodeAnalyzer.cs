using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using CodeAnalyzer.Core.Analyzers;
using CodeAnalyzer.Core.Models;
using System.Reflection;

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
            // Parser le code
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            // Créer une compilation pour permettre l'analyse sémantique
            var compilation = CSharpCompilation.Create(
                assemblyName: "TemporaryAssembly",
                syntaxTrees: new[] { tree },
                references: GetMetadataReferences(),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            // Appliquer les analyseurs
            NamingAnalyzer.Analyze(result, root);

            // Tu pourras ajouter d'autres analyseurs plus tard :
            // ComplexityAnalyzer.Analyze(result, root);
            // DocumentationAnalyzer.Analyze(result, root);

        }
        catch (Exception ex)
        {
            // En cas d'erreur de parsing
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
    /// Charge les références nécessaires pour la compilation
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
            MetadataReference.CreateFromFile(typeof(System.ComponentModel.INotifyPropertyChanged).Assembly.Location),
        };
    }
}