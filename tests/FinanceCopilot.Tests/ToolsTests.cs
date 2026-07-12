using System.Text.Json;
using FinanceCopilot.Billing;
using FinanceCopilot.Fleet;
using FinanceCopilot.Trips;
using Xunit;

namespace FinanceCopilot.Tests;

// The stub tools are deterministic and need no LLM, so they are unit-tested directly. The agent
// orchestration needs a model backend and is exercised via the Brain demo.
public class ToolsTests
{
    [Fact]
    public void Fleet_MaintenanceDue_ListsOnlyDueVehicles()
    {
        using var doc = JsonDocument.Parse(new FleetTools().GetMaintenanceDue());
        Assert.Equal(2, doc.RootElement.GetArrayLength()); // VEH-102 and VEH-103
    }

    [Fact]
    public void Fleet_VehicleStatus_ReturnsErrorForUnknownId()
    {
        using var doc = JsonDocument.Parse(new FleetTools().GetVehicleStatus("VEH-999"));
        Assert.True(doc.RootElement.TryGetProperty("error", out _));
    }

    [Fact]
    public void Trips_Status_ReturnsEtaForKnownTrip()
    {
        using var doc = JsonDocument.Parse(new TripsTools().GetTripStatus("TRIP-5003"));
        Assert.Equal("Delayed", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public void Billing_Outstanding_FlagsOverdueInvoices()
    {
        using var doc = JsonDocument.Parse(new BillingTools().GetOutstandingInvoices("Globex"));
        var invoices = doc.RootElement.GetProperty("invoices");
        Assert.Equal(1, invoices.GetArrayLength());
        Assert.True(invoices[0].GetProperty("dueInDays").GetInt32() < 0); // overdue
    }

    [Fact]
    public void Billing_UnknownCustomer_ReturnsEmpty()
    {
        using var doc = JsonDocument.Parse(new BillingTools().GetOutstandingInvoices("Nobody"));
        Assert.Equal(0, doc.RootElement.GetProperty("invoices").GetArrayLength());
    }

    [Fact]
    public void EachService_ExposesThreeTools()
    {
        Assert.Equal(3, new FleetTools().AsTools().Length);
        Assert.Equal(3, new TripsTools().AsTools().Length);
        Assert.Equal(3, new BillingTools().AsTools().Length);
    }
}
