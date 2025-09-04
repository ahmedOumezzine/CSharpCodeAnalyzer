using CodeAnalyzer.Core;
using CodeAnalyzer.Core.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CodeAnalyzer.Web.Controllers;

public class HomeController : Controller
{
    private readonly ICodeAnalyzer _analyzer;

    public HomeController(ICodeAnalyzer analyzer)
    {
        _analyzer = analyzer;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Analyze(string sourceCode)
    {
        if (string.IsNullOrWhiteSpace(sourceCode))
        {
            ViewBag.Result = new AnalysisResult();
            return View("Index");
        }

        var analyzer = new DefaultCodeAnalyzer();
        var result = analyzer.Analyze(sourceCode, "Fichier temporaire");

        ViewBag.Result = result;
        ViewBag.AnalysisResultJson = JsonSerializer.Serialize(result);

        return View("Index");
    }
}