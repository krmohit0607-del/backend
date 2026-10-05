namespace MultiTenantSaaS.Application.DTOs.VoyageRecaps;

public class VoyageRecapDto
{
    public Guid Id { get; set; }
    public Guid VoyageId { get; set; }
    public string? PdaNo { get; set; }
    public string? FdaNo { get; set; }
    public string? CharterPartyReference { get; set; }
    public decimal Laytime { get; set; }
    public decimal Demurrage { get; set; }
    public decimal Despatch { get; set; }
    public decimal NetResultShip { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreateVoyageRecapRequestDto
{
    public Guid VoyageId { get; set; }
    public string? PdaNo { get; set; }
    public string? FdaNo { get; set; }
    public string? CharterPartyReference { get; set; }
    public decimal Laytime { get; set; }
    public decimal Demurrage { get; set; }
    public decimal Despatch { get; set; }
    public decimal NetResultShip { get; set; }
}

public class UpdateVoyageRecapRequestDto
{
    public decimal? NetResultShip { get; set; }
    public string? Status { get; set; }
}
