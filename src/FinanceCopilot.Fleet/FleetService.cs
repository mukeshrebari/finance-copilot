using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace FinanceCopilot.Fleet;

/// <summary>The Fleet microservice owns its agent: its instructions and its tool surface.</summary>
public static class FleetService
{
    public const string Instructions =
        "You are the FLEET specialist. Answer questions about vehicle status, location, maintenance, " +
        "and fuel using only your tools. Never invent a vehicle or a figure.";

    public static AIAgent CreateAgent(IChatClient chat) =>
        chat.AsAIAgent(new ChatClientAgentOptions
        {
            Name = "FleetAgent",
            ChatOptions = new ChatOptions { Instructions = Instructions, Tools = [.. new FleetTools().AsTools()] },
        });
}
