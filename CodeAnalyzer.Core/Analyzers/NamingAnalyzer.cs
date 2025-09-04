using CodeAnalyzer.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace CodeAnalyzer.Core.Analyzers;

public static class NamingAnalyzer
{
    private static readonly HashSet<string> _keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "using", "class", "struct", "interface", "enum", "delegate", "event",
        "public", "private", "protected", "internal", "extern", "static",
        "virtual", "override", "abstract", "sealed", "async", "await",
        "void", "bool", "int", "string", "double", "float", "char", "long",
        "short", "byte", "decimal", "object", "var", "new", "this", "base",
        "if", "else", "for", "while", "do", "switch", "case", "default",
        "try", "catch", "finally", "throw", "return", "continue", "break",
        "lock", "fixed", "unsafe", "checked", "unchecked", "nameof", "typeof", "sizeof",
        "true", "false", "null",
        "Console", "Math", "File", "Directory", "DateTime", "List", "Task"
    };

    public static void Analyze(AnalysisResult result, SyntaxNode root)
    {
        var category = result.Categories.Find(c => c.Name == "Nommage")
            ?? new CategoryResult { Name = "Nommage", Issues = new List<RuleResult>() };

        // === 1. Classes ===
        foreach (var cls in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var name = cls.Identifier.Text;
            if (!IsKeyword(name) && !IsPascalCase(name))
            {
                AddIssue(category, "Classe en PascalCase",
                    $"La classe '{name}' doit commencer par une majuscule.",
                    "Utilisez PascalCase : ex: UserService", name, cls.Identifier);
            }
        }

        // === 2. Méthodes ===
        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var methodName = method.Identifier.Text;
            if (!IsKeyword(methodName))
            {
                if (!IsPascalCase(methodName))
                {
                    AddIssue(category, "Méthode en PascalCase",
                        $"La méthode '{methodName}' doit commencer par une majuscule.",
                        "Utilisez PascalCase : ex: GetData", methodName, method.Identifier);
                }

                if (method.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword)) && !methodName.EndsWith("Async"))
                {
                    AddIssue(category, "Méthode async doit finir par Async",
                        $"La méthode '{methodName}' est async mais ne se termine pas par 'Async'.",
                        $"Renommez-la en '{methodName}Async'", methodName, method.Identifier);
                }

                // === Paramètres ===
                foreach (var param in method.ParameterList.Parameters)
                {
                    var paramName = param.Identifier.Text;
                    if (!IsKeyword(paramName) && !IsPascalCase(paramName))
                    {
                        AddIssue(category, "Paramètre doit commencer par une majuscule",
                            $"Le paramètre '{paramName}' doit commencer par une majuscule.",
                            $"Utilisez PascalCase : ex: {ToPascalCase(paramName)}", paramName, param.Identifier);
                    }
                }
            }
        }

        // === 3. Champs ===
        foreach (var fieldDecl in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            var isPrivate = fieldDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword));
            foreach (var variable in fieldDecl.Declaration.Variables)
            {
                var fieldName = variable.Identifier.Text;

                if (fieldName.StartsWith("p", System.StringComparison.OrdinalIgnoreCase))
                {
                    AddIssue(category, "Interdire le préfixe 'p'",
                        $"Le champ '{fieldName}' utilise le préfixe 'p'.",
                        $"Utilisez '_{ToCamelCase(fieldName.Substring(1))}'", fieldName, variable.Identifier);
                }

                if (isPrivate && !fieldName.StartsWith("_"))
                {
                    AddIssue(category, "Champ privé doit commencer par _",
                        $"Le champ privé '{fieldName}' doit commencer par un tiret bas.",
                        $"Utilisez '_{ToCamelCase(fieldName)}'", fieldName, variable.Identifier);
                }

                if (fieldName.StartsWith("_") && fieldName.Length > 1)
                {
                    var after = fieldName.Substring(1);
                    if (!IsCamelCase(after))
                    {
                        AddIssue(category, "camelCase après _",
                            $"Le champ '{fieldName}' doit utiliser camelCase après le _.",
                            $"Utilisez '_{ToCamelCase(after)}'", fieldName, variable.Identifier);
                    }
                }
            }
        }

        if (!result.Categories.Contains(category))
            result.Categories.Add(category);
    }

    private static bool IsKeyword(string name)
    {
        return _keywords.Contains(name);
    }

    private static bool IsPascalCase(string name)
    {
        return !string.IsNullOrEmpty(name) && char.IsLetter(name[0]) && char.IsUpper(name[0]);
    }

    private static bool IsCamelCase(string name)
    {
        return !string.IsNullOrEmpty(name) && char.IsLetter(name[0]) && char.IsLower(name[0]);
    }

    private static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToUpper(name[0]) + name.Substring(1);
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLower(name[0]) + name.Substring(1);
    }

    private static void AddIssue(
        CategoryResult category,
        string ruleName,
        string description,
        string suggestion,
        string codeSnippet,
        SyntaxToken token)
    {
        var location = token.GetLocation();
        var lineSpan = location.GetLineSpan();
        var line = lineSpan.StartLinePosition.Line + 1;
        var column = lineSpan.StartLinePosition.Character + 1;

        category.Issues.Add(new RuleResult
        {
            RuleName = ruleName,
            Description = description,
            Suggestion = suggestion,
            CodeSnippet = codeSnippet,
            LineNumber = line,
            ColumnNumber = column,
            Passed = false
        });
    }
}