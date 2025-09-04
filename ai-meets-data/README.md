# VS Live 2025 - Redmond HQ - AI Samples

## Demo 1 - Vectors and Vector Index

Use DiskANN sample from [azure-sql-db-vector-search](https://github.com/Azure-Samples/azure-sql-db-vector-search/tree/main/DiskANN) repo.

## Demo 2 - Chat with your own data (RAG - Retrieval-Augmented Generation)

Use [azure-sql-db-chatbot](https://github.com/Azure-Samples/azure-sql-db-chatbot) repo. Implement a RAG solution and call OpenAI right from Azure SQL DB to ask questions about your data

## Demo 3 - Agentic RAG

Use then [azure-sql-db-ai-samples-search](https://github.com/yorek/azure-sql-db-ai-samples-search) repo. It shows A practical sample of Agentic-RAG pattern applied to a real-world use case: make finding samples using Azure SQL easier and more efficient! 

Run search in debug mode:

```sql
exec [web].[find_samples] 't-sql sample with llm structured output', @nocache=1, @debug=1
```

## Demo 4 - Multi Agent

Use [azure-sql-nl2sql](https://github.com/Azure-Samples/azure-sql-nl2sql) repo. It shows how to use Two-Experts Agent Model to generate high-quality SQL queries from Natural Language request

Use the following question as an example

```
Hi, can you help me figure out what is our best selling product in each market we operate, and what would be a cross selling opportunity with another product? 
```
