namespace Nevma.Notifications.Api.Application.PushDevices;

public interface IPushTokenProtector
{
    string Protect(string token);
    string Unprotect(string protectedToken);
    string Hash(string token);
}
