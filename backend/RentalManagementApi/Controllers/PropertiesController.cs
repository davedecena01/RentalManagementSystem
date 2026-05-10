using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManagementApi.Application.Services;
using RentalManagementApi.Common;
using RentalManagementApi.DTOs.Properties;

namespace RentalManagementApi.Controllers;

[ApiController]
[Route("api/properties")]
[Authorize(Policy = "LandlordPolicy")]
public class PropertiesController(PropertyService propertyService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var properties = await propertyService.GetAllAsync(landlordId.Value);
        return Ok(properties);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var property = await propertyService.GetByIdAsync(id, landlordId.Value);
        if (property is null) return NotFound(new ApiError("Property not found.", "PROPERTY_NOT_FOUND"));

        return Ok(property);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePropertyRequest request)
    {
        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        try
        {
            var property = await propertyService.CreateAsync(landlordId.Value, request);
            return CreatedAtAction(nameof(GetById), new { id = property.Id }, property);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiError(ex.Message, "INVALID_INPUT"));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePropertyRequest request)
    {
        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var property = await propertyService.UpdateAsync(id, landlordId.Value, request);
        if (property is null) return NotFound(new ApiError("Property not found.", "PROPERTY_NOT_FOUND"));

        return Ok(property);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var (deleted, error) = await propertyService.DeleteAsync(id, landlordId.Value);
        return error switch
        {
            "NOT_FOUND" => NotFound(new ApiError("Property not found.", "PROPERTY_NOT_FOUND")),
            "ACTIVE_LEASE" => Conflict(new ApiError("Cannot delete a property with an active lease.", "ACTIVE_LEASE")),
            _ => NoContent()
        };
    }

    // --- Inventory ---

    [HttpGet("{propertyId:guid}/inventory")]
    public async Task<IActionResult> GetInventory(Guid propertyId)
    {
        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var items = await propertyService.GetInventoryAsync(propertyId, landlordId.Value);
        return Ok(items);
    }

    [HttpPost("{propertyId:guid}/inventory")]
    public async Task<IActionResult> AddInventoryItem(Guid propertyId, [FromBody] CreateInventoryItemRequest request)
    {
        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var item = await propertyService.AddInventoryItemAsync(propertyId, landlordId.Value, request);
        if (item is null) return NotFound(new ApiError("Property not found.", "PROPERTY_NOT_FOUND"));

        return Ok(item);
    }

    [HttpPut("{propertyId:guid}/inventory/{itemId:guid}")]
    public async Task<IActionResult> UpdateInventoryItem(Guid propertyId, Guid itemId, [FromBody] UpdateInventoryItemRequest request)
    {
        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var item = await propertyService.UpdateInventoryItemAsync(propertyId, itemId, landlordId.Value, request);
        if (item is null) return NotFound(new ApiError("Inventory item not found.", "ITEM_NOT_FOUND"));

        return Ok(item);
    }

    [HttpDelete("{propertyId:guid}/inventory/{itemId:guid}")]
    public async Task<IActionResult> DeleteInventoryItem(Guid propertyId, Guid itemId)
    {
        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var deleted = await propertyService.DeleteInventoryItemAsync(propertyId, itemId, landlordId.Value);
        if (!deleted) return NotFound(new ApiError("Inventory item not found.", "ITEM_NOT_FOUND"));

        return NoContent();
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
