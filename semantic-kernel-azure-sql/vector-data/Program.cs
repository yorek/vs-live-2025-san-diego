using System.Text;
using Microsoft.SemanticKernel.Connectors.SqlServer;
using DotNetEnv;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Azure.AI.OpenAI;
using Azure;

Env.Load();

var azureOpenAIEndpoint = new Uri(Env.GetString("OPENAI_URL"));
var azureOpenAIApiKey = Env.GetString("OPENAI_KEY") ?? string.Empty;
var embeddingModelDeploymentName = Env.GetString("OPENAI_EMBEDDING_DEPLOYMENT_NAME");
var sqlConnectionString = Env.GetString("MSSQL_CONNECTION_STRING");

// If any of the required environment variables are missing, throw an exception
if (azureOpenAIEndpoint == null)
{
    throw new InvalidOperationException("The OPENAI_URL environment variable is not set.");
}
if (string.IsNullOrWhiteSpace(embeddingModelDeploymentName))
{
    throw new InvalidOperationException("The OPENAI_EMBEDDING_DEPLOYMENT_NAME environment variable is not set.");
}
if (string.IsNullOrWhiteSpace(sqlConnectionString))
{
    throw new InvalidOperationException("The MSSQL_CONNECTION_STRING environment variable is not set.");
}

var openAIClient = azureOpenAIApiKey switch
{
    null or "" => new AzureOpenAIClient(azureOpenAIEndpoint, new DefaultAzureCredential()),
    _ => new AzureOpenAIClient(azureOpenAIEndpoint, new AzureKeyCredential(azureOpenAIApiKey))
};

var embeddingGenerator = openAIClient.GetEmbeddingClient(embeddingModelDeploymentName).AsIEmbeddingGenerator();

Console.WriteLine("Connecting to the database vector store...");
var vectorStore = new SqlServerVectorStore(sqlConnectionString, new SqlServerVectorStoreOptions() { EmbeddingGenerator = embeddingGenerator });
var collection = vectorStore.GetCollection<int, CodeSample>("dbo.CodeSamples");
await collection.EnsureCollectionExistsAsync();

Console.WriteLine("Populating vector store...");
var codeSamples = CodeSample.GetCodeSamples();
var tasks = codeSamples.Select(item => Task.Run(async () =>
{
    item.Embedding = await embeddingGenerator.GenerateVectorAsync(item.Description);
    Console.WriteLine($"Generated embedding for Id: {item.Id}, Title: {item.Title}");
}));
await Task.WhenAll(tasks);
await collection.UpsertAsync(codeSamples);

Console.WriteLine("Searching vector store...");
var question = "Question: What is the repo that contains the samples used at VS Live at Microsoft Headquarters?";
Console.WriteLine(question);
var searchResult = collection.SearchAsync(
    question,    
    top: 3,
    new VectorSearchOptions<CodeSample>()
    {
        Filter = item => item.Type == "Repo"
    });

// Output the matching result.
await foreach (var result in searchResult)
{
    if (result.Score < 0.5)
    {
        Console.WriteLine($"Id: {result.Record.Id}, Title: {result.Record.Title}, Score: {result.Score}");
    }
}