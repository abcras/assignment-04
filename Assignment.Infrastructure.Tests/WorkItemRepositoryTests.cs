using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Infrastructure.Tests;

public class TaskRepositoryTests : IDisposable
{
    private readonly KanbanContext _context;
    private readonly IWorkItemRepository _workItemRepository;

    public TaskRepositoryTests()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();
        var builder = new DbContextOptionsBuilder<KanbanContext>().UseSqlite(connection);
        var context = new KanbanContext(builder.Options);
        context.Database.EnsureCreated();

        _context = context;
        _workItemRepository = new WorkItemRepository(_context);

        //Tags
        var cleaning = new Tag("Cleaning") { Id = 1 };
        var urgent = new Tag("Urgent") { Id = 2 };
        var TBD = new Tag("TBD") { Id = 3 };
        _context.Tags.AddRange(cleaning, urgent, TBD);

        //Tasks
        var task1 = new WorkItem("Clean Office") { Id = 1, State = State.Active };
        var task2 = new WorkItem("Do Taxes") { Id = 2, State = State.New };
        var task3 = new WorkItem("Go For A Run") { Id = 3, State = State.Resolved };
        _context.Items.AddRange(task1, task2, task3);

        var user1 = new User("Brian", "br@itu.dk") { Id = 1 };
        _context.Users.Add(user1);

        _context.SaveChanges();
    }

    [Fact]
    public void Create_should_set_New_Created_and_StateUpdated()
    {
        var now = DateTime.UtcNow;
        var (response, taskId) = _workItemRepository.Create(new WorkItemCreateDTO("test", 1, "test", ArraySegment<string>.Empty));

        response.Should().Be(Response.Created);

        var task = _workItemRepository.Find(taskId);

        task.State.Should().Be(State.New);
        task.Created.Should().BeCloseTo(now, precision: TimeSpan.FromSeconds(5));
        task.StateUpdated.Should().BeCloseTo(now, precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Update_state_sets_StateUpdated()
    {
        var now = DateTime.UtcNow;
        var taskId = 1;

        var response =
            _workItemRepository.Update(new WorkItemUpdateDTO(taskId, "test", 1, "test", ArraySegment<string>.Empty, State.Closed));

        response.Should().Be(Response.Updated);

        var task = _workItemRepository.Find(taskId);
        task.StateUpdated.Should().BeCloseTo(now, precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_task_should_return_Created()
    {
        var list = new List<String> { "Cleaning", "Urgent" };
        var newTask = new WorkItemCreateDTO("NewTask", 1, "New Task that is something", list);

        var (response, id) = _workItemRepository.Create(newTask);
        response.Should().Be(Response.Created);

        id.Should().Be(new WorkItemDTO(4, "NewTask", "Brian", list, State.New).Id);
    }

    [Fact]
    public void Delete_task_that_is_new_should_return_deleted()
    {
        var response = _workItemRepository.Delete(2);
        response.Should().Be(Response.Deleted);
        _context.Items.Find(2).Should().BeNull();
    }

    [Fact]
    public void Delete_task_that_is_active_should_return_state_removed()
    {
        var response = _workItemRepository.Delete(1);
        _context.Items.Find(1)!.State.Should().Be(State.Removed);
    }

    [Fact]
    public void Delete_task_that_is_Resolved_should_return_Conflict()
    {
        var response = _workItemRepository.Delete(3);
        response.Should().Be(Response.Conflict);
        _context.Items.Find(3)!.State.Should().Be(State.Resolved);
    }

    [Fact]
    public void Update_task_should_give_updated_tags()
    {
        var list = new List<string> { "Urgent", "TBD" };
        var urgent = new Tag("Urgent") { Id = 2 };
        var TBD = new Tag("TBD") { Id = 3 };
        var listT = new List<Tag> { urgent, TBD };

        var updateTask = new WorkItemUpdateDTO(1, "Clean Office", 1, null, list, State.Active);

        var resp = _workItemRepository.Update(updateTask);
        resp.Should().Be(Response.Updated);

        _context.Items.Find(1)!.Tags.Should().BeSameAs(listT);
    }

    [Fact]
    public void Assign_user_that_does_not_exist_return_BadRequest()
    {
        var updateTask = new WorkItemUpdateDTO(1, "Clean Office", 100, null, new List<string>(), State.Active);

        var response = _workItemRepository.Update(updateTask);
        response.Should().Be(Response.BadRequest);

    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

