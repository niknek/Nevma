using System.Net.Http.Headers;
using System.Net.Http.Json;
using Nevma.Contracts.Home;
using Nevma.Contracts.Identity;
using Nevma.Contracts.Notifications;
using Nevma.Contracts.Planning;
using Nevma.ServiceDefaults.Extensions;

namespace Nevma.Gateway.Home;

public sealed class HomeService(
    IHttpClientFactory httpClientFactory,
    TimeProvider timeProvider)
{
    public async Task<HomeResponse> GetAsync(
        string authorization,
        CancellationToken cancellationToken = default)
    {
        if (!AuthenticationHeaderValue.TryParse(authorization, out var header))
            throw new HomeAggregationException("A valid authorization header is required.");

        var tasksRequest = GetAsync<IReadOnlyCollection<TaskResponse>>(
            "Planning",
            "/api/tasks?filter=All",
            header,
            cancellationToken);
        var eventsRequest = GetAsync<IReadOnlyCollection<CalendarEventResponse>>(
            "Planning",
            CreateCalendarPath(timeProvider.GetUtcNow()),
            header,
            cancellationToken);
        var connectionsRequest = GetAsync<IReadOnlyCollection<ContactConnectionResponse>>(
            "Identity",
            "/api/connections?status=Pending",
            header,
            cancellationToken);
        var notificationsRequest = GetAsync<IReadOnlyCollection<NotificationResponse>>(
            "Notifications",
            "/api/notifications?take=20",
            header,
            cancellationToken);

        await Task.WhenAll(tasksRequest, eventsRequest, connectionsRequest, notificationsRequest);

        var tasks = await tasksRequest;
        var events = await eventsRequest;
        var connections = await connectionsRequest;
        var notifications = await notificationsRequest;
        var activeTasks = tasks
            .Where(task => task.Status == Contracts.Planning.TaskStatus.Active)
            .ToArray();
        var nextTask = activeTasks
            .OrderByDescending(task => task.Priority)
            .ThenBy(task => task.DueAt is null)
            .ThenBy(task => task.DueAt)
            .FirstOrDefault();

        return new HomeResponse(
            nextTask,
            activeTasks.Where(task => task.Priority == TaskPriority.Urgent).Take(5).ToArray(),
            events.OrderBy(calendarEvent => calendarEvent.StartsAt).Take(10).ToArray(),
            connections.OrderByDescending(connection => connection.CreatedAt).Take(10).ToArray(),
            notifications.Where(notification => notification.ReadAt is null).Take(10).ToArray(),
            timeProvider.GetUtcNow());
    }

    private async Task<T> GetAsync<T>(
        string clientName,
        string path,
        AuthenticationHeaderValue authorization,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(clientName);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = authorization;
        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception.IsTransientHttpFailure(cancellationToken))
        {
            throw new HomeAggregationException(
                $"{clientName} was unavailable while building Home.",
                exception);
        }
        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new HomeAggregationException(
                    $"{clientName} returned status {(int)response.StatusCode} while building Home.");
            }

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
                ?? throw new HomeAggregationException($"{clientName} returned an empty Home response.");
        }
    }

    private static string CreateCalendarPath(DateTimeOffset now)
    {
        var from = Uri.EscapeDataString(now.ToString("O"));
        var to = Uri.EscapeDataString(now.AddDays(7).ToString("O"));
        return $"/api/calendar?from={from}&to={to}";
    }
}

public sealed class HomeAggregationException : Exception
{
    public HomeAggregationException(string message) : base(message) { }
    public HomeAggregationException(string message, Exception innerException)
        : base(message, innerException) { }
}
