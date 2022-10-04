using System.Linq;

namespace Assignment.Infrastructure;

public class WorkItemRepository : IWorkItemRepository
{
    private readonly DbContext _context;

    public WorkItemRepository(DbContext context)
    {
        _context = context;
    }

    public (Response Response, int ItemId) Create(WorkItemCreateDTO task)
    {
        var entity = _context.Set<WorkItem>().FirstOrDefault(c => c.Title == task.Title);
        Response response;

        if (entity is null)
        {
            entity = new WorkItem(task.Title, task.Description!);

            _context.Set<WorkItem>().Add(entity);
            _context.SaveChanges();

            response = Response.Created;
        }
        else
        {
            response = Response.Conflict;
        }
        return (response, entity.Id);
    }

    public Response Delete(int itemId)
    {
        var entity = _context.Set<WorkItem>().FirstOrDefault(c => c.Id == itemId);

        switch (entity?.State)
        {
            case State.Active:
                entity.State = State.Removed;
                _context.SaveChanges();
                return Response.Updated;
            case State.Resolved:
            case State.Closed:
            case State.Removed:
                return Response.Conflict;
            case State.New:
                _context.Set<WorkItem>().Remove(entity);
                _context.SaveChanges();
                return Response.Deleted;
            case null:
                return Response.NotFound;
            default:
                throw new NotImplementedException("State variant not implemented");
        }
    }

    public WorkItemDetailsDTO Find(int itemId)
    {
        var entity = _context.Set<WorkItem>().Where(o => o.Id == itemId).First();

        WorkItemDetailsDTO detailsDTO =
            new WorkItemDetailsDTO(
            entity.Id,
            entity.Title,
            entity.Description,
            entity.Created,
            entity.AssignedTo.Name,
            entity.Tags.Select(c => c.Name).ToList(),
            entity.State,
            entity.StateUpdated
            );
        //This is slightly broken because WorkItem does not have a description nor a Created time nor a StateUpdated time

        return detailsDTO;
    }

    public IReadOnlyCollection<WorkItemDTO> Read()
    {
        return _context.Set<WorkItem>().Select(o => new WorkItemDTO(o.Id, o.Title, o.AssignedTo.Name, o.Tags.Select(c => c.Name).ToList(), o.State)).ToList();
    }

    public IReadOnlyCollection<WorkItemDTO> ReadByState(State state)
    {
        return _context.Set<WorkItem>().Where(o => o.State == state).Select(o => new WorkItemDTO(o.Id, o.Title, o.AssignedTo.Name, o.Tags.Select(c => c.Name).ToList(), o.State)).ToList();
    }

    public IReadOnlyCollection<WorkItemDTO> ReadByTag(string tag)
    {
        return _context.Set<WorkItem>().Where(t => t.Tags.Any( o => o.Name == tag)).Select(o => new WorkItemDTO(o.Id, o.Title, o.AssignedTo.Name, o.Tags.Select(c => c.Name).ToList(), o.State)).ToList();
    }

    public IReadOnlyCollection<WorkItemDTO> ReadByUser(int userId)
    {
        return _context.Set<WorkItem>().Where(t => t.AssignedToId == userId).Select(o => new WorkItemDTO(o.Id, o.Title, o.AssignedTo.Name, o.Tags.Select(c => c.Name).ToList(), o.State)).ToList();
    }

    public IReadOnlyCollection<WorkItemDTO> ReadRemoved()
    {
        return _context.Set<WorkItem>().Where(o => o.State == State.Removed).Select(o => new WorkItemDTO(o.Id, o.Title, o.AssignedTo.Name, o.Tags.Select(c => c.Name).ToList(), o.State)).ToList();
    }

    public Response Update(WorkItemUpdateDTO task)
    {
        var entity = _context.Set<WorkItem>().Find(task.Id);
        Response response;

        if (entity is null)
        {
            response = Response.NotFound;
        }
        //if two tasks exists with the same titles but different ids
        else if (_context.Set<WorkItem>().FirstOrDefault(t => t.Id != task.Id && t.Title == task.Title) != null)
        {
            response = Response.Conflict;
        }
        else if (_context.Set<User>().Find(task.AssignedToId) is null)
        {
            response = Response.BadRequest;
        }
        else
        {
            entity.AssignedTo = task.AssignedToId is not null ? _context.Set<User>().Find(task.AssignedToId) : entity.AssignedTo;
            //entity.Description = task.Description is not null ? task.Description : entity.Description;

            if (task.Tags is not null)
            {
                entity.Tags = new List<Tag>();
                foreach (var tag in task.Tags!)
                {
                    foreach (var conTag in _context.Set<Tag>())
                    {
                        if (conTag.Name == tag)
                        {
                            entity.Tags.Add(_context.Set<Tag>().Find(conTag.Id));
                        }
                    }
                }
            }

            entity.State = task.State;
            entity.Title = task.Title;
            _context.SaveChanges();
            response = Response.Updated;
        }

        return response;
    }
}
