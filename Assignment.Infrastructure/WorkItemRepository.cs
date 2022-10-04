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
            entity = new WorkItem ( task.Title);

            /*context.WorkItems.Add(entity);
            context.SaveChanges();*/

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
        throw new NotImplementedException();
    }

    public IReadOnlyCollection<WorkItemDTO> Read()
    {
        throw new NotImplementedException();
    }

    public IReadOnlyCollection<WorkItemDTO> ReadByState(State state)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyCollection<WorkItemDTO> ReadByTag(string tag)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyCollection<WorkItemDTO> ReadByUser(int userId)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyCollection<WorkItemDTO> ReadRemoved()
    {
        throw new NotImplementedException();
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
