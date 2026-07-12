using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace FinanceCopilot.Trips;

/// <summary>The Trips microservice's stub tools: dispatch/ETA/revenue for shipments.</summary>
public sealed class TripsTools
{
    private static readonly object[] Trips =
    [
        new { id = "TRIP-5001", status = "In Transit", etaHours = 6.5, currentLocation = "NH-48, KM 212", vehicleId = "VEH-101" },
        new { id = "TRIP-5002", status = "Delivered",  etaHours = 0.0, currentLocation = "Consignee Dock", vehicleId = "VEH-104" },
        new { id = "TRIP-5003", status = "Delayed",    etaHours = 14.0, currentLocation = "Checkpoint 3",  vehicleId = "VEH-102" },
    ];

    [Description("Get the status, ETA (hours), and current location for one trip by id, e.g. 'TRIP-5001'.")]
    public string GetTripStatus([Description("The trip id")] string tripId)
    {
        var t = Array.Find(Trips, x => ((dynamic)x).id == tripId);
        return t is null ? Json(new { error = $"no trip '{tripId}'" }) : Json(t);
    }

    [Description("Summarise all active trips: how many are running and the on-time percentage.")]
    public string GetActiveTrips()
        => Json(new { active = 2, onTimePct = 78.5 });

    [Description("Get the freight revenue booked for one trip.")]
    public string GetTripRevenue([Description("The trip id")] string tripId)
        => Json(new { tripId, freightRevenue = 3125.00, currency = "USD" });

    public AITool[] AsTools() =>
    [
        AIFunctionFactory.Create(GetTripStatus),
        AIFunctionFactory.Create(GetActiveTrips),
        AIFunctionFactory.Create(GetTripRevenue),
    ];

    internal static string Json(object? value) => JsonSerializer.Serialize(value);
}
