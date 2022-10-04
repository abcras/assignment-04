namespace Assignment.Infrastructure;

public class WorkItem
{
    public int Id { get; set; }

    public string Title { get; set; }

    public int? AssignedToId { get; set; }

    public string Description { get; set; }

    public DateTime Created { get; init; }

    public User? AssignedTo { get; set; }


    private State _state;

    public State State
    {
        get => _state;
        set
        {
            StateUpdated = DateTime.UtcNow;
           _state = value;
        }
    }

    public DateTime StateUpdated { get; set; }

    public ICollection<Tag> Tags { get; set; }

    public WorkItem(string title, string description)
    {
        Created = DateTime.UtcNow;
        Title = title;
        Description = description;
        Tags = new HashSet<Tag>();
        State = State.New;
    }
}
