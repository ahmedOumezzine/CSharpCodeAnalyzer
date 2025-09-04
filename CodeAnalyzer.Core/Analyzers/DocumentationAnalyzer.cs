using CodeAnalyzer.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalyzer.Core.Analyzers
{
    public static class DocumentationAnalyzer
    {
        public static void Analyze(AnalysisResult result, SyntaxNode root)
        {
            var category = result.Categories.Find(c => c.Name == "Documentation")
                ?? new CategoryResult { Name = "Documentation" };

            int totalItems = 0;
            int documentedItems = 0;

            // === 1. Classes publiques ===
            foreach (var cls in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (!cls.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
                    continue;

                totalItems++;
                if (!HasXmlDocumentation(cls, out string issues))
                {
                    AddIssue(category, "Classe non documentée", issues, cls.Identifier);
                }
                else documentedItems++;
            }

            // === 2. Interfaces ===
            foreach (var iface in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>())
            {
                totalItems++;
                if (!HasXmlDocumentation(iface, out string issues))
                {
                    AddIssue(category, "Interface non documentée", issues, iface.Identifier);
                }
                else documentedItems++;
            }

            // === 3. Méthodes publiques ===
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                if (!method.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
                    continue;

                totalItems++;
                if (!HasXmlDocumentation(method, out string issues, method.ParameterList.Parameters))
                {
                    AddIssue(category, "Méthode non documentée", issues, method.Identifier);
                }
                else documentedItems++;
            }

            // === 4. Propriétés publiques ===
            foreach (var prop in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
            {
                if (!prop.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
                    continue;

                totalItems++;
                if (!HasXmlDocumentation(prop, out string issues))
                {
                    AddIssue(category, "Propriété non documentée", issues, prop.Identifier);
                }
                else documentedItems++;
            }

            // === Score global de documentation ===
            category.Issues.Add(new RuleResult
            {
                RuleName = "Score de documentation",
                Description = $"Documentation complète : {documentedItems}/{totalItems} items documentés.",
                Passed = totalItems == 0 || documentedItems == totalItems,
                Suggestion = "Augmentez la documentation pour les éléments manquants.",
                CodeSnippet = "",
                LineNumber = 0,
                ColumnNumber = 0,
                Category = "Documentation",
                PrMessage = $"📊 **Documentation Score** : {documentedItems}/{totalItems} items documentés"
            });

            if (!result.Categories.Contains(category))
                result.Categories.Add(category);
        }

        private static bool HasXmlDocumentation(SyntaxNode node, out string issues, SeparatedSyntaxList<ParameterSyntax>? parameters = null)
        {
            issues = "";
            var trivia = node.GetLeadingTrivia();
            var xmlComment = trivia.FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));
            if (xmlComment == default)
            {
                issues = "Documentation XML manquante.";
                return false;
            }

            string xmlText = xmlComment.ToFullString();

            // TODO/FIXME détecté
            if (xmlText.Contains("TODO") || xmlText.Contains("FIXME"))
                issues += " Contient TODO/FIXME.";

            // Vérification des paramètres
            if (parameters.HasValue)
            {
                foreach (var param in parameters.Value)
                {
                    if (!xmlText.Contains($"<param name=\"{param.Identifier.Text}\""))
                        issues += $" Paramètre '{param.Identifier.Text}' non documenté.";
                }
            }

            return string.IsNullOrEmpty(issues);
        }

        private static void AddIssue(CategoryResult category, string ruleName, string description, SyntaxToken token)
        {
            var location = token.GetLocation().GetLineSpan();
            var line = location.StartLinePosition.Line + 1;
            var column = location.StartLinePosition.Character + 1;

            string prMessage =
$@"📄 **{ruleName}**
{description}
💡 Suggestion : Ajoutez /// <summary>...</summary> et <param> pour chaque paramètre manquant
📌 Ligne {line}, Colonne {column}";

            category.Issues.Add(new RuleResult
            {
                RuleName = ruleName,
                Description = description,
                Suggestion = "Ajoutez la documentation manquante",
                Passed = false,
                CodeSnippet = token.Text,
                LineNumber = line,
                ColumnNumber = column,
                Category = " ",
                PrMessage = prMessage
            });
        }
    }
}