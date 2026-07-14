namespace Nevma.Identity.Api.Application;

public interface IIdentityUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
