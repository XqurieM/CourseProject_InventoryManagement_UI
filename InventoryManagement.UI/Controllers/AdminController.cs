using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.UI.Controllers;

public class AdminController : Controller
{
    public IActionResult Users() => View();
}
