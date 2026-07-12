using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace FinanceCopilot.Trips;

public static class TripsService
{
    public const string Instructions =
        "You are the TRIPS specialist. Answer questions about trip status, ETAs, on-time performance, " +
        "and trip revenue using only your tools.";

    public static AIAgent CreateAgent(IChatClient chat) =>
        chat.AsAIAgent(new ChatClientAgentOptions
        {
            Name = "TripsAgent",
            ChatOptions = new ChatOptions { Instructions = Instructions, Tools = [.. new TripsTools().AsTools()] },
        });
}
