namespace MultiTenantSaaS.Application.DTOs.LaytimeCalculations;

public class LaytimeCalculationDto
{
    public Guid Id { get; set; }
    public Guid VoyageId { get; set; }
    public double? AllowedLaytimeDays { get; set; }
    public double? UsedLaytimeDays { get; set; }
    public double? WeatherDays { get; set; }
    public double? ShiftingDays { get; set; }
    public double? ExceptedDays { get; set; }
    public double? DemurrageDays { get; set; }
    public double? DespatchDays { get; set; }
    public double? DemurrageRate { get; set; }
    public double? DespatchRate { get; set; }
    public decimal? DemurrageTotal { get; set; }
    public decimal? DespatchTotal { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateLaytimeCalculationRequestDto
{
    public Guid VoyageId { get; set; }
    public double? AllowedLaytimeDays { get; set; }
    public double? UsedLaytimeDays { get; set; }
    public double? WeatherDays { get; set; }
    public double? ShiftingDays { get; set; }
    public double? ExceptedDays { get; set; }
    public double? DemurrageDays { get; set; }
    public double? DespatchDays { get; set; }
    public double? DemurrageRate { get; set; }
    public double? DespatchRate { get; set; }
}

public class UpdateLaytimeCalculationRequestDto
{
    public double? AllowedLaytimeDays { get; set; }
    public double? UsedLaytimeDays { get; set; }
    public double? WeatherDays { get; set; }
    public double? ShiftingDays { get; set; }
    public double? ExceptedDays { get; set; }
    public double? DemurrageDays { get; set; }
    public double? DespatchDays { get; set; }
    public double? DemurrageRate { get; set; }
    public double? DespatchRate { get; set; }
    public string Status { get; set; }
}
