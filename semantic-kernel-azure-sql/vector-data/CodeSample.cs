using Microsoft.Extensions.VectorData;

public class CodeSample
{
    [VectorStoreKey]
    public int Id { get; set; }

    [VectorStoreData(IsIndexed = true)]
    public required string Url { get; set; }

    [VectorStoreData()]
    public required string Type { get; set; }

    [VectorStoreData(IsIndexed = true)]
    public required string Title { get; set; }

    [VectorStoreData()]
    public required string Description { get; set; }

    [VectorStoreVector(Dimensions: 1536, DistanceFunction = DistanceFunction.CosineDistance)]
    public string Embedding => this.Description;

    public static List<CodeSample> GetCodeSamples() => [
        new CodeSample
        {
            Id = 1,
            Url = "https://github.com/yorek/azure-sql-db-ai-samples-search",
            Title = "azure-sql-db-ai-samples-search",
            Type = "Repo",
            Description = "A practical sample of RAG pattern applied to a real-world use case: make finding samples using Azure SQL easier and more efficient!"
        },
        new CodeSample
        {
            Id = 2,
            Url = "https://github.com/Azure-Samples/azure-sql-nl2sql",
            Title = "azure-sql-nl2sql",
            Type = "Repo",
            Description = "Two-Experts Agent Model to generate high-quality SQL queries from Natural Language requests using Azure OpenAI and Azure SQL."           
        },
        new CodeSample
        {
            Id = 3,
            Url = "https://github.com/yorek/vslive2025-san-diego",
            Title = "VS Live 2025 San Diego Code Samples",
            Type = "Repo",
            Description = "Samples used at VS Live San Diego 2025 about SQL Server 2025, JSON, Semantic Kernel, Azure OpenAI, Azure SQL, and more."
        },
        new CodeSample
        {
            Id = 4,
            Url = "https://github.com/yorek/vslive2025-redmond",
            Title = "VS Live 2025 Redmond Code Samples",
            Type = "Repo",
            Description = "Samples used at VS Live Redmond 2025 about SQL Server 2025, JSON, Semantic Kernel, Azure OpenAI, Azure SQL, and more."
        },
        new CodeSample
        {
            Id = 5,
            Url = "https://github.com/Azure-Samples/azure-sql-db-vector-search/tree/main/DiskANN",
            Title = "Approximate Nearest Neighbor Search",
            Type = "Repo",
            Description = "This sample demonstrates how to use DiskANN with Azure SQL Database to perform efficient vector similarity searches on large datasets."
        },
        new CodeSample
        {
            Id = 6,
            Url = "https://github.com/yorek/fabric-conference-2025",
            Title = "Fabric Conference 2025 Code Samples",
            Type = "Repo",
            Description = "Demos and samples used a Fabric Conference 2025 Workshop \"Building AI applications with SQL: Ground to Cloud to Fabric\"."
        },
        new CodeSample
        {
            Id = 7,
            Url = "https://www.youtube.com/watch?v=H_2OgOL3fpo&t=982s",
            Title = "Migrate and modernize Windows Server, SQL Server, and .NET workloads",
            Type = "Video",
            Description = "Learn how to enhance and boost the performance, scalability and security of your SQL Server and .NET workloads, and be AI-ready by migrating and modernizing to Microsoft Azure."
        }
    ];
}