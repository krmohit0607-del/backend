using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Chartering;
using MultiTenantSaaS.Application.Features.Chartering.Commands;
using MultiTenantSaaS.Application.Features.Chartering.Queries;
using MultiTenantSaaS.Application.Services;

namespace MultiTenantSaaS.API.Controllers;

[Route("api/chartering")]
[Authorize]
public class CharteringController : BaseApiController
{
    private readonly ICharteringCalculationService _calculationService;
    private readonly IEstimationValidationService _validationService;
    private readonly IBunkerRobService _bunkerRobService;
    private readonly ICalculationDetailsService _calculationDetailsService;

    public CharteringController(
        ICharteringCalculationService calculationService,
        IEstimationValidationService validationService,
        IBunkerRobService bunkerRobService,
        ICalculationDetailsService calculationDetailsService)
    {
        _calculationService = calculationService;
        _validationService = validationService;
        _bunkerRobService = bunkerRobService;
        _calculationDetailsService = calculationDetailsService;
    }

    /// <summary>
    /// List all saved voyage estimates for the caller's tenant.
    /// </summary>
    [HttpGet("estimates")]
    [ProducesResponseType(typeof(ApiResponse<List<VoyageEstimateDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEstimates()
    {
        var result = await Mediator.Send(new GetVoyageEstimatesQuery());
        return Ok(result);
    }

    /// <summary>
    /// Save or update a voyage estimation calculation.
    /// </summary>
    [HttpPost("estimates")]
    [ProducesResponseType(typeof(ApiResponse<VoyageEstimateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertEstimate([FromBody] CreateVoyageEstimateRequestDto dto)
    {
        var result = await Mediator.Send(new UpsertVoyageEstimateCommand(dto));
        return Ok(result);
    }

    /// <summary>
    /// Auto-save estimate with calculated results (debounced, fire-and-forget).
    /// Calculates voyage estimations from the full data JSON and stores both inputs and results.
    /// Auto-save does NOT create new records; only explicit saves create records.
    /// </summary>
    [HttpPost("estimates/auto-save")]
    [ProducesResponseType(typeof(ApiResponse<VoyageEstimateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AutoSaveEstimate([FromBody] CreateVoyageEstimateRequestDto dto)
    {
        // Mark as auto-save so backend doesn't create new records
        dto.IsAutoSave = true;

        // Calculate the results from the data JSON
        if (!string.IsNullOrEmpty(dto.DataJson))
        {
            var calculation = _calculationService.CalculateEstimate(dto.DataJson);
            dto.Profit = calculation.Profit;
            dto.Tce = calculation.Tce;
        }

        var result = await Mediator.Send(new UpsertVoyageEstimateCommand(dto));
        return Ok(result);
    }

    /// <summary>
    /// Calculate voyage estimation results from estimate inputs.
    /// Used by frontend to get real-time calculation results without saving.
    /// </summary>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(ApiResponse<VoyageEstimateCalculationDto>), StatusCodes.Status200OK)]
    public IActionResult CalculateEstimate([FromBody] string dataJson)
    {
        var result = _calculationService.CalculateEstimate(dataJson);
        return Ok(ApiResponse<VoyageEstimateCalculationDto>.SuccessResult(result));
    }

    /// <summary>
    /// Calculate loadable quantity based on vessel particulars and port conditions.
    /// </summary>
    [HttpPost("calculate-loadable-quantity")]
    [ProducesResponseType(typeof(ApiResponse<LoadableQuantityDto>), StatusCodes.Status200OK)]
    public IActionResult CalculateLoadableQuantity([FromBody] CalculateLoadableQuantityRequest request)
    {
        var result = _calculationService.CalculateLoadableQuantity(request.EstimateDataJson, request.LqDataJson);
        return Ok(ApiResponse<LoadableQuantityDto>.SuccessResult(result));
    }

    /// <summary>
    /// Delete a saved voyage estimate.
    /// </summary>
    [HttpDelete("estimates/{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteEstimate(Guid id)
    {
        var result = await Mediator.Send(new DeleteVoyageEstimateCommand(id));
        return Ok(result);
    }

    /// <summary>
    /// List all entries in the Cargo Book.
    /// </summary>
    [HttpGet("books/cargo")]
    [ProducesResponseType(typeof(ApiResponse<List<CargoBookDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCargoBook()
    {
        var result = await Mediator.Send(new GetCargoBookQuery());
        return Ok(result);
    }

    /// <summary>
    /// List all entries in the Tonnage Book.
    /// </summary>
    [HttpGet("books/tonnage")]
    [ProducesResponseType(typeof(ApiResponse<List<TonnageBookDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTonnageBook()
    {
        var result = await Mediator.Send(new GetTonnageBookQuery());
        return Ok(result);
    }

    /// <summary>
    /// Validate estimation inputs - identify missing or invalid data before calculation.
    /// Returns errors (prevent calculation) and warnings (informational).
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ApiResponse<EstimationValidationResultDto>), StatusCodes.Status200OK)]
    public IActionResult ValidateEstimate([FromBody] string dataJson)
    {
        var validationResult = _validationService.ValidateEstimateInputs(dataJson);
        
        var dto = new EstimationValidationResultDto
        {
            Issues = validationResult.Issues.Select(x => new ValidationIssueDto
            {
                Field = x.Field,
                Message = x.Message,
                Level = x.Level
            }).ToList()
        };

        return Ok(ApiResponse<EstimationValidationResultDto>.SuccessResult(dto));
    }

    /// <summary>
    /// Calculate bunker ROB (Remaining On Board) throughout the voyage.
    /// Shows opening/closing ROB for each leg and identifies negative ROB warnings.
    /// </summary>
    [HttpPost("calculate-rob")]
    [ProducesResponseType(typeof(ApiResponse<BunkerRobCalculationResultDto>), StatusCodes.Status200OK)]
    public IActionResult CalculateBunkerROB([FromBody] CalculateBunkerRobRequest request)
    {
        var calculation = _calculationService.CalculateEstimate(request.DataJson);
        var robResult = _bunkerRobService.CalculateROB(request.DataJson, calculation);

        var dto = new BunkerRobCalculationResultDto
        {
            InitialFOROB = robResult.InitialFOROB,
            InitialDOROB = robResult.InitialDOROB,
            FinalFOROB = robResult.FinalFOROB,
            FinalDOROB = robResult.FinalDOROB,
            FoRobProgression = robResult.FoRobProgression.Select(x => new RobLegDetailDto
            {
                LegNumber = x.LegNumber,
                PortName = x.PortName,
                OpeningROB = x.OpeningROB,
                Consumption = x.Consumption,
                Supply = x.Supply,
                ClosingROB = x.ClosingROB,
                HasWarning = x.HasWarning
            }).ToList(),
            DoRobProgression = robResult.DoRobProgression.Select(x => new RobLegDetailDto
            {
                LegNumber = x.LegNumber,
                PortName = x.PortName,
                OpeningROB = x.OpeningROB,
                Consumption = x.Consumption,
                Supply = x.Supply,
                ClosingROB = x.ClosingROB,
                HasWarning = x.HasWarning
            }).ToList(),
            NegativeRobWarnings = robResult.NegativeRobWarnings,
            HasNegativeROB = robResult.HasNegativeROB
        };

        return Ok(ApiResponse<BunkerRobCalculationResultDto>.SuccessResult(dto));
    }

    /// <summary>
    /// Get detailed calculation breakdown: Distance → Speed → Days → Consumption → Cost.
    /// Used for "expand details" and audit trail features.
    /// </summary>
    [HttpPost("calculate-details")]
    [ProducesResponseType(typeof(ApiResponse<CalculationDetailResultDto>), StatusCodes.Status200OK)]
    public IActionResult GetCalculationDetails([FromBody] string dataJson)
    {
        var calculation = _calculationService.CalculateEstimate(dataJson);
        var details = _calculationDetailsService.GenerateCalculationDetails(dataJson, calculation);

        var dto = new CalculationDetailResultDto
        {
            LegDetails = details.LegDetails.Select(x => new LegCalculationDetailDto
            {
                LegNumber = x.LegNumber,
                PortName = x.PortName,
                PortType = x.PortType,
                Distance = x.Distance,
                Speed = x.Speed,
                WeatherFactor = x.WeatherFactor,
                EffectiveSpeed = x.EffectiveSpeed,
                CalculatedSeaDays = x.CalculatedSeaDays,
                EcaDays = x.EcaDays,
                NormalDays = x.NormalDays,
                ConsumptionRate = x.ConsumptionRate,
                ConsumptionRateUnit = x.ConsumptionRateUnit,
                CalculatedConsumption = x.CalculatedConsumption,
                FuelPrice = x.FuelPrice,
                FuelPriceUnit = x.FuelPriceUnit,
                BunkerCost = x.BunkerCost,
                PortIdleDays = x.PortIdleDays,
                PortWorkDays = x.PortWorkDays,
                Demurrage = x.Demurrage,
                Despatch = x.Despatch,
                PortCharge = x.PortCharge
            }).ToList(),
            Summary = new CalculationSummaryDetailDto
            {
                TotalSeaDays = details.Summary.TotalSeaDays,
                EcaDays = details.Summary.EcaDays,
                BallastDays = details.Summary.BallastDays,
                LadenDays = details.Summary.LadenDays,
                PortDays = details.Summary.PortDays,
                VoyageDays = details.Summary.VoyageDays,
                TotalDistance = details.Summary.TotalDistance,
                EcaDistance = details.Summary.EcaDistance,
                VlsfoCons = details.Summary.VlsfoCons,
                UlsfoCons = details.Summary.UlsfoCons,
                MgoCons = details.Summary.MgoCons,
                VlsfoPrice = details.Summary.VlsfoPrice,
                UlsfoPrice = details.Summary.UlsfoPrice,
                MgoPrice = details.Summary.MgoPrice,
                VlsfoExp = details.Summary.VlsfoExp,
                UlsfoExp = details.Summary.UlsfoExp,
                MgoExp = details.Summary.MgoExp,
                TotalBunkerExp = details.Summary.TotalBunkerExp,
                Freight = details.Summary.Freight,
                AddComm = details.Summary.AddComm,
                Brokerage = details.Summary.Brokerage,
                FreightTax = details.Summary.FreightTax,
                TotalOpExpense = details.Summary.TotalOpExpense,
                Revenue = details.Summary.Revenue,
                OpProfit = details.Summary.OpProfit,
                NetHire = details.Summary.NetHire,
                TotalExpense = details.Summary.TotalExpense,
                Profit = details.Summary.Profit,
                ProfitPerDay = details.Summary.ProfitPerDay,
                TCE = details.Summary.TCE
            }
        };

        return Ok(ApiResponse<CalculationDetailResultDto>.SuccessResult(dto));
    }
}

public class CalculateLoadableQuantityRequest
{
    public string EstimateDataJson { get; set; } = string.Empty;
    public string LqDataJson { get; set; } = string.Empty;
}

public class CalculateBunkerRobRequest
{
    public string DataJson { get; set; } = string.Empty;
}
