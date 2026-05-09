using DotnetApp.Models;
using TaskStatus = DotnetApp.Models.TaskStatus;

namespace DotnetApp.Services;

public class BoardService
{
    private List<Project> _projects = new()
    {
        new Project { Id = 1, Name = "Website Redesign", Description = "Modernise the company website", Color = "#6366f1" },
        new Project { Id = 2, Name = "Mobile App", Description = "iOS and Android launch", Color = "#f59e0b" },
        new Project { Id = 3, Name = "API Gateway", Description = "Centralised API management layer", Color = "#10b981" },
    };

    private List<ProjectTask> _tasks = new()
    {
        new ProjectTask { Id = 1,  ProjectId = 1, Title = "Design system audit",      Description = "Review all existing components",     Status = TaskStatus.Done,       Priority = TaskPriority.High,   Assignee = "Alice" },
        new ProjectTask { Id = 2,  ProjectId = 1, Title = "Homepage redesign",        Description = "New hero section and nav",            Status = TaskStatus.InProgress, Priority = TaskPriority.High,   Assignee = "Bob" },
        new ProjectTask { Id = 3,  ProjectId = 1, Title = "Mobile responsiveness",    Description = "Fix breakpoints across pages",        Status = TaskStatus.Todo,       Priority = TaskPriority.Medium, Assignee = "Alice" },
        new ProjectTask { Id = 4,  ProjectId = 1, Title = "SEO meta tags",            Description = "Add og:tags and structured data",     Status = TaskStatus.Todo,       Priority = TaskPriority.Low,    Assignee = "" },
        new ProjectTask { Id = 5,  ProjectId = 2, Title = "Auth flow",                Description = "Login, signup, forgot password",      Status = TaskStatus.Done,       Priority = TaskPriority.High,   Assignee = "Carol" },
        new ProjectTask { Id = 6,  ProjectId = 2, Title = "Push notifications",       Description = "Firebase integration",               Status = TaskStatus.InProgress, Priority = TaskPriority.Medium, Assignee = "Dave" },
        new ProjectTask { Id = 7,  ProjectId = 2, Title = "App Store submission",     Description = "Prepare assets and metadata",         Status = TaskStatus.Todo,       Priority = TaskPriority.High,   Assignee = "Carol" },
        new ProjectTask { Id = 8,  ProjectId = 2, Title = "Offline mode",             Description = "Cache critical data locally",        Status = TaskStatus.Todo,       Priority = TaskPriority.Low,    Assignee = "" },
        new ProjectTask { Id = 9,  ProjectId = 3, Title = "Rate limiting",            Description = "Per-client request throttling",      Status = TaskStatus.Done,       Priority = TaskPriority.High,   Assignee = "Eve" },
        new ProjectTask { Id = 10, ProjectId = 3, Title = "JWT validation",           Description = "Centralise token verification",      Status = TaskStatus.InProgress, Priority = TaskPriority.High,   Assignee = "Eve" },
        new ProjectTask { Id = 11, ProjectId = 3, Title = "API docs",                 Description = "OpenAPI spec for all routes",        Status = TaskStatus.Todo,       Priority = TaskPriority.Medium, Assignee = "Frank" },
        new ProjectTask { Id = 12, ProjectId = 3, Title = "Request logging",          Description = "Structured logs to stdout",          Status = TaskStatus.Todo,       Priority = TaskPriority.Low,    Assignee = "" },
    };

    private int _nextProjectId = 4;
    private int _nextTaskId = 13;

    // ── Projects ────────────────────────────────────────────────────────────
    public IEnumerable<Project> GetProjects() => _projects;

    public Project? GetProject(int id) => _projects.FirstOrDefault(p => p.Id == id);

    public Project CreateProject(CreateProjectRequest req)
    {
        var project = new Project
        {
            Id = _nextProjectId++,
            Name = req.Name.Trim(),
            Description = req.Description.Trim(),
            Color = req.Color,
        };
        _projects.Add(project);
        return project;
    }

    public bool DeleteProject(int id)
    {
        var project = _projects.FirstOrDefault(p => p.Id == id);
        if (project is null) return false;
        _projects.Remove(project);
        _tasks.RemoveAll(t => t.ProjectId == id);
        return true;
    }

