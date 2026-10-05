using MultiTenantSaaS.Application.DTOs.Chartering;

namespace MultiTenantSaaS.Application.Services;

/// <summary>
/// Validation service for chartering estimation inputs.
/// Identifies missing or invalid data before calculation.
/// </summary>
public interface IEstimationValidationService
{
    EstimationValidationResultDto ValidateEstimateInputs(string dataJson);
}

public class EstimationValidationService : IEstimationValidationService
{
    public EstimationValidationResultDto ValidateEstimateInputs(string dataJson)
    {
        var result = new EstimationValidationResultDto();
        
        if (string.IsNullOrWhiteSpace(dataJson))
        {
            result.Issues.Add(new ValidationIssueDto { Field = "EstimationData", Message = "Estimation data is empty", Level = "Error" });
            return result;
        }

        try
        {
            var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(dataJson);
            if (data.ValueKind != System.Text.Json.JsonValueKind.Object)
            {
                result.Issues.Add(new ValidationIssueDto { Field = "EstimationData", Message = "Invalid estimation data format", Level = "Error" });
                return result;
            }

            // Validate vessel
            var vesselName = GetStringValue(data, "vesselName");
            if (string.IsNullOrWhiteSpace(vesselName))
                result.Issues.Add(new ValidationIssueDto { Field = "Vessel", Message = "No vessel selected", Level = "Warning" });

            // Validate performance (speeds, consumption)
            ValidatePerformance(data, result);

            // Validate cargoes
            ValidateCargoes(data, result);

            // Validate ports
            ValidatePorts(data, result);

            // Validate commercial terms
            ValidateCommercial(data, result);

            // Validate bunker ROB
            ValidateBunkerROB(data, result);
        }
        catch (Exception ex)
        {
            result.Issues.Add(new ValidationIssueDto { Field = "Parsing", Message = $"Failed to parse estimation data: {ex.Message}", Level = "Error" });
        }

        return result;
    }

    private void ValidatePerformance(System.Text.Json.JsonElement data, EstimationValidationResultDto result)
    {
        if (!data.TryGetProperty("perf", out var perf))
        {
            result.Issues.Add(new ValidationIssueDto { Field = "Performance", Message = "Vessel performance/consumption not configured", Level = "Warning" });
            return;
        }

        var speedMode = GetStringValue(perf, "speedMode");
        if (string.IsNullOrWhiteSpace(speedMode))
            result.Issues.Add(new ValidationIssueDto { Field = "Performance", Message = "Speed mode not selected (Full/Eco/Custom)", Level = "Warning" });

        // Validate selected consumption
        var mainNormal = perf.TryGetProperty("mainNormal", out var mn) ? mn : default;
        var mainEca = perf.TryGetProperty("mainEca", out var me) ? me : default;

        var ladenCons = GetDoubleValue(speedMode == "Eco" ? mainEca : mainNormal, "laden");
        if (ladenCons <= 0)
            result.Issues.Add(new ValidationIssueDto { Field = "Performance", Message = "Laden fuel consumption is zero or missing", Level = "Warning" });

        var idleCons = GetDoubleValue(speedMode == "Eco" ? mainEca : mainNormal, "idle");
        if (idleCons < 0)
            result.Issues.Add(new ValidationIssueDto { Field = "Performance", Message = "Idle fuel consumption is negative", Level = "Warning" });
    }

    private void ValidateCargoes(System.Text.Json.JsonElement data, EstimationValidationResultDto result)
    {
        if (!data.TryGetProperty("cargoes", out var cargoesEl) || cargoesEl.ValueKind != System.Text.Json.JsonValueKind.Array)
        {
            result.Issues.Add(new ValidationIssueDto { Field = "Cargo", Message = "No cargoes defined", Level = "Error" });
            return;
        }

        var cargoArray = cargoesEl.EnumerateArray().ToList();
        if (cargoArray.Count == 0)
        {
            result.Issues.Add(new ValidationIssueDto { Field = "Cargo", Message = "At least one cargo row is required", Level = "Error" });
            return;
        }

        int cargoNum = 0;
        foreach (var cargo in cargoArray)
        {
            cargoNum++;
            var quantity = GetDoubleValue(cargo, "quantity");
            if (quantity <= 0)
                result.Issues.Add(new ValidationIssueDto { Field = "Cargo", Message = $"Cargo {cargoNum}: Quantity is zero or missing", Level = "Warning" });

            var loadPort = GetStringValue(cargo, "loadPort");
            if (string.IsNullOrWhiteSpace(loadPort))
                result.Issues.Add(new ValidationIssueDto { Field = "Cargo", Message = $"Cargo {cargoNum}: Loading port not specified", Level = "Warning" });

            var dischPort = GetStringValue(cargo, "dischPort");
            if (string.IsNullOrWhiteSpace(dischPort))
                result.Issues.Add(new ValidationIssueDto { Field = "Cargo", Message = $"Cargo {cargoNum}: Discharge port not specified", Level = "Warning" });

            var frt = GetDoubleValue(cargo, "frt");
            if (frt < 0)
                result.Issues.Add(new ValidationIssueDto { Field = "Cargo", Message = $"Cargo {cargoNum}: Freight rate is negative", Level = "Warning" });
        }
    }

