using Nevma.Identity.Api.Domain.Connections;

namespace Nevma.Identity.Api.Application.Connections;

public interface IContactConnectionRepository
{
    Task<ContactConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ContactConnection?> GetByUsersAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContactConnection>> ListForUserAsync(
        Guid userId,
        ContactConnectionStatus? status,
        CancellationToken cancellationToken = default);
    Task AddAsync(ContactConnection connection, CancellationToken cancellationToken = default);
}
