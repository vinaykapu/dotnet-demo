using DotnetApp.Models;
using DotnetApp.Services;
using TaskStatus = DotnetApp.Models.TaskStatus;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<BoardService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseDefaultFiles();
app.UseStaticFiles();


app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    uptime = Environment.TickCount64 / 1000.0
}));

// ── Projects ──────────────────────────────────────────────────────────────────
app.MapGet("/api/projects", (BoardService svc) =>
    Results.Ok(svc.GetProjects()));

app.MapGet("/api/projects/{id}", (int id, BoardService svc) =>
    svc.GetProject(id) is { } p ? Results.Ok(p) : Results.NotFound(new { error = "Project not found" }));

app.MapPost("/api/projects", (CreateProjectRequest req, BoardService svc) =>
{
    if (string.IsNullOrWhiteSpace(req.Name))
        return Results.BadRequest(new { error = "name is required" });
    var project = svc.CreateProject(req);
    return Results.Created($"/api/projects/{project.Id}", project);
});

app.MapDelete("/api/projects/{id}", (int id, BoardService svc) =>
    svc.DeleteProject(id) ? Results.Ok(new { message = "Project deleted" }) : Results.NotFound(new { error = "Project not found" }));

// ── Tasks ─────────────────────────────────────────────────────────────────────
app.MapGet("/api/tasks", (BoardService svc, int? projectId, string? status) =>
{
    TaskStatus? parsedStatus = null;
    if (status is not null)
    {
        if (!Enum.TryParse<TaskStatus>(status, true, out var s))
            return Results.BadRequest(new { error = "Invalid status. Use: Todo, InProgress, Done" });
        parsedStatus = s;
    }
    return Results.Ok(svc.GetTasks(projectId, parsedStatus));
});

app.MapGet("/api/tasks/{id}", (int id, BoardService svc) =>
    svc.GetTask(id) is { } t ? Results.Ok(t) : Results.NotFound(new { error = "Task not found" }));

app.MapPost("/api/tasks", (CreateTaskRequest req, BoardService svc) =>
{
    if (string.IsNullOrWhiteSpace(req.Title))
        return Results.BadRequest(new { error = "title is required" });
    var task = svc.CreateTask(req);
    return task is null
        ? Results.BadRequest(new { error = "Invalid projectId" })
        : Results.Created($"/api/tasks/{task.Id}", task);
});

app.MapPut("/api/tasks/{id}", (int id, UpdateTaskRequest req, BoardService svc) =>
{
    var task = svc.UpdateTask(id, req);
    return task is null ? Results.NotFound(new { error = "Task not found" }) : Results.Ok(task);
});

app.MapDelete("/api/tasks/{id}", (int id, BoardService svc) =>
    svc.DeleteTask(id) ? Results.Ok(new { message = "Task deleted" }) : Results.NotFound(new { error = "Task not found" }));

// ── Stats ─────────────────────────────────────────────────────────────────────
app.MapGet("/api/stats", (BoardService svc, int? projectId) =>
    Results.Ok(svc.GetStats(projectId)));

app.Run();

public partial class Program { }
