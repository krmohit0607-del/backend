using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Domain.Entities;

namespace MultiTenantSaaS.Infrastructure.Services.Assistant;

internal static class FinanceRules
{
    public static readonly string[] SettledStatuses =
        ["closed", "reconciled", "payment executed", "bank confirmation", "paid", "received", "cancelled", "settled"];

    public static bool IsSettled(FinancialTransaction t) => SettledStatuses.Contains(t.Status.Trim().ToLowerInvariant());

    public static bool IsOverdue(FinancialTransaction t, string todayIso) =>
        !IsSettled(t) && !string.IsNullOrEmpty(t.DueIso) && string.CompareOrdinal(t.DueIso, todayIso) < 0;
}

internal sealed class ListTransactionsTool(IApplicationDbContext db) : IAssistantTool
{
    public string Name => "list_financial_transactions";
    public string Description => "List accounts-module payables and receivables (hire, freight, PDA/FDA, bunker, claims) with counterparty, invoice, amount, due date and status. Can show only overdue items.";
    public IReadOnlyList<AssistantParameter> Parameters { get; } =
    [
        new("kind", "string", "Payable or Receivable.", false, ["Payable", "Receivable"]),
        new("category", "string", "Part of the category, e.g. 'Hire', 'Bunker'."),
        new("status", "string", "Part of the status text."),
        new("vessel", "string", "Part of the vessel name."),
        new("counterparty", "string", "Part of the counterparty name."),
        new("overdueOnly", "boolean", "Only items past their due date that are not settled."),
        ToolArgs.Limit()
    ];

    public async Task<object> ExecuteAsync(AssistantToolContext context, JsonElement args, CancellationToken ct)
    {
        var query = db.FinancialTransactions.AsNoTracking().AsQueryable();
        var kind = ToolArgs.Text(args, "kind");
        var category = ToolArgs.Text(args, "category").ToLowerInvariant();
        var status = ToolArgs.Text(args, "status").ToLowerInvariant();
        var vessel = ToolArgs.Text(args, "vessel").ToLowerInvariant();
        var counterparty = ToolArgs.Text(args, "counterparty").ToLowerInvariant();
        if (kind.Length > 0) query = query.Where(t => t.Kind == kind);
        if (category.Length > 0) query = query.Where(t => t.Category.ToLower().Contains(category));
        if (status.Length > 0) query = query.Where(t => t.Status.ToLower().Contains(status));
        if (vessel.Length > 0) query = query.Where(t => t.VesselName.ToLower().Contains(vessel));
        if (counterparty.Length > 0) query = query.Where(t => t.Counterparty.ToLower().Contains(counterparty));

        var rows = await query.OrderBy(t => t.DueIso).Take(2000).ToListAsync(ct);
        if (ToolArgs.Bool(args, "overdueOnly"))
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            rows = rows.Where(t => FinanceRules.IsOverdue(t, today)).ToList();
        }

        var limit = ToolArgs.Limit(args);
        return new
        {
            total = rows.Count,
            returned = Math.Min(rows.Count, limit),
            note = "Amounts are in their own currency; no currency conversion is applied.",
            transactions = rows.Take(limit).Select(t => new
            {
                transactionNo = t.TransactionNo,
                kind = t.Kind,
                category = t.Category,
                vessel = t.VesselName,
                voyage = t.Voyage,
                counterparty = t.Counterparty,
                invoiceNo = t.InvoiceNo,
                currency = t.Currency,
                amount = t.Amount,
                dueDate = t.DueIso,
                status = t.Status,
                approval = t.Approval,
                priority = t.Priority,
                paymentDate = t.PaymentDate
            }).ToList()
        };
    }
}

internal sealed class FinanceSummaryTool(IApplicationDbContext db) : IAssistantTool
{
    public string Name => "get_finance_summary";
    public string Description => "Totals of payables and receivables per currency: open, overdue and settled amounts and counts. Optionally for one vessel. Use this for cash-position and exposure questions.";
    public IReadOnlyList<AssistantParameter> Parameters { get; } = [new("vessel", "string", "Part of the vessel name to restrict the totals.")];

    public async Task<object> ExecuteAsync(AssistantToolContext context, JsonElement args, CancellationToken ct)
    {
        var query = db.FinancialTransactions.AsNoTracking().AsQueryable();
        var vessel = ToolArgs.Text(args, "vessel").ToLowerInvariant();
        if (vessel.Length > 0) query = query.Where(t => t.VesselName.ToLower().Contains(vessel));
        var rows = await query.ToListAsync(ct);
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var groups = rows.GroupBy(t => new { t.Kind, t.Currency }).OrderBy(g => g.Key.Kind).ThenBy(g => g.Key.Currency).Select(g => new
        {
            kind = g.Key.Kind,
            currency = g.Key.Currency,
            openAmount = Math.Round(g.Where(t => !FinanceRules.IsSettled(t)).Sum(t => t.Amount), 2),
            openCount = g.Count(t => !FinanceRules.IsSettled(t)),
            overdueAmount = Math.Round(g.Where(t => FinanceRules.IsOverdue(t, today)).Sum(t => t.Amount), 2),
            overdueCount = g.Count(t => FinanceRules.IsOverdue(t, today)),
            settledAmount = Math.Round(g.Where(FinanceRules.IsSettled).Sum(t => t.Amount), 2),
            settledCount = g.Count(FinanceRules.IsSettled)
        }).ToList();
        return new { asOf = today, transactionCount = rows.Count, note = "Totals are per currency; no currency conversion is applied.", totals = groups };
    }
}

