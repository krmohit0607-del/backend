namespace MultiTenantSaaS.Application.DTOs.Performance;

/// <summary>One entry of the wind/wave weather-factor calibration table (see `WeatherFactorEntry`).</summary>
public class WeatherFactorEntryDto
{
    public string Weather { get; set; } = string.Empty;
    public double DeadWeight { get; set; }
    public double Magnitude { get; set; }
    public double Angle { get; set; }
    public double Value { get; set; }
}
