using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.UI.Controllers;

public class ItemController : Controller
{
    public IActionResult Create(int inventoryId = 1) => View();
    public IActionResult Edit(int id = 1) => View();
    public IActionResult Details(int id = 1) => View();
}
