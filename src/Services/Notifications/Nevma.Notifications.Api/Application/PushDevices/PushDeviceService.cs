using Nevma.Contracts.Notifications;
using DomainDevice = Nevma.Notifications.Api.Domain.PushDevices.PushDevice;
using DomainPlatform = Nevma.Notifications.Api.Domain.PushDevices.PushPlatform;

namespace Nevma.Notifications.Api.Application.PushDevices;

public sealed class PushDeviceService(
    IPushDeviceRepository repository,
    IPushTokenProtector tokenProtector,
    INotificationsUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<RegisterPushDeviceResult> RegisterAsync(
        Guid userId,
        RegisterPushDeviceRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
            return RegisterPushDeviceResult.Failure(errors);

        var now = timeProvider.GetUtcNow();
        var protectedToken = tokenProtector.Protect(request.Token);
        var tokenHash = tokenProtector.Hash(request.Token);
        var device = await repository.FindAsync(userId, request.DeviceId, cancellationToken);
        if (device is null)
        {
            device = DomainDevice.Register(
                userId,
                request.DeviceId,
                (DomainPlatform)(int)request.Platform,
                protectedToken,
                tokenHash,
                now);
            await repository.AddAsync(device, cancellationToken);
        }
        else
        {
            device.Refresh(
                (DomainPlatform)(int)request.Platform,
                protectedToken,
                tokenHash,
                now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return RegisterPushDeviceResult.Success(ToResponse(device));
    }

    public async Task<RevokePushDeviceResult> RevokeAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var device = await repository.GetAsync(id, userId, cancellationToken);
        if (device is null)
            return new RevokePushDeviceResult.NotFound();
        device.Revoke();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new RevokePushDeviceResult.Revoked();
    }

    private static Dictionary<string, string[]> Validate(RegisterPushDeviceRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.DeviceId) || request.DeviceId.Length > 200)
            errors[nameof(request.DeviceId)] = ["Device ID is required and cannot exceed 200 characters."];
        if (!Enum.IsDefined(request.Platform))
            errors[nameof(request.Platform)] = ["Push platform is invalid."];
        if (string.IsNullOrWhiteSpace(request.Token) || request.Token.Length > 4_096)
            errors[nameof(request.Token)] = ["Push token is required and cannot exceed 4096 characters."];
        return errors;
    }

    private static PushDeviceResponse ToResponse(DomainDevice device) =>
        new(
            device.Id,
            device.DeviceId,
            (PushPlatform)(int)device.Platform,
            device.IsActive,
            device.RegisteredAt,
            device.LastSeenAt);
}

public sealed record RegisterPushDeviceResult(
    PushDeviceResponse? Device,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public bool IsSuccess => Device is not null;

    public static RegisterPushDeviceResult Success(PushDeviceResponse device) =>
        new(device, new Dictionary<string, string[]>());

    public static RegisterPushDeviceResult Failure(IReadOnlyDictionary<string, string[]> errors) =>
        new(null, errors);
}

public abstract record RevokePushDeviceResult
{
    public sealed record Revoked : RevokePushDeviceResult;
    public sealed record NotFound : RevokePushDeviceResult;
}
