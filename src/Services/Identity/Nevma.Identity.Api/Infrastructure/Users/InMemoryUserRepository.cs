using System.Collections.Concurrent;
using Nevma.Identity.Api.Application.Users;
using Nevma.Identity.Api.Domain.Users;

namespace Nevma.Identity.Api.Infrastructure.Users;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _users = new();

    public User? GetById(Guid id) => _users.GetValueOrDefault(id);

    public void Add(User user) => _users[user.Id] = user;
}
