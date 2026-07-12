using Nevma.Identity.Api.Domain.Users;

namespace Nevma.Identity.Api.Application.Users;

public interface IUserRepository
{
    User? GetById(Guid id);
    void Add(User user);
}
