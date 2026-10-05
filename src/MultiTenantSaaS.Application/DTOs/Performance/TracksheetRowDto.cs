namespace MultiTenantSaaS.Application.DTOs.Performance;

/// <summary>Mirrors the frontend `TrackRow` type field-for-field.</summary>
public class TracksheetRowDto
{
    public string Id { get; set; } = string.Empty;
    public string NextPort { get; set; } = string.Empty;
    public string Rt { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public double? Hrs { get; set; }
    public string Lat { get; set; } = string.Empty;
    public string Lng { get; set; } = string.Empty;

    public double? VlsfoRob { get; set; }
    public double? VlsfoBunkered { get; set; }
    public double? VlsfoCorrected { get; set; }
    public double? LsmgoRob { get; set; }
    public double? LsmgoBunkered { get; set; }
    public double? LsmgoCorrected { get; set; }
    public double? NoneRob { get; set; }
    public double? NoneBunkered { get; set; }
    public double? NoneCorrected { get; set; }

    public double? DistR { get; set; }
    public double? DistO { get; set; }
    public double? DtgO { get; set; }
    public double? AvgSpeedO { get; set; }

    public double? Rpm { get; set; }
    public double? EnginePower { get; set; }
    public double? Slip { get; set; }
    public double? Course { get; set; }
    public double? Amount { get; set; }

    public string WindO { get; set; } = string.Empty;
    public string WavesO { get; set; } = string.Empty;
    public double WindF { get; set; }
    public double WaveF { get; set; }
    public double CurrF { get; set; }
    public string AvgF { get; set; } = string.Empty;
}

public class SaveTracksheetRequestDto
{
    public string VoyageId { get; set; } = string.Empty;
    public string VesselImo { get; set; } = string.Empty;
    public List<TracksheetRowDto> Rows { get; set; } = new();
}
