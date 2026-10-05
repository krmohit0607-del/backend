using MultiTenantSaaS.Application.Services;

namespace MultiTenantSaaS.UnitTests.Services;

public class InvoiceDocumentParserTests
{
    [Theory]
    [InlineData("PDA", "estimated")]
    [InlineData("FDA", "fdaFinal")]
    public void MapsDocumentTotalToSelectedDisbursement(string kind, string field)
    {
        var fields = InvoiceDocumentParser.Extract("Port: Qingdao\nAgent: Harbor Agency\nDue Date: 12/10/2026\nCurrency: USD\nGrand Total: 12,345.67", kind);
        Assert.Equal(12345.67m, fields[field]);
        Assert.Equal("Qingdao", fields["port"]);
        Assert.Equal("12-10-2026", fields["due"]);
        Assert.False(fields.ContainsKey("status"));
        Assert.False(fields.ContainsKey(kind == "FDA" ? "estimated" : "fdaFinal"));
    }

    [Fact]
    public void ExtractsAgentInvoiceWithoutChangingPaymentState()
    {
        var fields = InvoiceDocumentParser.Extract("Invoice No:INV-123\nVendor: Harbor Agency\nAmount Due: USD 1,250.50\nDue Date: 2026-10-12", "Agent");
        Assert.Equal("INV-123", fields["invoiceNo"]);
        Assert.Equal(1250.50m, fields["amount"]);
        Assert.False(fields.ContainsKey("paid"));
        Assert.False(fields.ContainsKey("approved"));
    }

    [Fact]
    public void ServiceCostExcludesTaxFromGrandTotal()
    {
        var fields = InvoiceDocumentParser.Extract("Service: Survey\nTax: 20.00\nGrand Total: 120.00", "Service");
        Assert.Equal(100m, fields["cost"]);
        Assert.Equal(20m, fields["tax"]);
    }

    [Fact]
    public void FreightExtractsEditableFieldsWithoutOverridingVoyageTerms()
    {
        var fields = InvoiceDocumentParser.Extract("Invoice No: F-1\nInvoice To: ACCT1\nInvoice Date: 2 October 2026\nB/L Quantity (MT): 50,000\nFreight Rate: 22\nPayment Terms: Within 90 days", "Freight");
        Assert.Equal("50000", fields["blQtyOverride"]);
        Assert.Equal("22", fields["freightRateOverride"]);
        Assert.Equal("02-10-2026", fields["invoiceDate"]);
        Assert.False(fields.ContainsKey("paymentTerms"));
    }

    [Fact]
    public void DoesNotInventFieldsForUnrecognisedText()
    {
        Assert.Empty(InvoiceDocumentParser.Extract("An unlabelled document with no invoice details", "Agent"));
    }

    [Fact]
    public void ParsesEuropeanAmountAndKeepsExplicitZeroTax()
    {
        var fields = InvoiceDocumentParser.Extract("Cost: EUR 1.234,56\nTax: 0", "Service");
        Assert.Equal(1234.56m, fields["cost"]);
        Assert.Equal(0m, fields["tax"]);
    }

    [Fact]
    public void ReadsSeparatePdfLabelAndValueCells()
    {
        var fields = InvoiceDocumentParser.Extract("Invoice No:\tINV-456\nVendor\nHarbor Agency\nGrand Total:\tUSD 100.00", "Agent");
        Assert.Equal("INV-456", fields["invoiceNo"]);
        Assert.Equal("Harbor Agency", fields["vendor"]);
        Assert.Equal(100m, fields["amount"]);
    }

    [Fact]
    public void TaxPercentIsNotTheTaxAmount()
    {
        var fields = InvoiceDocumentParser.Extract("Subtotal: 1000\nTax: 20% USD 200.00", "Service");
        Assert.Equal(200m, fields["tax"]);
    }
}