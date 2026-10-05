namespace MultiTenantSaaS.Application.DTOs.VoyageOrders;

public class VoyageOrderDto
{
    public Guid Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string? Vessel { get; set; }
    public string? Owner { get; set; }
    public string? Charterer { get; set; }
    public string? Broker { get; set; }
    public string? LoadPort { get; set; }
    public string? DischargePort { get; set; }
    public string? Cargo { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public string? Commodity { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreateVoyageOrderRequestDto
{
    public string OrderNo { get; set; } = string.Empty;
    public string? Vessel { get; set; }
    public string? Owner { get; set; }
    public string? Charterer { get; set; }
    public string? Broker { get; set; }
    public string? LoadPort { get; set; }
    public string? DischargePort { get; set; }
    public string? Cargo { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public string? Commodity { get; set; }
}

public class UpdateVoyageOrderRequestDto
{
    public string? Status { get; set; }
    public decimal? Quantity { get; set; }
}