    // ── Tasks ────────────────────────────────────────────────────────────────
    public IEnumerable<ProjectTask> GetTasks(int? projectId = null, TaskStatus? status = null)
    {
        var q = _tasks.AsQueryable();
        if (projectId.HasValue) q = q.Where(t => t.ProjectId == projectId.Value);
        if (status.HasValue) q = q.Where(t => t.Status == status.Value);
        return q.OrderBy(t => t.Priority == TaskPriority.High ? 0 : t.Priority == TaskPriority.Medium ? 1 : 2);
    }

    public ProjectTask? GetTask(int id) => _tasks.FirstOrDefault(t => t.Id == id);

    public ProjectTask? CreateTask(CreateTaskRequest req)
    {
        if (_projects.All(p => p.Id != req.ProjectId)) return null;
        var task = new ProjectTask
        {
            Id = _nextTaskId++,
            ProjectId = req.ProjectId,
            Title = req.Title.Trim(),
            Description = req.Description.Trim(),
            Priority = req.Priority,
            Assignee = req.Assignee.Trim(),
        };
        _tasks.Add(task);
        return task;
    }

    public ProjectTask? UpdateTask(int id, UpdateTaskRequest req)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task is null) return null;

        if (req.Title is not null) task.Title = req.Title.Trim();
        if (req.Description is not null) task.Description = req.Description.Trim();
        if (req.Status.HasValue) task.Status = req.Status.Value;
        if (req.Priority.HasValue) task.Priority = req.Priority.Value;
        if (req.Assignee is not null) task.Assignee = req.Assignee.Trim();
        task.UpdatedAt = DateTime.UtcNow;
        return task;
    }

    public bool DeleteTask(int id)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task is null) return false;
        _tasks.Remove(task);
        return true;
    }

    // ── Stats ─────────────────────────────────────────────────────────────────
    public object GetStats(int? projectId = null)
    {
        var tasks = projectId.HasValue ? _tasks.Where(t => t.ProjectId == projectId.Value).ToList() : _tasks;
        return new
        {
            total = tasks.Count,
            todo = tasks.Count(t => t.Status == TaskStatus.Todo),
            inProgress = tasks.Count(t => t.Status == TaskStatus.InProgress),
            done = tasks.Count(t => t.Status == TaskStatus.Done),
            highPriority = tasks.Count(t => t.Priority == TaskPriority.High),
        };
    }

    // For test resets
    public void Reset()
    {
        _projects = new()
        {
            new Project { Id = 1, Name = "Website Redesign", Description = "Modernise the company website", Color = "#6366f1" },
            new Project { Id = 2, Name = "Mobile App", Description = "iOS and Android launch", Color = "#f59e0b" },
            new Project { Id = 3, Name = "API Gateway", Description = "Centralised API management layer", Color = "#10b981" },
        };
        _tasks = new()
        {
            new ProjectTask { Id = 1,  ProjectId = 1, Title = "Design system audit",   Status = TaskStatus.Done,       Priority = TaskPriority.High,   Assignee = "Alice" },
            new ProjectTask { Id = 2,  ProjectId = 1, Title = "Homepage redesign",     Status = TaskStatus.InProgress, Priority = TaskPriority.High,   Assignee = "Bob" },
            new ProjectTask { Id = 3,  ProjectId = 1, Title = "Mobile responsiveness", Status = TaskStatus.Todo,       Priority = TaskPriority.Medium, Assignee = "Alice" },
            new ProjectTask { Id = 5,  ProjectId = 2, Title = "Auth flow",             Status = TaskStatus.Done,       Priority = TaskPriority.High,   Assignee = "Carol" },
            new ProjectTask { Id = 6,  ProjectId = 2, Title = "Push notifications",    Status = TaskStatus.InProgress, Priority = TaskPriority.Medium, Assignee = "Dave" },
            new ProjectTask { Id = 9,  ProjectId = 3, Title = "Rate limiting",         Status = TaskStatus.Done,       Priority = TaskPriority.High,   Assignee = "Eve" },
            new ProjectTask { Id = 10, ProjectId = 3, Title = "JWT validation",        Status = TaskStatus.InProgress, Priority = TaskPriority.High,   Assignee = "Eve" },
        };
        _nextProjectId = 4;
        _nextTaskId = 13;
    }
}
