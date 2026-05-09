using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DotnetApp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetApp.Tests;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        factory.Services.GetRequiredService<BoardService>().Reset();
    }

    [Fact]
    public async Task Health_Returns_Ok()
    {
        var res = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task GetProjects_Returns_SeedData()
    {
        var res = await _client.GetAsync("/api/projects");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var projects = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(projects.GetArrayLength() >= 3);
    }

    [Fact]
    public async Task CreateTask_Returns_201()
    {
        var res = await _client.PostAsJsonAsync("/api/tasks", new
        {
            projectId = 1,
            title = "New test task",
            priority = 1,
            assignee = "Tester"
        });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task GetStats_Returns_Counts()
    {
        var res = await _client.GetAsync("/api/stats");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("total").GetInt32() >= 1);
    }
}
