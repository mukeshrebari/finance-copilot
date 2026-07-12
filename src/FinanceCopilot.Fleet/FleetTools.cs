using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace FinanceCopilot.Fleet;

/// <summary>
/// The Fleet microservice's tools. These are STUBS — they return synthetic data in place of real
/// telematics/maintenance systems — but each is a fully-formed, typed, LLM-callable tool. Swap the
/// bodies for real service calls and nothing above this layer changes.
/// </summary>
public sealed class FleetTools
{
    private static readonly object[] Vehicles =
    [
        new { id = "VEH-101", status = "In Transit", location = "NH-48, KM 212", odometerKm = 84210, maintenanceDue = false },
        new { id = "VEH-102", status = "Idle",       location = "Depot A",        odometerKm = 149980, maintenanceDue = true },
        new { id = "VEH-103", status = "In Workshop", location = "Workshop B",     odometerKm = 203050, maintenanceDue = true },
    ];

    [Description("Get the current status, location, and odometer for one vehicle by its id, e.g. 'VEH-101'.")]
    public string GetVehicleStatus([Description("The vehicle id")] string vehicleId)
    {
        var v = Array.Find(Vehicles, x => ((dynamic)x).id == vehicleId);
        return v is null ? Json(new { error = $"no vehicle '{vehicleId}'" }) : Json(v);
    }

    [Description("List every vehicle currently due for maintenance.")]
    public string GetMaintenanceDue()
        => Json(Array.FindAll(Vehicles, x => ((dynamic)x).maintenanceDue));

    [Description("Get fuel cost and efficiency for one vehicle.")]
    public string GetFuelSummary([Description("The vehicle id")] string vehicleId)
        => Json(new { vehicleId, litresThisMonth = 1820, costPerKm = 12.4, efficiencyKmpl = 3.6, currency = "USD" });

    /// <summary>Expose the methods above as LLM-callable tools.</summary>
    public AITool[] AsTools() =>
    [
        AIFunctionFactory.Create(GetVehicleStatus),
        AIFunctionFactory.Create(GetMaintenanceDue),
        AIFunctionFactory.Create(GetFuelSummary),
    ];

    internal static string Json(object? value) => JsonSerializer.Serialize(value);
}
