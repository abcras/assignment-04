using System.Collections.Immutable;

namespace Assignment.Infrastructure;

public class UserRepository : IUserRepository
{
    private readonly DbContext _context;

    public UserRepository(DbContext context)
    {
        _context = context;
    }

    public (Response Response, int UserId) Create(UserCreateDTO user)
    {
        if (_context.Set<User>().FirstOrDefault(u => u.Email == user.Email) is not null)
        {
            return (Response.Conflict, -1);
        }

        var entry = _context.Set<User>().Add(new User(user.Email, user.Name));

        _context.SaveChanges();

        return (Response.Created, entry.Entity.Id);
    }

    public IReadOnlyCollection<UserDTO> Read()
        => _context.Set<User>().Select(u => new UserDTO(u.Id, u.Name, u.Email)).ToImmutableArray();

    public UserDTO Find(int userId)
        => _context.Set<User>().FirstOrDefault(u => u.Id == userId) is not { } entity
            ? null
            : new UserDTO(entity.Id, entity.Name, entity.Email);

    public Response Update(UserUpdateDTO user)
    {
        if (_context.Set<User>().FirstOrDefault(u => u.Email == user.Email) is not null)
        {
            return Response.Conflict;
        }

        if (_context.Set<User>().FirstOrDefault(u => u.Id == user.Id) is not { } entity)
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
        if (_context.Set<User>().FirstOrDefault(u => u.Id == userId) is not { } entity)
        {
            return Response.NotFound;
        }

        if (entity.Items?.Count > 0 && !force)
        {
            return Response.Conflict;
        }

        _context.Set<User>().Remove(entity);
        _context.SaveChanges();

        return Response.Deleted;
    }
}
