using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.UI.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}
