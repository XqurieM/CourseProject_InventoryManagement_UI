using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.UI.Controllers;

public class SearchController : Controller
{
    public IActionResult Results() => View();
}
