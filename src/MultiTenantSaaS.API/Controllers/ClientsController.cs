using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Clients;
using MultiTenantSaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/clients")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ClientsController(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>
    /// List all clients for the caller's tenant.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ClientDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClients()
    {
        var clients = await _db.Clients
            .Include(c => c.Contacts)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var dtos = clients.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<ClientDto>>.SuccessResult(dtos));
    }

    /// <summary>
    /// Get a single client by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ClientDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClientById(Guid id)
    {
        var client = await _db.Clients
            .Include(c => c.Contacts)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (client == null)
            return NotFound(ApiResponse<object>.FailureResult("Client not found"));

        return Ok(ApiResponse<ClientDto>.SuccessResult(MapToDto(client)));
    }

    /// <summary>
    /// Create a new client.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ClientDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateClient([FromBody] CreateClientRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(ApiResponse<object>.FailureResult("Client name is required"));

        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUser.TenantId,
            Name = dto.Name,
            Kind = dto.Kind,
            Category = dto.Category,
            Location = dto.Location,
            Email = dto.Email,
            ContactName = dto.ContactName,
            Phone = dto.Phone,
            WebsiteUrl = dto.WebsiteUrl,
            Role = dto.Role,
            IsActive = true,
            PicAssignment = dto.PicAssignment,
            BankName = dto.BankName,
            AccountHolder = dto.AccountHolder,
            AccountNumber = dto.AccountNumber,
            Swift = dto.Swift,
            Iban = dto.Iban,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUser.UserId,
            UpdatedByUserId = _currentUser.UserId
        };

        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        var resultDto = MapToDto(client);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<ClientDto>.SuccessResult(resultDto, "Client created successfully"));
    }

    /// <summary>
    /// Update an existing client.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ClientDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateClient(Guid id, [FromBody] UpdateClientRequestDto dto)
    {
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.Id == id);
        if (client == null)
            return NotFound(ApiResponse<object>.FailureResult("Client not found"));

        if (!string.IsNullOrWhiteSpace(dto.Name))
            client.Name = dto.Name;
        if (dto.Kind != null)
            client.Kind = dto.Kind;
        if (dto.Category != null)
            client.Category = dto.Category;
        if (dto.Location != null)
            client.Location = dto.Location;
        if (dto.Email != null)
            client.Email = dto.Email;
        if (dto.ContactName != null)
            client.ContactName = dto.ContactName;
        if (dto.Phone != null)
            client.Phone = dto.Phone;
        if (dto.WebsiteUrl != null)
            client.WebsiteUrl = dto.WebsiteUrl;
        if (dto.Role != null)
            client.Role = dto.Role;
        if (dto.IsActive.HasValue)
            client.IsActive = dto.IsActive.Value;
        if (dto.PicAssignment != null)
            client.PicAssignment = dto.PicAssignment;
        if (dto.BankName != null)
            client.BankName = dto.BankName;
        if (dto.AccountHolder != null)
            client.AccountHolder = dto.AccountHolder;
        if (dto.AccountNumber != null)
            client.AccountNumber = dto.AccountNumber;
        if (dto.Swift != null)
            client.Swift = dto.Swift;
        if (dto.Iban != null)
            client.Iban = dto.Iban;
        if (dto.BankAccountVerified.HasValue)
            client.BankAccountVerified = dto.BankAccountVerified.Value;
        if (dto.ComplianceStatus != null)
            client.ComplianceStatus = dto.ComplianceStatus;
        if (dto.Notes != null)
            client.Notes = dto.Notes;

        client.UpdatedAt = DateTime.UtcNow;
        client.UpdatedByUserId = _currentUser.UserId;

        _db.Clients.Update(client);
        await _db.SaveChangesAsync();

        var resultDto = MapToDto(client);
        return Ok(ApiResponse<ClientDto>.SuccessResult(resultDto, "Client updated successfully"));
    }

    /// <summary>
    /// Delete a client.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteClient(Guid id)
    {
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.Id == id);
        if (client == null)
            return NotFound(ApiResponse<object>.FailureResult("Client not found"));

        _db.Clients.Remove(client);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResult("Client deleted successfully"));
    }

    private static ClientDto MapToDto(Client client)
    {
        return new ClientDto
        {
            Id = client.Id,
            Name = client.Name,
            Kind = client.Kind,
            Category = client.Category,
            Location = client.Location,
            Email = client.Email,
            ContactName = client.ContactName,
            Phone = client.Phone,
            WebsiteUrl = client.WebsiteUrl,
            Role = client.Role,
            IsActive = client.IsActive,
            PicAssignment = client.PicAssignment,
            BankName = client.BankName,
            AccountHolder = client.AccountHolder,
            AccountNumber = client.AccountNumber,
            Swift = client.Swift,
            Iban = client.Iban,
            BankAccountVerified = client.BankAccountVerified,
            ComplianceStatus = client.ComplianceStatus,
            ComplianceCheckDate = client.ComplianceCheckDate,
            Notes = client.Notes,
            Contacts = client.Contacts?.Select(c => new ClientContactDto
            {
                Id = c.Id,
                ClientId = c.ClientId,
                Name = c.Name,
                Title = c.Title,
                Email = c.Email,
                Phone = c.Phone,
                Mobile = c.Mobile,
                IsPrimary = c.IsPrimary,
                IsActive = c.IsActive
            }).ToList() ?? new(),
            CreatedAt = client.CreatedAt,
            UpdatedAt = client.UpdatedAt
        };
    }
}
