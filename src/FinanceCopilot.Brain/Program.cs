using FinanceCopilot.Billing;
using FinanceCopilot.Fleet;
using FinanceCopilot.Trips;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

// The controller "brain". It doesn't own any domain tools — it owns the three SPECIALIST AGENTS,
// each of which owns its own tools. Every specialist is wrapped as a single tool (ask_fleet /
// ask_trips / ask_billing) on the brain, so the brain can consult one or several and synthesize a
// combined answer. That is how the agents "coordinate": through the brain, which routes and merges.
//
// Requires an LLM backend: set OPENAI_API_KEY (and optionally LLM_MODEL). Without one, run the
// tests, which exercise the deterministic tool layer with no model.

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new("set OPENAI_API_KEY");
var model = Environment.GetEnvironmentVariable("LLM_MODEL") ?? "gpt-4o-mini";

IChatClient chat = new OpenAIClient(apiKey).GetChatClient(model).AsIChatClient();

// Each microservice builds its own agent (instructions + tools).
AIAgent fleet = FleetService.CreateAgent(chat);
AIAgent trips = TripsService.CreateAgent(chat);
AIAgent billing = BillingService.CreateAgent(chat);

// Expose each specialist agent to the brain AS A TOOL (agent-as-tool delegation).
AITool askFleet = AIFunctionFactory.Create(
    async (string question) => (await fleet.RunAsync(question)).Text,
    name: "ask_fleet",
    description: "Consult the fleet specialist about vehicle status, maintenance, and fuel.");
AITool askTrips = AIFunctionFactory.Create(
    async (string question) => (await trips.RunAsync(question)).Text,
    name: "ask_trips",
    description: "Consult the trips specialist about trip status, ETAs, on-time performance, and trip revenue.");
AITool askBilling = AIFunctionFactory.Create(
    async (string question) => (await billing.RunAsync(question)).Text,
    name: "ask_billing",
    description: "Consult the billing specialist about outstanding invoices, settlements, and receivables aging.");

AIAgent brain = chat.AsAIAgent(new ChatClientAgentOptions
{
    Name = "FinanceCopilotBrain",
    ChatOptions = new ChatOptions
    {
        Instructions =
            "You are a logistics finance copilot. Break the user's request into parts, consult the " +
            "specialist tools (ask_fleet, ask_trips, ask_billing) as needed — you may call more than " +
            "one — then synthesize a single, grounded answer. Do not invent data the tools didn't return.",
        Tools = [askFleet, askTrips, askBilling],
    },
});

foreach (var q in new[]
{
    "Which vehicles are due for maintenance?",
    "For any vehicle that is due for maintenance, is its customer's freight billing overdue?",
})
{
    Console.WriteLine($"\nUser: {q}");
    Console.WriteLine($"Copilot: {(await brain.RunAsync(q)).Text}");
}
