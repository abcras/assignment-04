namespace Assignment.Infrastructure;

public class TagRepository : ITagRepository
{
    private readonly DbContext _context;

    public TagRepository(DbContext context)
    {
        _context = context;
    }

    public (Response Response, int TagId) Create(TagCreateDTO tag)
    {
        var entity = _context.Set<Tag>().FirstOrDefault(c => c.Name == tag.Name);
        Response response;

        if (entity is null)
        {
            entity = new Tag(tag.Name);

            _context.Set<Tag>().Add(entity);
            _context.SaveChanges();

            response = Response.Created;
        }
        else
        {
            response = Response.Conflict;
        }

        return (response, entity.Id);
    }

    public Response Delete(int tagId, bool force = false)
    {
        var entity = _context.Set<Tag>().FirstOrDefault(c => c.Id == tagId);
        Response response;

        if (entity is not null)
        {
            if (entity.WorkItems.Count() > 0)
            {
                if (!force)
                {
                    response = Response.Conflict;
                }
                else
                {
                    _context.Set<Tag>().Remove(entity);
                    _context.SaveChanges();
                    response = Response.Deleted;
                }
            }
            else
            {
                _context.Set<Tag>().Remove(entity);
                _context.SaveChanges();
                response = Response.Deleted;
            }
        }
        else
        {
            response = Response.NotFound;
        }

        return response;
    }

    public TagDTO Find(int tagId)
    {
        var tags = from t in _context.Set<Tag>()
                   where t.Id == tagId
                   select new TagDTO(t.Id, t.Name);

        return tags.FirstOrDefault();
    }

    public IReadOnlyCollection<TagDTO> Read()
    {
        var tags = from t in _context.Set<Tag>()
                   select new TagDTO(t.Id, t.Name);

        return tags.ToList();
    }

    public Response Update(TagUpdateDTO tag)
    {
        var entity = _context.Set<Tag>().Find(tag.Id);
        Response response;

        if (entity is null)
        {
            response = Response.NotFound;
        }
        //if two tags exists with the same name but different ids
        else if (_context.Set<Tag>().FirstOrDefault(t => t.Id != tag.Id && t.Name == tag.Name) != null)
        {
            response = Response.Conflict;
        }
        else
        {
            entity.Name = tag.Name;
            _context.SaveChanges();
            response = Response.Updated;
        }

        return response;
    }
}
