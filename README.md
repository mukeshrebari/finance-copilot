# finance-copilot

**A multi-agent logistics finance copilot built on the [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/): a controller "brain" agent that coordinates three specialist service-agents, each owning its own tools.**

Real operational questions cross service boundaries — *"which trucks are due for service, and are those customers behind on their freight bills?"* answers only if fleet data and billing data are consulted together. This repo models that with one coordinator agent orchestrating three independent service-agents, so each stays focused and independently deployable while the brain composes their answers.

---

## Architecture

```
                                   ┌──────────────────────────────┐
   user question ─────────────────▶│   FinanceCopilotBrain        │  (controller / coordinator)
                                   │   tools: ask_fleet,          │
                                   │          ask_trips,          │
                                   │          ask_billing         │
                                   └───┬───────────┬───────────┬──┘
                          ask_fleet ▼           ▼           ▼ ask_billing
                        ┌───────────────┐ ┌───────────┐ ┌────────────────┐
                        │  FleetAgent   │ │ TripsAgent│ │  BillingAgent  │   (3 microservices,
                        │  GetVehicle…  │ │ GetTrip…  │ │  GetOutstanding│    each = agent + tools)
                        │  Maintenance… │ │ ActiveTr… │ │  Settlement…   │
                        │  FuelSummary  │ │ TripRev…  │ │  AgingReport   │
                        └───────────────┘ └───────────┘ └────────────────┘
```

- **Three microservices**, each a class library that owns an agent (`FleetService`, `TripsService`,
  `BillingService`). Each agent has a focused set of **stub tool methods** — synthetic stand-ins for
  real telematics / dispatch / billing systems — exposed via `AIFunctionFactory`.
- **One controller brain** (`FinanceCopilot.Brain`) that holds *no* domain tools. Instead each
  specialist agent is wrapped as a single tool (`ask_fleet` / `ask_trips` / `ask_billing`), so the
  brain can consult one or several and synthesize a combined answer. That wrapping — an agent
  exposed as a callable function — is how the agents **coordinate**: through the brain, which routes
  and merges.

## Why this shape

| Decision | Why |
|----------|-----|
| **Agent-as-tool delegation** | The brain calls `ask_fleet(question)` exactly like any other tool; the framework's function-calling loop decides when and how often. Composition without a bespoke router. |
| **Coordinator holds no domain tools** | It only orchestrates. Each domain's tools live with its service — least privilege and clean bounded contexts. |
| **Specialist agents are narrow** | A fleet agent that only sees fleet tools makes fewer wrong tool choices and carries less context than one agent holding all nine tools. |
| **Tools are stubs behind a real interface** | Swap a stub body for a real service/HTTP/MCP call and nothing above the tool changes. The orchestration is what's being demonstrated, not the data source. |
| **Microsoft Agent Framework** | `IChatClient.AsAIAgent(...)` + `AIFunctionFactory.Create(...)` give provider-agnostic agents and tools; the same code runs against OpenAI, Azure OpenAI, or a local model. |

## Project layout

```
src/FinanceCopilot.Fleet      # vehicle status, maintenance-due, fuel  (agent + tools)
src/FinanceCopilot.Trips      # trip status, ETA, on-time %, revenue   (agent + tools)
src/FinanceCopilot.Billing    # outstanding invoices, settlements, aging (agent + tools)
src/FinanceCopilot.Brain      # the coordinator: wraps each agent as a tool, runs the demo
tests/FinanceCopilot.Tests    # deterministic tests of the stub tool layer (no LLM)
```

## Run it

```bash
# The deterministic tool layer needs no model:
dotnet test

# The full multi-agent copilot needs an LLM backend:
export OPENAI_API_KEY=sk-...        # or configure Azure OpenAI / a local OpenAI-compatible endpoint
dotnet run --project src/FinanceCopilot.Brain
```

The demo asks a single-service question ("which vehicles are due for maintenance?") and a
cross-service one ("for any vehicle due for maintenance, is its customer's billing overdue?") —
watch the brain consult `ask_fleet` then `ask_billing` and merge the result.

See **[GUIDE.md](./GUIDE.md)** to build it from scratch and add a fourth service.
