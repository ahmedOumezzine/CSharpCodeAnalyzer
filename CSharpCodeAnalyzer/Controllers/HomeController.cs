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

    public IActionResult Index(string result = null)
    {
        ViewBag.Result = TempData["AnalysisResult"] as AnalysisResult;
        return View();
    }

    [HttpPost]
    public IActionResult Analyze(string sourceCode)
    {
        if (string.IsNullOrWhiteSpace(sourceCode))
        {
            TempData["AnalysisResult"] = JsonSerializer.Serialize(new AnalysisResult());
            return RedirectToAction("Index");
        }

        var analyzer = new DefaultCodeAnalyzer();
        var result = analyzer.Analyze(sourceCode, "Fichier temporaire");

        // ✅ Sérialise l'objet en JSON avant de le stocker
        TempData["AnalysisResult"] = JsonSerializer.Serialize(result);

        return RedirectToAction("Index");
    }
}