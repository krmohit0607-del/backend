using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.API.Authorization;
using MultiTenantSaaS.Application.Common.Models;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/inventory")]
[Authorize]
public class InventoryController : BaseApiController
{
    /// <summary>
    /// View inventory items. Requires View permission on the Inventory module.
    /// </summary>
    [HttpGet]
    [HasModuleAccess("Inventory", ModulePermissionType.View)]
    [ProducesResponseType(typeof(ApiResponse<List<object>>), StatusCodes.Status200OK)]
    public IActionResult GetInventoryItems()
    {
        var sampleItems = new List<object>
        {
            new { Id = Guid.NewGuid(), Sku = "SKU-001", Name = "Industrial Sensor A1", Quantity = 150, UnitPrice = 89.99 },
            new { Id = Guid.NewGuid(), Sku = "SKU-002", Name = "Hydraulic Valve V4", Quantity = 45, UnitPrice = 240.50 }
        };

        return Ok(ApiResponse<List<object>>.SuccessResult(sampleItems, "Inventory items retrieved successfully."));
    }

    /// <summary>
    /// Create an inventory item. Requires Create permission on the Inventory module.
    /// </summary>
    [HttpPost]
    [HasModuleAccess("Inventory", ModulePermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    public IActionResult CreateInventoryItem([FromBody] dynamic item)
    {
        return StatusCode(StatusCodes.Status201Created, ApiResponse<object>.SuccessResult(item, "Inventory item created successfully."));
    }

    /// <summary>
    /// Update an inventory item. Requires Edit permission on the Inventory module.
    /// </summary>
    [HttpPut("{id}")]
    [HasModuleAccess("Inventory", ModulePermissionType.Edit)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult UpdateInventoryItem(Guid id, [FromBody] dynamic item)
    {
        return Ok(ApiResponse<object>.SuccessResult(item, $"Inventory item {id} updated successfully."));
    }

    /// <summary>
    /// Delete an inventory item. Requires Delete permission on the Inventory module.
    /// </summary>
    [HttpDelete("{id}")]
    [HasModuleAccess("Inventory", ModulePermissionType.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public IActionResult DeleteInventoryItem(Guid id)
    {
        return Ok(ApiResponse.SuccessResult($"Inventory item {id} deleted successfully."));
    }
}
