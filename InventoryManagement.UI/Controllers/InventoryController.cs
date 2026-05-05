using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.UI.Controllers;

public class InventoryController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Create() => View();
    public IActionResult Details(int id = 1) => View();
    public IActionResult Access() => View();
    public IActionResult Fields() => View();
    public IActionResult CustomId() => View();
}
