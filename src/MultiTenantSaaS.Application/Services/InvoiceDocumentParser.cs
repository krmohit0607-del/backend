using System.Globalization;
using System.Text.RegularExpressions;

namespace MultiTenantSaaS.Application.Services;

public static class InvoiceDocumentParser
{
    public static Dictionary<string, object> Extract(string text, string kind)
    {
        var fields = new Dictionary<string, object>();
        var lines = text.Replace("\r", "").Split(['\n', '\t']).Select(line => line.Trim()).Where(line => line.Length > 0).ToArray();
        string? Find(string labels)
        {
            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index];
                var match = Regex.Match(line, $@"^(?:{labels})(?:\s*[:=#]\s*|\s+-\s+|\s+)(.+)$", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
                if (match.Success) return match.Groups[1].Value.Trim();
                if (index + 1 < lines.Length && Regex.IsMatch(line, $@"^(?:{labels})\s*[:=#]?\s*$", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)) && !Regex.IsMatch(lines[index + 1], @"^[\p{L} ]+[:=#]"))
                    return lines[index + 1];
            }
            return null;
        }
        void Text(string key, string labels)
        {
            var value = Find(labels);
            if (!string.IsNullOrWhiteSpace(value)) fields[key] = value;
        }
        void Amount(string key, string labels, bool asString = false)
        {
            var value = Find(labels);
            if (value == null) return;
            var match = Regex.Matches(value, @"(?<!\w)-?\d[\d ,.]*\d|(?<!\w)-?\d", RegexOptions.None, TimeSpan.FromSeconds(1)).Cast<Match>()
                .FirstOrDefault(candidate => asString || !value[(candidate.Index + candidate.Length)..].TrimStart().StartsWith('%'));
            if (match == null) return;
            var number = match.Value.Replace(" ", "").TrimEnd('.', ',');
            if (Regex.IsMatch(number, @",\d{2}$") && !Regex.IsMatch(number, @"\.\d{2}$")) number = number.Replace(".", "").Replace(',', '.');
            else number = number.Replace(",", "");
            if (decimal.TryParse(number, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                fields[key] = asString ? parsed.ToString(CultureInfo.InvariantCulture) : parsed;
        }
        void Date(string key, string labels)
        {
            var value = Find(labels);
            if (value == null) return;
            var formats = new[] { "d-M-yyyy", "d/M/yyyy", "d.M.yyyy", "yyyy-MM-dd", "d MMM yyyy", "d MMMM yyyy", "MMM d, yyyy", "MMMM d, yyyy" };
            if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsed))
                fields[key] = parsed.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        }
        var currency = Find(@"currency|invoice currency");
        var currencyMatch = Regex.Match(currency ?? text, @"\b(?:USD|EUR|GBP|SGD|AED|INR|JPY|CNY)\b", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        if (currencyMatch.Success && kind is not ("Freight" or "Demurrage")) fields["currency"] = currencyMatch.Value.ToUpperInvariant();
        if (kind is "PDA" or "FDA")
        {
            Text("port", @"port(?: of call)?|port name");
            Text("agent", @"agent(?: name)?|vendor|supplier|issued by|from");
            Date("due", @"due date|payment due(?: date)?");
            Amount("estimated", @"estimated pda|pda(?: total| amount)?|pro[ -]?forma(?: total| amount)?");
            Amount("fdaFinal", @"fda(?: final| total| amount)?|final disbursement(?: amount| total)?");
            if (!fields.ContainsKey("fdaFinal") && !fields.ContainsKey("estimated"))
                Amount(kind == "FDA" ? "fdaFinal" : "estimated", @"grand total|invoice total|total amount(?: due)?|amount due|balance due|total(?: due)?");
            Amount("advance", @"advance(?: paid| payment)?|amount paid");
        }
        else if (kind is "Agent" or "Service")
        {
            Text(kind == "Agent" ? "invoiceNo" : "invoice", @"invoice(?:\s*(?:no\.?|number|#))|reference");
            Text("vendor", @"vendor(?: name)?|supplier(?: name)?|agent(?: name)?|issued by|from");
            if (kind == "Agent")
            {
                Text("category", @"category|invoice category");
                Text("port", @"port(?: of call)?|port name");
                Date("due", @"due date|payment due(?: date)?");
                Amount("amount", @"grand total|invoice total|total amount(?: due)?|amount due|balance due|total(?: due)?");
            }
            else
            {
                Text("service", @"service(?: name| description)?|description");
                Text("reason", @"reason|remarks|purpose");
                Amount("cost", @"subtotal|sub total|net amount|cost|service cost|amount before tax");
                Amount("tax", @"tax(?: amount)?|vat(?: amount)?|gst(?: amount)?");
                if (!fields.ContainsKey("cost"))
                {
                    Amount("cost", @"grand total|invoice total|total amount(?: due)?|amount due|total");
                    if (fields.TryGetValue("cost", out var total) && fields.TryGetValue("tax", out var tax)) fields["cost"] = (decimal)total - (decimal)tax;
                }
            }
        }
        else
        {
            Text("invoiceNo", @"invoice(?:\s*(?:no\.?|number|#))|reference");
            Text("invoiceTo", @"invoice to|bill to|charterers?|customer");
            Date("invoiceDate", @"invoice date|date");
            Date("dueDate", @"due date|payment due(?: date)?");
            if (kind == "Freight")
            {
                Amount("blQtyOverride", @"(?:b/?l )?(?:quantity|qty)(?:\s*\(mt\))?", true);
                Amount("freightRateOverride", @"freight(?: rate)?(?:\s*\((?:pmt|usd/mt)\))?|rate per (?:mt|ton)", true);
                Amount("adcomOverride", @"address comm(?:ission)?(?:\s*\(%\))?|adcom", true);
                Amount("pctFreightDue", @"% freight due|freight due %", true);
            }
        }
        return fields;
    }
}