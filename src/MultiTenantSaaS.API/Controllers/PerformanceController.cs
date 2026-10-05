using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Application.Common.Models;
using MultiTenantSaaS.Application.DTOs.Performance;
using MultiTenantSaaS.Domain.Entities.Performance;

namespace MultiTenantSaaS.API.Controllers;

/// <summary>
/// Performance module (tracksheets + voyage performance reports) — backed by
/// its own, physically separate database (<see cref="IPerformanceDbContext"/>
/// / "PerformanceConnection"), independent from the main tenant database, so
/// this can be run/scaled as its own service while still scoping every row
/// to the caller's tenant (via the shared auth token) and correlating back
/// to a voyage by plain id.
/// </summary>
[Route("api/performance")]
[Authorize]
public class PerformanceController : ControllerBase
{
    private readonly IPerformanceDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public PerformanceController(IPerformanceDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>All tracksheet rows for a voyage, in grid order.</summary>
    [HttpGet("tracksheet")]
    [ProducesResponseType(typeof(ApiResponse<List<TracksheetRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTracksheet([FromQuery] string voyageId)
    {
        var rows = await _db.TracksheetRows
            .Where(r => r.VoyageId == voyageId)
            .OrderBy(r => r.SortOrder)
            .ToListAsync();

        return Ok(ApiResponse<List<TracksheetRowDto>>.SuccessResult(rows.Select(MapToDto).ToList()));
    }

    /// <summary>
    /// Replace the entire tracksheet grid for a voyage with the given rows
    /// (grid rows can be freely added/edited/removed, so a full-replace is
    /// simpler and safer than diffing individual row changes).
    /// </summary>
    [HttpPut("tracksheet")]
    [ProducesResponseType(typeof(ApiResponse<List<TracksheetRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveTracksheet([FromQuery] string voyageId, [FromBody] SaveTracksheetRequestDto dto)
    {
        var existing = await _db.TracksheetRows.Where(r => r.VoyageId == voyageId).ToListAsync();
        foreach (var row in existing) _db.TracksheetRows.Remove(row);

        var tenantId = _currentUser.TenantId;
        var toInsert = dto.Rows.Select((r, index) => new TracksheetRow
        {
            TenantId = tenantId,
            VoyageId = voyageId,
            VesselImo = dto.VesselImo,
            SortOrder = index,
            NextPort = r.NextPort,
            Rt = r.Rt,
            Date = r.Date,
            Time = r.Time,
            Hrs = r.Hrs,
            Lat = r.Lat,
            Lng = r.Lng,
            VlsfoRob = r.VlsfoRob,
            VlsfoBunkered = r.VlsfoBunkered,
            VlsfoCorrected = r.VlsfoCorrected,
            LsmgoRob = r.LsmgoRob,
            LsmgoBunkered = r.LsmgoBunkered,
            LsmgoCorrected = r.LsmgoCorrected,
            NoneRob = r.NoneRob,
            NoneBunkered = r.NoneBunkered,
            NoneCorrected = r.NoneCorrected,
            DistR = r.DistR,
            DistO = r.DistO,
            DtgO = r.DtgO,
            AvgSpeedO = r.AvgSpeedO,
            Rpm = r.Rpm,
            EnginePower = r.EnginePower,
            Slip = r.Slip,
            Course = r.Course,
            Amount = r.Amount,
            WindO = r.WindO,
            WavesO = r.WavesO,
            WindF = r.WindF,
            WaveF = r.WaveF,
            CurrF = r.CurrF,
            AvgF = r.AvgF,
            UpdatedAt = DateTime.UtcNow,
        }).ToList();

        await _db.TracksheetRows.AddRangeAsync(toInsert);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<List<TracksheetRowDto>>.SuccessResult(toInsert.Select(MapToDto).ToList()));
    }

    /// <summary>The saved end-of-voyage performance report, or 404 if none has been saved yet.</summary>
    [HttpGet("report")]
    [ProducesResponseType(typeof(ApiResponse<PerformanceReportResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReport([FromQuery] string voyageId)
    {
        var report = await _db.PerformanceReports.FirstOrDefaultAsync(r => r.VoyageId == voyageId);
        if (report == null) return NotFound(ApiResponse<object>.FailureResult("No saved performance report for this voyage"));

        return Ok(ApiResponse<PerformanceReportResponseDto>.SuccessResult(new PerformanceReportResponseDto
        {
            VoyageId = report.VoyageId,
            VesselImo = report.VesselImo,
            VesselName = report.VesselName,
            Report = JsonDocument.Parse(report.ReportJson).RootElement,
            UpdatedAt = report.UpdatedAt ?? report.CreatedAt,
        }));
    }

    /// <summary>Create or overwrite the saved performance report for a voyage.</summary>
    [HttpPut("report")]
    [ProducesResponseType(typeof(ApiResponse<PerformanceReportResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveReport([FromQuery] string voyageId, [FromBody] SavePerformanceReportRequestDto dto)
    {
        var reportJson = dto.Report.GetRawText();
        var existing = await _db.PerformanceReports.FirstOrDefaultAsync(r => r.VoyageId == voyageId);

        if (existing == null)
        {
            existing = new VoyagePerformanceReport
            {
                TenantId = _currentUser.TenantId,
                VoyageId = voyageId,
            };
            _db.PerformanceReports.Add(existing);
        }

        existing.VesselImo = dto.VesselImo;
        existing.VesselName = dto.VesselName;
        existing.ReportJson = reportJson;
        existing.UpdatedByEmail = _currentUser.Email;
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<PerformanceReportResponseDto>.SuccessResult(new PerformanceReportResponseDto
        {
            VoyageId = existing.VoyageId,
            VesselImo = existing.VesselImo,
            VesselName = existing.VesselName,
            Report = JsonDocument.Parse(existing.ReportJson).RootElement,
            UpdatedAt = existing.UpdatedAt ?? existing.CreatedAt,
        }));
    }

    private static TracksheetRowDto MapToDto(TracksheetRow r) => new()
    {
        Id = r.Id.ToString(),
        NextPort = r.NextPort,
        Rt = r.Rt,
        Date = r.Date,
        Time = r.Time,
        Hrs = r.Hrs,
        Lat = r.Lat,
        Lng = r.Lng,
        VlsfoRob = r.VlsfoRob,
        VlsfoBunkered = r.VlsfoBunkered,
        VlsfoCorrected = r.VlsfoCorrected,
        LsmgoRob = r.LsmgoRob,
        LsmgoBunkered = r.LsmgoBunkered,
        LsmgoCorrected = r.LsmgoCorrected,
        NoneRob = r.NoneRob,
        NoneBunkered = r.NoneBunkered,
        NoneCorrected = r.NoneCorrected,
        DistR = r.DistR,
        DistO = r.DistO,
        DtgO = r.DtgO,
        AvgSpeedO = r.AvgSpeedO,
        Rpm = r.Rpm,
        EnginePower = r.EnginePower,
        Slip = r.Slip,
        Course = r.Course,
        Amount = r.Amount,
        WindO = r.WindO,
        WavesO = r.WavesO,
        WindF = r.WindF,
        WaveF = r.WaveF,
        CurrF = r.CurrF,
        AvgF = r.AvgF,
    };
}
