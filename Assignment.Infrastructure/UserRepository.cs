using System.Collections.Immutable;

namespace Assignment3.Entities;

public class UserRepository : IUserRepository
{
    private readonly KanbanContext _context;

    public UserRepository(KanbanContext context)
    {
        _context = context;
    }

    public (Response Response, int UserId) Create(UserCreateDTO user)
    {
        if (_context.Users.FirstOrDefault(u => u.Email == user.Email) is not null)
        {
            return (Response.Conflict, -1);
        }

        var entry = _context.Users.Add(new User
        {
            Email = user.Email,
            Name = user.Name,
        });

        _context.SaveChanges();

        return (Response.Created, entry.Entity.Id);
    }

    public IReadOnlyCollection<UserDTO> ReadAll()
        => _context.Users.Select(u => new UserDTO(u.Id, u.Name, u.Email)).ToImmutableArray();

    public UserDTO Read(int userId)
        => _context.Users.FirstOrDefault(u => u.Id == userId) is not { } entity
            ? null
            : new UserDTO(entity.Id, entity.Name, entity.Email);

    public Response Update(UserUpdateDTO user)
    {
        if (_context.Users.FirstOrDefault(u => u.Email == user.Email) is not null)
        {
            return Response.Conflict;
        }

        if (_context.Users.FirstOrDefault(u => u.Id == user.Id) is not { } entity)
        {
            return Response.NotFound;
        }

        entity.Email = user.Email;
        entity.Name = user.Name;
        _context.SaveChanges();

        return Response.Updated;
    }

    public Response Delete(int userId, bool force = false)
    {
        if (_context.Users.FirstOrDefault(u => u.Id == userId) is not { } entity)
        {
            return Response.NotFound;
        }

        if (entity.Tasks?.Count > 0 && !force)
        {
            return Response.Conflict;
        }

        _context.Users.Remove(entity);
        _context.SaveChanges();

        return Response.Deleted;
    }
}