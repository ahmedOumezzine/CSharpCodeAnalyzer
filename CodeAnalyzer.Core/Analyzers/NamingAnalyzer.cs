using Microsoft.CodeAnalysis.CSharp.Syntax;
using CodeAnalyzer.Core.Models;
using Microsoft.CodeAnalysis;

namespace CodeAnalyzer.Core.Analyzers;

public static class NamingAnalyzer
{
    public static void Analyze(AnalysisResult result, SyntaxNode root)
    {
        var category = new CategoryResult { Name = "Nommage" };

        // Méthodes
        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var name = method.Identifier.Text;
            category.Issues.Add(new RuleResult
            {
                RuleName = "Méthode en PascalCase",
                Description = $"La méthode '{name}' doit commencer par une majuscule.",
                Passed = char.IsUpper(name[0]),
                Suggestion = "Utilisez PascalCase : ex: GetData",
                CodeSnippet = name
            });

            // Paramètres
            foreach (var param in method.ParameterList.Parameters)
            {
                var paramName = param.Identifier.Text;
                category.Issues.Add(new RuleResult
                {
                    RuleName = "Paramètre en camelCase",
                    Description = $"Le paramètre '{paramName}' doit commencer par une minuscule.",
                    Passed = char.IsLower(paramName[0]),
                    Suggestion = "Utilisez camelCase : ex: userId",
                    CodeSnippet = paramName
                });
            }
        }

        // Classes
        foreach (var cls in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var className = cls.Identifier.Text;
            category.Issues.Add(new RuleResult
            {
                RuleName = "Classe en PascalCase",
                Description = $"La classe '{className}' doit commencer par une majuscule.",
                Passed = char.IsUpper(className[0]),
                Suggestion = "Utilisez PascalCase : ex: UserService",
                CodeSnippet = className
            });
        }

        result.Categories.Add(category);
    }
}