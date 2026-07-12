using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace FinanceCopilot.Billing;

public static class BillingService
{
    public const string Instructions =
        "You are the BILLING specialist. Answer questions about outstanding freight invoices, " +
        "settlements, and receivables aging using only your tools.";

    public static AIAgent CreateAgent(IChatClient chat) =>
        chat.AsAIAgent(new ChatClientAgentOptions
        {
            Name = "BillingAgent",
            ChatOptions = new ChatOptions { Instructions = Instructions, Tools = [.. new BillingTools().AsTools()] },
        });
}
