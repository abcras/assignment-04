using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Infrastructure.Tests;

public class UserRepositoryTests : IDisposable
{
    private readonly KanbanContext _context;
    private readonly IUserRepository _userRepository;

    public UserRepositoryTests()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();
        var builder = new DbContextOptionsBuilder<KanbanContext>().UseSqlite(connection);
        var context = new KanbanContext(builder.Options);
        context.Database.EnsureCreated();

        _context = context;
        _userRepository = new UserRepository(_context);

        _context.Users.AddRange(
            new User("rafa", "rafa"),
            new User("jouj", "jouj") { Items = new []{new WorkItem("task1", "test")}},
            new User("bemi", "bemi"));
        _context.SaveChanges();
    }

    [Fact]
    public void Create()
    {
        var user = new UserCreateDTO("Create", "Create");
        var (resp, uid) = _userRepository.Create(user);
        resp.Should().Be(Response.Created);
        uid.Should().Be(4);
    }

    [Fact]
    public void Create_returns_conflict_same_email()
    {
        var user = new UserCreateDTO("Create", "jouj");
        var (resp, uid) = _userRepository.Create(user);
        resp.Should().Be(Response.Conflict);
        uid.Should().Be(-1);
    }

    [Fact]
    public void ReadAll()
    {
        var expected = new[]
        {
            new UserDTO(1, "rafa", "rafa"),
            new UserDTO(2, "jouj", "jouj"),
            new UserDTO(3, "bemi", "bemi"),
        };
        var actual = _userRepository.Read();
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Read()
    {
        var expected = new UserDTO(1, "rafa", "rafa");
        var actual = _userRepository.Find(1);
        expected.Should().Be(actual);
    }

    [Fact]
    public void Read_returns_null_not_found()
    {
        _userRepository.Find(0).Should().BeNull();
    }

    [Fact]
    public void Update()
    {
        _userRepository.Update(new UserUpdateDTO(3, "test", "test")).Should().Be(Response.Updated);
    }

    [Fact]
    public void Update_not_found()
    {
        _userRepository.Update(new UserUpdateDTO(0, "test", "test")).Should().Be(Response.NotFound);
    }
    
    [Fact]
    public void Update_conflict()
    {
        _userRepository.Update(new UserUpdateDTO(0, "test", "jouj")).Should().Be(Response.Conflict);
    }

    [Fact]
    public void Delete()
    {
        _userRepository.Delete(1).Should().Be(Response.Deleted);
    }

    [Fact]
    public void Delete_not_found()
    {
        _userRepository.Delete(0).Should().Be(Response.NotFound);
    }

    [Fact]
    public void Delete_in_use_disallowed()
    {
        _userRepository.Delete(2).Should().Be(Response.Conflict);
    }

    [Fact]
    public void Delete_force()
    {
        _userRepository.Delete(2, true).Should().Be(Response.Deleted);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
