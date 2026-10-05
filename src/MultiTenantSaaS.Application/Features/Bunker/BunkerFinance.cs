using System.Text.Json;
using MultiTenantSaaS.Application.Common.Exceptions;

namespace MultiTenantSaaS.Application.Features.Bunker;

/// <summary>
/// Backend-authoritative cost + workflow rules for bunker requirements. Mirrors the frontend's
/// display formulas (src/data/bunker.ts) so totals can never be persisted as an arbitrary
/// client-sent value and so quote scoring can't be forged, and enforces the one hard workflow
/// rule (no payment before manager approval) regardless of what the client sends.
/// </summary>
public static class BunkerFinance
{
    private record ChargeJson(double Amount);
    private record ClaimJson(double Amount, string? Status);
    private record FuelLineJson(double Quantity, double? SuppliedQty, double? DeliveredQty);

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public static double SumAdditionalCharges(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return 0;
        try
        {
            var charges = JsonSerializer.Deserialize<List<ChargeJson>>(json, JsonOpts);
            return charges?.Sum(c => c.Amount) ?? 0;
        }
        catch { return 0; }
    }

    public static double SumDeductibleClaims(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return 0;
        try
        {
            var claims = JsonSerializer.Deserialize<List<ClaimJson>>(json, JsonOpts);
            return claims?.Where(c => !string.Equals(c.Status, "Rejected", StringComparison.OrdinalIgnoreCase)).Sum(c => c.Amount) ?? 0;
        }
        catch { return 0; }
    }

    /// <summary>
    /// Total quantity basis for cost calculations. Sums multi-fuel lines when present (falls back
    /// to the scalar Quantity otherwise). When <paramref name="preferActual"/> is true, uses the
    /// actually-delivered quantity (BDN-first, then supplied, then nominated) instead of the
    /// nominal/ordered quantity — the single rule deciding "what was ordered" vs "what arrived".
    /// </summary>
    public static double QuantityBasis(string? fuelLinesJson, double quantity, double? suppliedQty, double? deliveredQty, bool preferActual)
    {
        if (!string.IsNullOrWhiteSpace(fuelLinesJson))
        {
            try
            {
                var lines = JsonSerializer.Deserialize<List<FuelLineJson>>(fuelLinesJson, JsonOpts);
                if (lines is { Count: > 0 })
                {
                    return preferActual
                        ? lines.Sum(l => l.DeliveredQty ?? l.SuppliedQty ?? l.Quantity)
                        : lines.Sum(l => l.Quantity);
                }
            }
            catch { /* fall through to scalar fields */ }
        }
        return preferActual ? (deliveredQty ?? suppliedQty ?? quantity) : quantity;
    }

    private static readonly string[] StatusOrder =
    {
        "Pending RFQ", "RFQ Sent", "Quotes Received", "Supplier Selected", "Booked", "Supplied",
        "Invoice Received", "Manager Approval Pending", "Approved", "Sent to Accounts", "Payment Due", "Paid", "Closed",
    };

    private static int Rank(string status)
    {
        var i = Array.IndexOf(StatusOrder, status);
        return i < 0 ? 0 : i;
    }

    /// <summary>
    /// The one enforced business rule: a requirement can never reach Payment Due/Paid/Sent to
    /// Accounts unless its invoice has actually been Approved by a manager, and it can't be marked
    /// Approved before an invoice has been received — prevents a client from skipping the approval
    /// gate regardless of what the UI normally does.
    /// </summary>
    public static void ValidateWorkflow(string status, string approvalStatus)
    {
        if (Rank(status) >= Rank("Sent to Accounts") && !string.Equals(approvalStatus, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("status", "A bunker requirement cannot move to Sent to Accounts/Payment Due/Paid before its invoice is Approved by a manager.");
        }
        if (string.Equals(approvalStatus, "Approved", StringComparison.OrdinalIgnoreCase) && Rank(status) < Rank("Invoice Received"))
        {
            throw new ValidationException("approvalStatus", "A bunker requirement cannot be Approved before its supplier invoice has been received.");
        }
    }

    /// <summary>Recomputes TotalCost (nominal/booking basis) and, once an invoice exists, InvoiceAmount
    /// (actual-qty basis net of claims) directly on the entity — call right before SaveChanges.</summary>
    public static void RecomputeTotals(Domain.Entities.BunkerRequirement req)
    {
        if (req.PricePerMt is > 0)
        {
            var bookingQty = QuantityBasis(req.FuelLinesJson, req.Quantity, null, null, preferActual: false);
            var chargesTotal = SumAdditionalCharges(req.AdditionalChargesJson);
            req.TotalCost = Math.Round(bookingQty * req.PricePerMt.Value) + chargesTotal;
        }

        if (!string.IsNullOrWhiteSpace(req.InvoiceNo) && req.PricePerMt is > 0)
        {
            var invoiceQty = QuantityBasis(req.FuelLinesJson, req.Quantity, req.SuppliedQty, req.DeliveredQty, preferActual: true);
            var chargesTotal = SumAdditionalCharges(req.AdditionalChargesJson);
            var claimsTotal = SumDeductibleClaims(req.ClaimsJson);
            req.InvoiceAmount = Math.Round(invoiceQty * req.PricePerMt.Value) + chargesTotal - claimsTotal;
        }
    }
}