    private void ValidatePorts(System.Text.Json.JsonElement data, EstimationValidationResultDto result)
    {
        if (!data.TryGetProperty("ports", out var portsEl) || portsEl.ValueKind != System.Text.Json.JsonValueKind.Array)
        {
            result.Issues.Add(new ValidationIssueDto { Field = "Ports", Message = "No ports defined", Level = "Error" });
            return;
        }

        var portArray = portsEl.EnumerateArray().ToList();
        if (portArray.Count == 0)
        {
            result.Issues.Add(new ValidationIssueDto { Field = "Ports", Message = "At least one port is required", Level = "Error" });
            return;
        }

        int portNum = 0;
        foreach (var port in portArray)
        {
            portNum++;
            var portName = GetStringValue(port, "port");
            if (string.IsNullOrWhiteSpace(portName))
                result.Issues.Add(new ValidationIssueDto { Field = "Ports", Message = $"Port {portNum}: Port name not specified", Level = "Warning" });

            var distance = GetDoubleValue(port, "distance");
            var seaManual = GetDoubleValue(port, "seaManual");
            if (distance <= 0 && seaManual <= 0)
                result.Issues.Add(new ValidationIssueDto { Field = "Ports", Message = $"Port {portNum}: Distance and sea days both missing", Level = "Warning" });

            if (distance < 0)
                result.Issues.Add(new ValidationIssueDto { Field = "Ports", Message = $"Port {portNum}: Distance cannot be negative", Level = "Error" });

            var ecaDistance = GetDoubleValue(port, "ecaDistance");
            if (ecaDistance > distance)
                result.Issues.Add(new ValidationIssueDto { Field = "Ports", Message = $"Port {portNum}: ECA distance exceeds total distance", Level = "Warning" });

            var speed = GetDoubleValue(port, "speed");
            if (speed > 0 && speed < 5)
                result.Issues.Add(new ValidationIssueDto { Field = "Ports", Message = $"Port {portNum}: Speed is very low ({speed} kts)", Level = "Warning" });
            if (speed > 25)
                result.Issues.Add(new ValidationIssueDto { Field = "Ports", Message = $"Port {portNum}: Speed is very high ({speed} kts)", Level = "Warning" });

            var dem = GetDoubleValue(port, "dem");
            if (dem < 0)
                result.Issues.Add(new ValidationIssueDto { Field = "Ports", Message = $"Port {portNum}: Demurrage rate is negative", Level = "Warning" });
        }

        // Validate date progression
        if (portArray.Count > 0)
        {
            var lastPort = portArray[portArray.Count - 1];
            var lastPortType = GetStringValue(lastPort, "type");
            if (lastPortType != "Redelivery" && lastPortType != "Delivery")
                result.Issues.Add(new ValidationIssueDto { Field = "Ports", Message = "Final port should typically be Delivery/Redelivery", Level = "Warning" });
        }
    }

    private void ValidateCommercial(System.Text.Json.JsonElement data, EstimationValidationResultDto result)
    {
        if (!data.TryGetProperty("commercial", out var commercial))
            return;

        var vlsfoPrice = GetDoubleValue(commercial, "vlsfoPrice");
        var ulsfoPrice = GetDoubleValue(commercial, "ulsfoPrice");
        var mgoPrice = GetDoubleValue(commercial, "mgoPrice");

        if (vlsfoPrice <= 0)
            result.Issues.Add(new ValidationIssueDto { Field = "Commercial", Message = "VLSFO price not set or zero", Level = "Warning" });
        if (ulsfoPrice <= 0)
            result.Issues.Add(new ValidationIssueDto { Field = "Commercial", Message = "ULSFO price not set or zero", Level = "Warning" });
        if (mgoPrice <= 0)
            result.Issues.Add(new ValidationIssueDto { Field = "Commercial", Message = "MGO price not set or zero", Level = "Warning" });

        var dailyHire = GetDoubleValue(commercial, "dailyHire");
        if (dailyHire < 0)
            result.Issues.Add(new ValidationIssueDto { Field = "Commercial", Message = "Daily hire is negative", Level = "Warning" });
    }

    private void ValidateBunkerROB(System.Text.Json.JsonElement data, EstimationValidationResultDto result)
    {
        // Check for negative ROB scenarios
        var bodQty = data.TryGetProperty("commercial", out var c) ? GetDoubleValue(c, "bodQty") : 0;
        var borQty = data.TryGetProperty("commercial", out var c2) ? GetDoubleValue(c2, "borQty") : 0;

        if (bodQty < 0)
            result.Issues.Add(new ValidationIssueDto { Field = "Bunker", Message = "BOD (delivery) quantity cannot be negative", Level = "Error" });
        if (borQty < 0)
            result.Issues.Add(new ValidationIssueDto { Field = "Bunker", Message = "BOR (redelivery) quantity cannot be negative", Level = "Error" });
    }

    private string GetStringValue(System.Text.Json.JsonElement el, string key)
    {
        if (el.TryGetProperty(key, out var prop) && prop.ValueKind == System.Text.Json.JsonValueKind.String)
            return prop.GetString() ?? "";
        return "";
    }

    private double GetDoubleValue(System.Text.Json.JsonElement el, string key)
    {
        if (el.TryGetProperty(key, out var prop))
        {
            if (prop.ValueKind == System.Text.Json.JsonValueKind.Number)
                return prop.GetDouble();
        }
        return 0;
    }

    private DateTime GetDateTimeValue(System.Text.Json.JsonElement el, string key)
    {
        if (el.TryGetProperty(key, out var prop) && prop.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            if (DateTime.TryParse(prop.GetString(), out var dt))
                return dt;
        }
        return DateTime.UtcNow;
    }
}
