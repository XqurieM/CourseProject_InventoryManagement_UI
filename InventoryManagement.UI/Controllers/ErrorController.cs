using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.UI.Controllers;

public class ErrorController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Unauthorized() => View();
    public IActionResult Forbidden() => View();
}
