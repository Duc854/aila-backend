using AILA.Domain.Entities;

namespace AILA.Application.Common.Interfaces.Repositories
{
    public interface ITagRepository : IGenericRepository<Tag> // Wait, the entity is Tags.cs but class is Tag? I need to check. Let's use Tags as default if it's named Tags, but usually it's Tag.
    {
    }
}
