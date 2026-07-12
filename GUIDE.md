# Build guide: a multi-agent copilot with the Microsoft Agent Framework

Build `finance-copilot` from scratch: three service-agents coordinated by a controller brain, using
agent-as-tool delegation. The pattern applies to any domain where questions cross service
boundaries.

**Prerequisites:** .NET 10 SDK. For the live demo: an OpenAI/Azure OpenAI key (or a local
OpenAI-compatible endpoint).

---

## Step 0 — The idea

One giant agent holding every tool gets confused and context-heavy as tools multiply. Instead:

- Give each **bounded context** (fleet, trips, billing) its own agent with only its tools.
- Give a **coordinator** agent no domain tools at all — only the ability to call each specialist.
- Wrap each specialist agent **as a tool** so the coordinator composes them with ordinary
  function-calling. No custom router, no shared state.

---

## Step 1 — Scaffold

```bash
mkdir finance-copilot && cd finance-copilot
dotnet new classlib -o src/FinanceCopilot.Fleet
dotnet new classlib -o src/FinanceCopilot.Trips
dotnet new classlib -o src/FinanceCopilot.Billing
dotnet new console  -o src/FinanceCopilot.Brain
dotnet new xunit    -o tests/FinanceCopilot.Tests

# each service library:
dotnet add src/FinanceCopilot.Fleet package Microsoft.Agents.AI
dotnet add src/FinanceCopilot.Fleet package Microsoft.Extensions.AI
# the brain also needs the OpenAI bridge:
dotnet add src/FinanceCopilot.Brain package Microsoft.Extensions.AI.OpenAI
dotnet add src/FinanceCopilot.Brain package OpenAI
dotnet add src/FinanceCopilot.Brain reference src/FinanceCopilot.Fleet src/FinanceCopilot.Trips src/FinanceCopilot.Billing
```

> Version note: `Microsoft.Agents.AI` (1.13+) requires `Microsoft.Extensions.AI` 10.x — keep them
> on the same major line or NuGet reports a downgrade error.

---

## Step 2 — Stub tools with rich descriptions

Each service gets a tools class ([`FleetTools.cs`](./src/FinanceCopilot.Fleet/FleetTools.cs)). The
method bodies return synthetic data, but each method is a real, typed tool:

```csharp
[Description("Get the current status, location, and odometer for one vehicle by its id, e.g. 'VEH-101'.")]
public string GetVehicleStatus([Description("The vehicle id")] string vehicleId) => /* stub */;
```

The `[Description]` text is the contract the model reads — write it like docs for a junior dev.
Expose the methods as tools with `AIFunctionFactory.Create`:

```csharp
public AITool[] AsTools() =>
[
    AIFunctionFactory.Create(GetVehicleStatus),
    AIFunctionFactory.Create(GetMaintenanceDue),
    AIFunctionFactory.Create(GetFuelSummary),
];
```

Keep tools **narrow and read-only** — the model gets `GetVehicleStatus`, never "run SQL".

---

## Step 3 — Each service owns its agent

[`FleetService.cs`](./src/FinanceCopilot.Fleet/FleetService.cs) builds the agent from an injected
chat client, its instructions, and its tools:

```csharp
public static AIAgent CreateAgent(IChatClient chat) =>
    chat.AsAIAgent(new ChatClientAgentOptions
    {
        Name = "FleetAgent",
        ChatOptions = new ChatOptions { Instructions = Instructions, Tools = [.. new FleetTools().AsTools()] },
    });
```

The framework runs the tool-calling loop for you — no manual "did the model ask for a tool?" plumbing.

---

## Step 4 — The brain wraps each agent as a tool

In [`Program.cs`](./src/FinanceCopilot.Brain/Program.cs), create one chat client, build the three
specialists, then expose each **as a tool** by wrapping its `RunAsync`:

```csharp
IChatClient chat = new OpenAIClient(apiKey).GetChatClient(model).AsIChatClient();
AIAgent fleet = FleetService.CreateAgent(chat);

AITool askFleet = AIFunctionFactory.Create(
    async (string question) => (await fleet.RunAsync(question)).Text,
    name: "ask_fleet",
    description: "Consult the fleet specialist about vehicle status, maintenance, and fuel.");
```

Then the coordinator is just another agent whose tools are the three `ask_*` functions:

```csharp
AIAgent brain = chat.AsAIAgent(new ChatClientAgentOptions
{
    Name = "FinanceCopilotBrain",
    ChatOptions = new ChatOptions
    {
        Instructions = "Break the request into parts, consult the specialists as needed, then synthesize one grounded answer.",
        Tools = [askFleet, askTrips, askBilling],
    },
});
var answer = (await brain.RunAsync("Which vehicles are due for maintenance and are those customers overdue?")).Text;
```

The brain now decides — per question — which specialists to consult and how to merge them. Add a
service later and you add one `ask_*` tool; the brain adapts with no code change to routing.

---

## Step 5 — Test the deterministic layer

The stub tools need no model, so unit-test them directly
([`ToolsTests.cs`](./tests/FinanceCopilot.Tests/ToolsTests.cs)): maintenance-due lists only due
vehicles, an unknown id returns an error, an overdue invoice has negative `dueInDays`. Getting these
green means your capabilities are correct before any model is involved.

```bash
dotnet test
```

---

## Step 6 — Run the live copilot

```bash
export OPENAI_API_KEY=sk-...
dotnet run --project src/FinanceCopilot.Brain
```

---

## Where to take it next

- **Make the services real microservices** — host each behind a minimal API or an MCP server and
  have the `ask_*` tools call over the network. The brain doesn't change.
- **Swap the backend** — `AsAIAgent` is provider-agnostic; point it at Azure OpenAI or a local model.
- **Add guardrails** — wrap a write tool in an approval step so sensitive actions need confirmation.
- **Persist conversations** — give the brain a thread so multi-turn questions keep context.

## Takeaways

- Model each bounded context as its own narrow agent; give the coordinator no domain tools.
- Agent-as-tool turns orchestration into ordinary function-calling — composable and router-free.
- Keep tools narrow, typed, and stubbed behind an interface so the data source is swappable.
- Test the deterministic tool layer without a model; reserve the LLM for the orchestration demo.
