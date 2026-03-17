using Microsoft.SemanticKernel;
using Orleans.Configuration;
using Yassi.Agents;
using Yassi.Contracts;
using Yassi.LlmClient;
using Yassi.Mcp;
using Yassi.Orchestrator;

namespace Yassi.Api;

public partial class Program
{
    private static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // ── Config ──
        string groqKey = builder.Configuration["Groq:ApiKey"]!;
        string braveKey = builder.Configuration["Brave:ApiKey"]!;
        string serperKey = builder.Configuration["Serper:ApiKey"]!;
        string tavilyKey = builder.Configuration["Tavily:ApiKey"]!;

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

        builder.Services.AddSingleton(sp =>
            new SerperSearchTool(sp.GetRequiredService<IHttpClientFactory>().CreateClient(), serperKey));

        builder.Services.AddSingleton(sp =>
            new TavilySearchTool(sp.GetRequiredService<IHttpClientFactory>().CreateClient(), tavilyKey));

        builder.Services.AddSingleton<DuckDuckGoTool>();  // no key needed

        builder.Services.AddSingleton<SearchAgent>(sp =>
            new SearchAgent(
                sp.GetRequiredService<TavilySearchTool>(),
                sp.GetRequiredService<GroqClient>()));

        builder.Services.AddSingleton<CodeAgent>();
        builder.Services.AddSingleton<OrchestratorService>();

        // ── Semantic Kernel (Groq is OpenAI-compatible) ──
        builder.Services.AddSingleton(sp =>
        {
            HttpClient http = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
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

        WebApplication app = builder.Build();
        app.UseCors();

        // ── Chat endpoint ──
        app.MapPost("/chat", async (ChatRequest req, OrchestratorService orchestrator, CancellationToken ct) =>
        {
            AgentRequest agentReq = new(
                req.ConversationId ?? Guid.NewGuid().ToString(),
                req.Message,
                []);

            AgentResponse response = await orchestrator.HandleAsync(agentReq, ct);
            return Results.Ok(response);
        });

        // ── Health check ──
        app.MapGet("/health", () => "Yassi is alive");

        app.Run();
    }
}

internal record ChatRequest(string Message, string? ConversationId);