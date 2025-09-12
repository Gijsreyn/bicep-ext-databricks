using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text.Json;
using Databricks.Handlers;
using Databricks.Models;

namespace Databricks.Extension.Tests.Handlers;

public abstract class HandlerTestBase<THandler, TResource, TIdentifiers>
    where THandler : DatabricksResourceHandlerBase<TResource, TIdentifiers>
    where TResource : class
    where TIdentifiers : class
{
    protected Mock<ILogger<THandler>> MockLogger { get; }
    protected Mock<HttpMessageHandler> MockHttpHandler { get; }
    protected HttpClient HttpClient { get; }
    
    protected HandlerTestBase()
    {
        MockLogger = new Mock<ILogger<THandler>>();
        MockHttpHandler = new Mock<HttpMessageHandler>();
        HttpClient = new HttpClient(MockHttpHandler.Object)
        {
            BaseAddress = new Uri("https://test.databricks.com/api/")
        };
    }

    protected Configuration CreateTestConfiguration()
    {
        return new Configuration
        {
            WorkspaceUrl = "https://test.databricks.com"
        };
    }

    protected HttpResponseMessage CreateSuccessResponse(object content)
    {
        var json = JsonSerializer.Serialize(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });
        
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    protected HttpResponseMessage CreateErrorResponse(HttpStatusCode statusCode, string message = "")
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(message, System.Text.Encoding.UTF8, "application/json")
        };
    }

    protected HttpResponseMessage CreateNotFoundResponse()
    {
        return CreateErrorResponse(HttpStatusCode.NotFound, """{"error_code": "RESOURCE_DOES_NOT_EXIST", "message": "Resource not found"}""");
    }
}
