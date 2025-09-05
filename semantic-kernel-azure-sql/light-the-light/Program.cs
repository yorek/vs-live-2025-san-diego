// Import packages
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Azure.Identity;
using Azure.AI.OpenAI;
using Azure;
using DotNetEnv;
using System.Text.Json;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using VSLive.Samples.LightTheLight;

// Load .env
Env.Load();

// Populate values from your OpenAI deployment
var deploymentName = Env.GetString("AZURE_OPENAI_CHAT_DEPLOYMENT_NAME") ?? string.Empty;
var endpoint = Env.GetString("AZURE_OPENAI_ENDPOINT") ?? string.Empty;
var apiKey = Env.GetString("AZURE_OPENAI_KEY") ?? string.Empty;
var applicationInsightsConnectionString = Env.GetString("APPLICATION_INSIGHTS_CONNECTION_STRING") ?? string.Empty;

// Replace the connection string with your Application Insights connection string
var connectionString = applicationInsightsConnectionString;

var resourceBuilder = ResourceBuilder
    .CreateDefault()
    .AddService("TelemetryApplicationInsightsQuickstart");

// Enable model diagnostics with sensitive data.
AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);

using var traceProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddSource("Microsoft.SemanticKernel*")
    .AddAzureMonitorTraceExporter(options => options.ConnectionString = connectionString)
    .Build();

using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddMeter("Microsoft.SemanticKernel*")
    .AddAzureMonitorMetricExporter(options => options.ConnectionString = connectionString)
    .Build();

using var loggerFactory = LoggerFactory.Create(builder =>
{
    // Add OpenTelemetry as a logging provider
    builder.AddOpenTelemetry(options =>
    {
        options.SetResourceBuilder(resourceBuilder);
        options.AddAzureMonitorLogExporter(options => options.ConnectionString = connectionString);
        // Format log messages. This is default to false.
        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;
    });
    builder.SetMinimumLevel(LogLevel.Information);
});

// Create a kernel with Azure OpenAI chat completion
var builder = Kernel.CreateBuilder();

// Create the Azure OpenAI client
AzureOpenAIClient azureClient = apiKey switch
{
    null or "" => new(new Uri(endpoint), new DefaultAzureCredential()),
    _ => new(new Uri(endpoint), new AzureKeyCredential(apiKey))
};

// Add Azure OpenAI chat completion
builder.AddAzureOpenAIChatCompletion(deploymentName, azureClient);

// Add enterprise components
builder.Services.AddSingleton(loggerFactory);

// Build the kernel
Kernel kernel = builder.Build();

// Get the services
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// Welcome message
Console.WriteLine("💡 Light the Light ");
Console.WriteLine("🤖 I'm here to help you control your lights!");

// Add the lights plugin 
var lightsPlugin = new LightsPlugin();
kernel.Plugins.AddFromObject(lightsPlugin);

// List the available plugins
Console.WriteLine("🤖 Functions (or tools) I can use:");
foreach (var plugin in kernel.Plugins)
{
    foreach (var function in plugin)
    {
        Console.WriteLine($"\t{plugin.Name}.{function.Name}");
    }
}

// Enable planning
OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

// Start the web socket server
WebSocketServer webSocketServer = new(lightsPlugin);
Task webSocketServerTask = webSocketServer.StartAsync("http://localhost:5000");

// Create a history store the conversation
var history = new ChatHistory();

history.AddSystemMessage("""
You are a helpful home assistant. You can turn lights on and off, add new lights, and remove lights. Use the available functions to perform these actions.
If the user ask for something that is not related to lights, respond that you are a home assistant and can only help with lights.
""");

// Initiate a back-and-forth chat
string? userInput;
do
{
    // Check if cancellation was requested
    if (webSocketServer.IsCancellationRequested)
    {
        Console.WriteLine("🛑 Shutting down due to cancellation request...");
        break;
    }

    // Collect user input
    Console.Write("👤 > ");
    userInput = Console.ReadLine();
    
    if (userInput is "/exit" or "/quit")
    {
        break;
    }

    if (userInput is "/history")
    {
        foreach (var message in history)
        {
            Console.WriteLine($"HISTORY> {message.Role}: {message.Content}");

            if (message.Metadata != null && message.Metadata.Count > 0)
            {
                var metadataJson = JsonSerializer.Serialize(message.Metadata, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                Console.WriteLine($"HISTORY> Metadata:");
                Console.WriteLine(metadataJson);
            }        
        }

        continue;
    }

    if (!string.IsNullOrEmpty(userInput))
    {
        // Add user input
        history.AddUserMessage(userInput);

        // Get the streaming response from the AI
        Console.Write("🤔 thinking...");
        string fullResponse = string.Empty;
        bool responseStarted = false;
        await foreach (var streamingResult in chatCompletionService.GetStreamingChatMessageContentsAsync(
            history,
            executionSettings: openAIPromptExecutionSettings,
            kernel: kernel))
        {
            if (!string.IsNullOrEmpty(streamingResult.Content))
            {
                if (!responseStarted)
                {
                    Console.WriteLine();
                    Console.Write("🤖 > ");
                    responseStarted = true;
                }
            }
            else
            {
                Console.Write(".");
            }

            Console.Write(streamingResult.Content);
            fullResponse += streamingResult.Content;
        }
        Console.WriteLine();

        // Add the complete message from the agent to the chat history
        history.AddAssistantMessage(fullResponse);

        // Notify the web app about the light state change
        await webSocketServer.BroadcastLightUpdateAsync();
    }
} while (userInput is not null && !webSocketServer.IsCancellationRequested);

// Stop the web server
await webSocketServer.StopAsync();