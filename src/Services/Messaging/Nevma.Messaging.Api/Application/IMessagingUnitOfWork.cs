namespace Nevma.Messaging.Api.Application;

public interface IMessagingUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
