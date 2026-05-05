using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.UI.Controllers;

public class AccountController : Controller
{
    public IActionResult Login() => View();
    public IActionResult Register() => View();
}
