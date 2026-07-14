namespace Nevma.Planning.Api.Application;

public interface IPlanningUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