internal sealed class GetSettlementTool(IApplicationDbContext db) : IAssistantTool
{
    private const int Rows = 25;

    public string Name => "get_voyage_settlement";
    public string Description => "Postfix settlement picture for one voyage: PDAs, FDAs, agent invoices, additional services, claims, hire installments, laytime/demurrage calculations and settlement milestones.";
    public IReadOnlyList<AssistantParameter> Parameters { get; } = [ToolArgs.VoyageParameter];

    public async Task<object> ExecuteAsync(AssistantToolContext context, JsonElement args, CancellationToken ct)
    {
        var voyage = await ToolArgs.ResolveVoyageAsync(db, context, args, ct);
        if (voyage is null) return ToolArgs.NoVoyage();
        var keys = voyage.Keys;
        var guid = voyage.Entity?.Id ?? Guid.Empty;

        // Guid-keyed tables exist only for stored voyages; string-keyed ones also cover frontend voyage ids.
        var pdas = await db.ProFormaDisbursements.AsNoTracking().Where(p => p.VoyageId == guid).Take(Rows)
            .Select(p => new { p.PdaNo, p.Port, p.Agent, p.Currency, estimated = p.Estimated, advance = p.Advance, fdaFinal = p.FdaFinal, p.Status, p.Approval }).ToListAsync(ct);
        var fdas = await db.FinalDisbursements.AsNoTracking().Where(f => f.VoyageId == guid).Take(Rows)
            .Select(f => new { f.FdaNo, f.Port, f.Agent, f.Currency, amount = f.FdaAmount, advance = f.PdaAdvance, balance = f.BalancePayable, f.Status, f.Approval }).ToListAsync(ct);
        var invoices = await db.AgentInvoices.AsNoTracking().Where(i => i.VoyageId == guid).Take(Rows)
            .Select(i => new { i.InvoiceNo, i.Agent, i.Category, i.Port, i.Currency, i.Amount, i.Approved, i.Paid, dueDate = i.DueDate, i.DeptStatus, i.AccountsStatus }).ToListAsync(ct);
        var services = await db.AdditionalServices.AsNoTracking().Where(s => s.VoyageId == guid).Take(Rows)
            .Select(s => new { s.Service, s.Vendor, s.Currency, s.Cost, s.Tax, s.Status }).ToListAsync(ct);
        var milestones = await db.SettlementMilestones.AsNoTracking().Where(m => m.VoyageId == guid).OrderBy(m => m.Sequence).Take(Rows)
            .Select(m => new { m.MilestoneLabel, m.Status, scheduled = m.ScheduledDate, completed = m.CompletedDate }).ToListAsync(ct);
        var laytime = await db.LaytimeCalculations.AsNoTracking().Where(l => l.VoyageId == guid).Take(Rows)
            .Select(l => new { l.LaytimeTerms, l.DaysAllowed, l.DaysUsed, l.NetDemurragedays, l.Currency, l.DemurrageRate, l.DemurrageAmount, l.DespatchEarning, l.NetDemurrage, l.Status }).ToListAsync(ct);
        var claims = await db.ClaimRecords.AsNoTracking().Where(c => keys.Contains(c.VoyageId)).Take(Rows)
            .Select(c => new { c.ClaimType, c.ClaimReference, c.Currency, c.Amount, c.Settlement, c.Status, c.Owner, dueDate = c.DueDate, c.WorkflowStatus }).ToListAsync(ct);
        var hire = await db.HirePayments.AsNoTracking().Where(h => keys.Contains(h.VoyageId)).OrderBy(h => h.DueDate).Take(Rows)
            .Select(h => new { h.Side, h.Name, h.Account, from = h.FromDate, to = h.ToDate, dueDate = h.DueDate, h.OnHireDays, h.OffHireDays, h.Currency, h.Amount, h.Status }).ToListAsync(ct);

        return new
        {
            voyage = voyage.Display,
            note = "Amounts are in their own currency. Lists show at most 25 rows each.",
            proFormaDisbursements = pdas,
            finalDisbursements = fdas,
            agentInvoices = invoices,
            additionalServices = services,
            claims,
            hirePayments = hire,
            laytimeCalculations = laytime,
            milestones
        };
    }
}
