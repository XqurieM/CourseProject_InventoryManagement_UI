using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.UI.Controllers;

public class ProfileController : Controller
{
    public IActionResult Index() => View();
}
