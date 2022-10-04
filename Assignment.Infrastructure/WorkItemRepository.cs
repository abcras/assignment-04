using System.Linq;

namespace Assignment.Infrastructure;

public class WorkItemRepository : IWorkItemRepository
{
    KanbanContext context;

    public WorkItemRepository(KanbanContext context)
    {
        this.context = context;
    }

    public (Response Response, int ItemId) Create(WorkItemCreateDTO task)
    {
        var entity = context.Items.FirstOrDefault(c => c.Title == task.Title);
        Response response;

        if (entity is null)
        {
            entity = new WorkItem(task.Title, task.Description!);

            context.Items.Add(entity);
            context.SaveChanges();

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
        var entity = context.Items.FirstOrDefault(c => c.Id == itemId);

        switch (entity?.State)
        {
            case State.Active:
                entity.State = State.Removed;
                context.SaveChanges();
                return Response.Updated;
            case State.Resolved:
            case State.Closed:
            case State.Removed:
                return Response.Conflict;
            case State.New:
                context.Items.Remove(entity);
                context.SaveChanges();
                return Response.Deleted;
            case null:
                return Response.NotFound;
            default:
                throw new NotImplementedException("State variant not implemented");
        }
    }

    public WorkItemDetailsDTO Find(int itemId)
    {
        var entity = context.Items.Where(o => o.Id == itemId).First();

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
        return context.Items.Select(o => new WorkItemDTO(o.Id, o.Title, o.AssignedTo.Name, o.Tags.Select(c => c.Name).ToList(), o.State)).ToList();
    }

    public IReadOnlyCollection<WorkItemDTO> ReadByState(State state)
    {
        return context.Items.Where(o => o.State == state).Select(o => new WorkItemDTO(o.Id, o.Title, o.AssignedTo.Name, o.Tags.Select(c => c.Name).ToList(), o.State)).ToList();
    }

    public IReadOnlyCollection<WorkItemDTO> ReadByTag(string tag)
    {
        return context.Items.Where(t => t.Tags.Any( o => o.Name == tag)).Select(o => new WorkItemDTO(o.Id, o.Title, o.AssignedTo.Name, o.Tags.Select(c => c.Name).ToList(), o.State)).ToList();
    }

    public IReadOnlyCollection<WorkItemDTO> ReadByUser(int userId)
    {
        return context.Items.Where(t => t.AssignedToId == userId).Select(o => new WorkItemDTO(o.Id, o.Title, o.AssignedTo.Name, o.Tags.Select(c => c.Name).ToList(), o.State)).ToList();
    }

    public IReadOnlyCollection<WorkItemDTO> ReadRemoved()
    {
        return context.Items.Where(o => o.State == State.Removed).Select(o => new WorkItemDTO(o.Id, o.Title, o.AssignedTo.Name, o.Tags.Select(c => c.Name).ToList(), o.State)).ToList();
    }

    public Response Update(WorkItemUpdateDTO task)
    {
        var entity = context.Items.Find(task.Id);
        Response response;

        if (entity is null)
        {
            response = Response.NotFound;
        }
        //if two tasks exists with the same titles but different ids
        else if (context.Items.FirstOrDefault(t => t.Id != task.Id && t.Title == task.Title) != null)
        {
            response = Response.Conflict;
        }
        else if (context.Users.Find(task.AssignedToId) is null)
        {
            response = Response.BadRequest;
        }
        else
        {
            entity.AssignedTo = task.AssignedToId is not null ? context.Users.Find(task.AssignedToId) : entity.AssignedTo;
            //entity.Description = task.Description is not null ? task.Description : entity.Description;


            if (task.Tags is not null)
            {
                entity.Tags = new List<Tag>();
                foreach (var tag in task.Tags!)
                {
                    foreach (var conTag in context.Tags)
                    {
                        if (conTag.Name == tag)
                        {
                            entity.Tags.Add(context.Tags.Find(conTag.Id));
                        }
                    }
                }
            }

            entity.State = task.State;
            entity.Title = task.Title;
            context.SaveChanges();
            response = Response.Updated;
        }

        return response;
    }
}
