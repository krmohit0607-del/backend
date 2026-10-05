namespace MultiTenantSaaS.Application.DTOs.Clients;

public class ClientDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Email { get; set; }
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? Role { get; set; }
    public bool IsActive { get; set; } = true;
    public string? PicAssignment { get; set; }
    public string? BankName { get; set; }
    public string? AccountHolder { get; set; }
    public string? AccountNumber { get; set; }
    public string? Swift { get; set; }
    public string? Iban { get; set; }
    public bool BankAccountVerified { get; set; }
    public string? ComplianceStatus { get; set; }
    public DateTime? ComplianceCheckDate { get; set; }
    public string? Notes { get; set; }
    public List<ClientContactDto> Contacts { get; set; } = new();
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ClientContactDto
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CreateClientRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = "Account"; // Account or ServiceProvider
    public string Category { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Email { get; set; }
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? Role { get; set; }
    public string? PicAssignment { get; set; }
    public string? BankName { get; set; }
    public string? AccountHolder { get; set; }
    public string? AccountNumber { get; set; }
    public string? Swift { get; set; }
    public string? Iban { get; set; }
    public string? Notes { get; set; }
}

public class UpdateClientRequestDto
{
    public string? Name { get; set; }
    public string? Kind { get; set; }
    public string? Category { get; set; }
    public string? Location { get; set; }
    public string? Email { get; set; }
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
    public string? PicAssignment { get; set; }
    public string? BankName { get; set; }
    public string? AccountHolder { get; set; }
    public string? AccountNumber { get; set; }
    public string? Swift { get; set; }
    public string? Iban { get; set; }
    public bool? BankAccountVerified { get; set; }
    public string? ComplianceStatus { get; set; }
    public string? Notes { get; set; }
}
