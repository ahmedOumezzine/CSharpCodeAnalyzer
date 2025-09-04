using CodeAnalyzer.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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
                    $"La classe '{name}' n'utilise pas correctement PascalCase.",
                    $"Utilisez PascalCase : ex: {ToPascalCase(name)}", name, cls.Identifier);
            }
        }

        // === 2. Méthodes et leurs corps ===
        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var methodName = method.Identifier.Text;

            // --- Vérification du nom de la méthode ---
            if (!IsKeyword(methodName) && !IsPascalCase(methodName))
            {
                AddIssue(category, "Méthode en PascalCase",
                    $"La méthode '{methodName}' n'utilise pas correctement PascalCase.",
                    $"Utilisez PascalCase : ex: {ToPascalCase(methodName)}", methodName, method.Identifier);
            }

            // --- Vérification async ---
            if (method.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword)) && !methodName.EndsWith("Async"))
            {
                AddIssue(category, "Méthode async doit finir par Async",
                    $"La méthode '{methodName}' est async mais ne se termine pas par 'Async'.",
                    $"Renommez-la en '{methodName}Async'", methodName, method.Identifier);
            }

            // --- Analyse des paramètres ---
            var parameterNames = new HashSet<string>(method.ParameterList.Parameters.Select(p => p.Identifier.Text), StringComparer.OrdinalIgnoreCase);

            foreach (var param in method.ParameterList.Parameters)
            {
                var paramName = param.Identifier.Text;

                // Interdire `_` en début
                if (paramName.StartsWith("_"))
                {
                    AddIssue(category, "Paramètre ne doit pas commencer par _",
                        $"Le paramètre '{paramName}' ne doit pas commencer par un tiret bas.",
                        $"Utilisez '{ToCamelCase(paramName.TrimStart('_'))}'", paramName, param.Identifier);
                }

                // Vérifier camelCase
                if (!IsCamelCase(paramName))
                {
                    AddIssue(category, "Paramètre en camelCase",
                        $"Le paramètre '{paramName}' n'utilise pas correctement camelCase.",
                        $"Utilisez camelCase : ex: {ToCamelCase(paramName)}", paramName, param.Identifier);
                }
            }

            // --- Analyse des chaînes formatées dans le corps ---
            AnalyzeStringInterpolations(category, method, parameterNames);
        }

        // === 3. Champs ===
        foreach (var fieldDecl in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            var isPrivate = fieldDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword));
            foreach (var variable in fieldDecl.Declaration.Variables)
            {
                var fieldName = variable.Identifier.Text;

                // Interdire le préfixe 'p'
                if (fieldName.StartsWith("p", System.StringComparison.OrdinalIgnoreCase))
                {
                    AddIssue(category, "Interdire le préfixe 'p'",
                        $"Le champ '{fieldName}' utilise le préfixe 'p'.",
                        $"Utilisez '_{ToCamelCase(fieldName.Substring(1))}'", fieldName, variable.Identifier);
                }

                // Champs privés doivent commencer par _
                if (isPrivate && !fieldName.StartsWith("_"))
                {
                    AddIssue(category, "Champ privé doit commencer par _",
                        $"Le champ privé '{fieldName}' doit commencer par un tiret bas.",
                        $"Utilisez '_{ToCamelCase(fieldName)}'", fieldName, variable.Identifier);
                }

                // Si commence par _, le reste doit être en camelCase
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

    // === Analyse des chaînes interpolées ou formatées ===
    private static void AnalyzeStringInterpolations(CategoryResult category, MethodDeclarationSyntax method, HashSet<string> parameterNames)
    {
        var stringLiterals = method.DescendantNodes()
            .OfType<LiteralExpressionSyntax>()
            .Where(le => le.IsKind(SyntaxKind.StringLiteralExpression));

        var interpolatedStrings = method.DescendantNodes().OfType<InterpolatedStringExpressionSyntax>();

        // 1. Chaînes avec string.Format ou $"..."
        foreach (var literal in stringLiterals)
        {
            var text = literal.Token.Text;

            // Enlève les guillemets
            if (text.StartsWith("\"") && text.EndsWith("\""))
                text = text.Substring(1, text.Length - 2);

            // Trouve tous les {id} dans la chaîne
            var matches = Regex.Matches(text, @"\{([^}:,}]+)");
            foreach (Match match in matches)
            {
                var placeholder = match.Groups[1].Value;

                // Ignore les index comme {0}, {1}
                if (int.TryParse(placeholder, out _)) continue;

                if (parameterNames.Contains(placeholder, StringComparer.OrdinalIgnoreCase))
                {
                    // Trouve le vrai nom du paramètre (cas exact)
                    var actualParam = method.ParameterList.Parameters
                        .FirstOrDefault(p => p.Identifier.Text.Equals(placeholder, System.StringComparison.OrdinalIgnoreCase));

                    if (actualParam != null && !IsCamelCase(actualParam.Identifier.Text))
                    {
                        var suggested = ToCamelCase(actualParam.Identifier.Text);
                        AddIssue(category, "Paramètre dans chaîne doit être en camelCase",
                            $"Le paramètre '{actualParam.Identifier.Text}' référencé dans une chaîne devrait être en camelCase.",
                            $"Renommez-le en '{suggested}'", actualParam.Identifier.Text, actualParam.Identifier);
                    }
                }
            }
        }

        // 2. Chaînes interpolées : $"Bonjour {nom}"
        foreach (var interpolated in interpolatedStrings)
        {
            foreach (var hole in interpolated.Contents.OfType<InterpolationSyntax>())
            {
                if (hole.Expression is IdentifierNameSyntax identifier)
                {
                    var paramName = identifier.Identifier.Text;

                    if (parameterNames.Contains(paramName, StringComparer.OrdinalIgnoreCase))
                    {
                        var paramDecl = method.ParameterList.Parameters
                            .FirstOrDefault(p => p.Identifier.Text.Equals(paramName, System.StringComparison.OrdinalIgnoreCase));

                        if (paramDecl != null && !IsCamelCase(paramName))
                        {
                            var suggested = ToCamelCase(paramName);
                            AddIssue(category, "Paramètre interpolé doit être en camelCase",
                                $"Le paramètre '{paramName}' utilisé dans une chaîne interpolée n'est pas en camelCase.",
                                $"Renommez-le en '{suggested}'", paramName, identifier.Identifier);
                        }
                    }
                }
            }
        }
    }

    private static bool IsKeyword(string name)
    {
        return _keywords.Contains(name);
    }

    private static List<string> SplitIntoWords(string name)
    {
        var words = new List<string>();
        var currentWord = "";

        foreach (char c in name)
        {
            if (char.IsDigit(c))
            {
                currentWord += c;
            }
            else if (char.IsUpper(c))
            {
                if (!string.IsNullOrEmpty(currentWord))
                {
                    words.Add(currentWord);
                }
                currentWord = c.ToString();
            }
            else if (char.IsLower(c))
            {
                currentWord += c;
            }
        }

        if (!string.IsNullOrEmpty(currentWord))
        {
            words.Add(currentWord);
        }

        return words;
    }

    private static bool IsPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name) || !char.IsUpper(name[0])) return false;
        var words = SplitIntoWords(name);
        return words.All(word => word.Length == 0 || char.IsUpper(word[0]));
    }

    private static bool IsCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name) || !char.IsLower(name[0])) return false;
        var words = SplitIntoWords(name);
        if (words.Count == 0) return false;
        return words.Skip(1).All(word => word.Length == 0 || char.IsUpper(word[0]));
    }

    private static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var words = SplitIntoWords(name);
        return string.Concat(words.Select(w => w.Length > 0 ? char.ToUpper(w[0]) + w.Substring(1).ToLower() : w));
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var words = SplitIntoWords(name);
        if (words.Count == 0) return name;

        var first = char.ToLower(words[0][0]) + words[0].Substring(1).ToLower();
        var rest = string.Concat(words.Skip(1).Select(w => w.Length > 0 ? char.ToUpper(w[0]) + w.Substring(1).ToLower() : w));
        return first + rest;
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