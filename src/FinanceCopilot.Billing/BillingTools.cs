using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace FinanceCopilot.Billing;

/// <summary>The Billing microservice's stub tools: freight invoices, settlements, receivables aging.</summary>
public sealed class BillingTools
{
    private static readonly Dictionary<string, object[]> Outstanding = new()
    {
        ["Acme"] = [ new { invoiceNo = "INV-9001", amount = 4200.00, dueInDays = -3 }, new { invoiceNo = "INV-9014", amount = 1875.50, dueInDays = 12 } ],
        ["Globex"] = [ new { invoiceNo = "INV-9007", amount = 960.00, dueInDays = -21 } ],
    };

    [Description("List outstanding freight invoices for a customer, e.g. 'Acme'. Negative dueInDays means overdue.")]
    public string GetOutstandingInvoices([Description("Customer name")] string customer)
        => Outstanding.TryGetValue(customer, out var rows)
            ? Json(new { customer, invoices = rows })
            : Json(new { customer, invoices = Array.Empty<object>() });

    [Description("Summarise settlements: settled vs pending amounts across all customers.")]
    public string GetSettlementSummary()
        => Json(new { settled = 128400.00, pending = 24160.50, currency = "USD" });

    [Description("Get the receivables aging report in standard buckets.")]
    public string GetAgingReport()
        => Json(new { current = 18200.00, d1_30 = 6035.50, d31_60 = 960.00, d60_plus = 0.0, currency = "USD" });

    public AITool[] AsTools() =>
    [
        AIFunctionFactory.Create(GetOutstandingInvoices),
        AIFunctionFactory.Create(GetSettlementSummary),
        AIFunctionFactory.Create(GetAgingReport),
    ];

    internal static string Json(object? value) => JsonSerializer.Serialize(value);
}
