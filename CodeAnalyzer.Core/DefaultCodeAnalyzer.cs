using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using CodeAnalyzer.Core.Analyzers;
using CodeAnalyzer.Core.Models;

namespace CodeAnalyzer.Core;

public class DefaultCodeAnalyzer : ICodeAnalyzer
{
    public AnalysisResult Analyze(string code, string fileName = "Unknown")
    {
        var result = new AnalysisResult { FileName = fileName };
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        // Appliquer chaque analyseur
        NamingAnalyzer.Analyze(result, root);
        ComplexityAnalyzer.Analyze(result, root);
        DocumentationAnalyzer.Analyze(result, root);

        return result;
    }
}