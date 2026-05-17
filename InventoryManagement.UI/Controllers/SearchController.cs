using InventoryManagement.UI.Models;
using InventoryManagement.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.UI.Controllers;

public class SearchController : AppController
{
    private readonly IInventoryFacade _inventoryFacade;

    public SearchController(IInventoryFacade inventoryFacade)
    {
        _inventoryFacade = inventoryFacade;
    }

    [HttpGet]
    public async Task<IActionResult> Results(string q = "", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return View(new SearchResultsPageViewModel
            {
                Query = q
            });
        }

        var searchResult = await _inventoryFacade.GetGlobalSearchAsync(q, cancellationToken);
        if (!searchResult.IsSuccess || searchResult.Value is null)
        {
            return RedirectForFailure(searchResult, fallbackAction: "Index", fallbackController: "Home");
        }

        return View(new SearchResultsPageViewModel
        {
            Query = q,
            Inventories = searchResult.Value.Inventories,
            Items = searchResult.Value.Items,
            WasSearched = true
        });
    }
}
