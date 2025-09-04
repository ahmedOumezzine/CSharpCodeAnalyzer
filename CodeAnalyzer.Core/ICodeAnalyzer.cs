namespace CodeAnalyzer.Core;

public interface ICodeAnalyzer
{
    Models.AnalysisResult Analyze(string code, string fileName = "Unknown");
}