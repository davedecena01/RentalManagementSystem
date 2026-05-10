using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManagementApi.Application.Services;
using RentalManagementApi.Common;
using RentalManagementApi.DTOs.Provisions;

namespace RentalManagementApi.Controllers;

[ApiController]
[Route("api/provision-templates")]
[Authorize(Policy = "LandlordPolicy")]
public class ProvisionTemplatesController(ProvisionService provisionService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var templates = await provisionService.GetTemplatesAsync(landlordId.Value);
        return Ok(templates);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProvisionTemplateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
            return BadRequest(new ApiError("Title and body are required.", "INVALID_INPUT"));

        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var template = await provisionService.CreateTemplateAsync(landlordId.Value, request);
        return Ok(template);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProvisionTemplateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
            return BadRequest(new ApiError("Title and body are required.", "INVALID_INPUT"));

        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var template = await provisionService.UpdateTemplateAsync(id, landlordId.Value, request);
        if (template is null) return NotFound(new ApiError("Template not found.", "TEMPLATE_NOT_FOUND"));

        return Ok(template);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var deleted = await provisionService.DeleteTemplateAsync(id, landlordId.Value);
        if (!deleted) return NotFound(new ApiError("Template not found.", "TEMPLATE_NOT_FOUND"));

        return NoContent();
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
