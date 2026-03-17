using Orleans;
using Orleans.Configuration;
using Yassi.Agents;
using Yassi.LlmClient;
using Yassi.Mcp;
using Yassi.Orchestrator;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

// ── Config ──
var groqKey = builder.Configuration["Groq:ApiKey"]!;
var braveKey = builder.Configuration["Brave:ApiKey"]!;

// ── Orleans Client ──
builder.Host.UseOrleansClient(client =>
{
    client.UseLocalhostClustering();
    client.Configure<ClusterOptions>(o =>
    {
        o.ClusterId = "yassi-dev";
        o.ServiceId = "YassiService";
    });
});

// ── HTTP clients ──
builder.Services.AddHttpClient<GroqClient>();
builder.Services.AddHttpClient<BraveSearchTool>();

// ── App services ──
builder.Services.AddSingleton(sp =>
    new GroqClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient(), groqKey));

builder.Services.AddSingleton(sp =>
    new BraveSearchTool(sp.GetRequiredService<IHttpClientFactory>().CreateClient(), braveKey));

builder.Services.AddSingleton<SearchAgent>();
builder.Services.AddSingleton<CodeAgent>();
builder.Services.AddSingleton<OrchestratorService>();

// ── Semantic Kernel (Groq is OpenAI-compatible) ──
builder.Services.AddSingleton(sp =>
{
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    return Kernel.CreateBuilder()
        .AddOpenAIChatCompletion(
            modelId: "llama3-8b-8192",
            apiKey: groqKey,
            httpClient: http,
            endpoint: new Uri("https://api.groq.com/openai/v1"))
        .Build();
});

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();

// ── Chat endpoint ──
app.MapPost("/chat", async (ChatRequest req, OrchestratorService orchestrator, CancellationToken ct) =>
{
    var agentReq = new Yassi.Contracts.AgentRequest(
        req.ConversationId ?? Guid.NewGuid().ToString(),
        req.Message,
        []);

    var response = await orchestrator.HandleAsync(agentReq, ct);
    return Results.Ok(response);
});

// ── Health check ──
app.MapGet("/health", () => "Yassi is alive");

app.Run();

record ChatRequest(string Message, string? ConversationId);