using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR.Client;
using Nevma.Contracts.Commands;
using Nevma.Contracts.Home;
using Nevma.Contracts.Identity;
using Nevma.Contracts.Messaging;
using Nevma.Contracts.Notifications;
using Nevma.Contracts.Planning;
using Nevma.Contracts.Search;

namespace Nevma.EndToEndTests;

public sealed class CoreWorkflowTests
{
    private static readonly Uri IdentityUri = new("https://localhost:7293");
    private static readonly Uri PlanningUri = new("http://localhost:5276");
    private static readonly Uri MessagingUri = new("http://localhost:5085");
    private static readonly Uri NotificationsUri = new("http://localhost:5095");
    private static readonly Uri GatewayUri = new("http://localhost:5033");
    private static readonly Uri CommandsUri = new("http://localhost:5116");
    private const string RedirectUri = "com.nevma.app:/oauth/callback";

    [LiveInfrastructureFact]
    public async Task Connected_users_can_complete_the_first_product_workflow()
    {
        using var identity = CreateClient(IdentityUri);
        var suffix = Guid.NewGuid().ToString("N");
        var password = $"Nevma!{suffix}Aa1";
        var organizerEmail = $"organizer-{suffix}@nevma.local";
        var inviteeEmail = $"invitee-{suffix}@nevma.local";

        var organizer = await RegisterAsync(identity, organizerEmail, password, "E2E Organizer");
        var invitee = await RegisterAsync(identity, inviteeEmail, password, "E2E Invitee");
        var organizerToken = await AcquireTokenAsync(
            organizerEmail,
            password,
            $"e2e-organizer-{suffix}",
            "E2E Organizer phone");
        var inviteeToken = await AcquireTokenAsync(
            inviteeEmail,
            password,
            $"e2e-invitee-{suffix}",
            "E2E Invitee phone");

        using var organizerIdentity = CreateClient(IdentityUri, organizerToken);
        using var inviteeIdentity = CreateClient(IdentityUri, inviteeToken);
        var connectionResponse = await organizerIdentity.PostAsJsonAsync(
            "/api/connections/",
            new CreateContactConnectionRequest(invitee.Id));
        Assert.Equal(HttpStatusCode.Created, connectionResponse.StatusCode);
        var connection = await ReadAsync<ContactConnectionResponse>(connectionResponse);

        var acceptConnection = await inviteeIdentity.PostAsync(
            $"/api/connections/{connection.Id}/accept",
            content: null);
        Assert.Equal(HttpStatusCode.OK, acceptConnection.StatusCode);

        using var organizerPlanning = CreateClient(PlanningUri, organizerToken);
        using var inviteePlanning = CreateClient(PlanningUri, inviteeToken);
        var taskResponse = await organizerPlanning.PostAsJsonAsync(
            "/api/tasks/",
            new CreateTaskRequest(
                $"Urgent E2E task {suffix[..8]}",
                null,
                DateTimeOffset.UtcNow.AddHours(1),
                TaskPriority.Urgent,
                null));
        Assert.Equal(HttpStatusCode.Created, taskResponse.StatusCode);
        var task = await ReadAsync<TaskResponse>(taskResponse);

        using var gateway = CreateClient(GatewayUri, organizerToken);
        var home = await gateway.GetFromJsonAsync<HomeResponse>("/api/home");
        Assert.NotNull(home);
        Assert.Equal(task.Id, home.NextTask?.Id);
        Assert.Contains(home.UrgentTasks, item => item.Id == task.Id);
        var search = await gateway.GetFromJsonAsync<SearchResponse>(
            $"/api/search?q={Uri.EscapeDataString(suffix[..8])}");
        Assert.Contains(search!.Items, item => item.Kind == "task" && item.Id == task.Id);
        Assert.Empty(search.UnavailableSources);

        using var commands = CreateClient(CommandsUri, organizerToken);
        var voiceTaskTitle = $"Voice E2E task {suffix[..8]}";
        var previewResponse = await commands.PostAsJsonAsync(
            "/api/commands/preview",
            new PreviewCommandRequest($"task: {voiceTaskTitle} | priority=high", $"e2e-command-{suffix}"));
        Assert.Equal(HttpStatusCode.Created, previewResponse.StatusCode);
        var preview = await ReadAsync<CommandResponse>(previewResponse);
        Assert.Equal(CommandStatus.Previewed, preview.Status);
        var duplicatePreviewResponse = await commands.PostAsJsonAsync(
            "/api/commands/preview",
            new PreviewCommandRequest($"task: {voiceTaskTitle} | priority=high", $"e2e-command-{suffix}"));
        Assert.Equal(HttpStatusCode.OK, duplicatePreviewResponse.StatusCode);
        var duplicatePreview = await ReadAsync<CommandResponse>(duplicatePreviewResponse);
        Assert.Equal(preview.Id, duplicatePreview.Id);
        var confirmResponse = await commands.PostAsync($"/api/commands/{preview.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        var executed = await ReadAsync<CommandResponse>(confirmResponse);
        Assert.Equal(CommandStatus.Executed, executed.Status);
        var voiceTasks = await organizerPlanning.GetFromJsonAsync<List<TaskResponse>>("/api/tasks/");
        Assert.Contains(voiceTasks!, item => item.Title == voiceTaskTitle);
        var undoResponse = await commands.PostAsync($"/api/commands/{preview.Id}/undo", null);
        Assert.Equal(HttpStatusCode.OK, undoResponse.StatusCode);
        var undone = await ReadAsync<CommandResponse>(undoResponse);
        Assert.Equal(CommandStatus.Undone, undone.Status);

        var liveNotification = new TaskCompletionSource<NotificationResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        await using var notificationHub = new HubConnectionBuilder()
            .WithUrl(new Uri(NotificationsUri, "/hubs/notifications"), options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(inviteeToken);
            })
            .Build();
        notificationHub.On<NotificationResponse>("notification.received", notification =>
        {
            if (notification.Type == "meeting-invitation.changed")
                liveNotification.TrySetResult(notification);
        });
        await notificationHub.StartAsync();

        var title = $"E2E coffee {suffix[..8]}";
        var startsAt = DateTimeOffset.UtcNow.AddHours(2);
        var invitationResponse = await organizerPlanning.PostAsJsonAsync(
            "/api/meeting-invitations/",
            new CreateMeetingInvitationRequest(
                invitee.Id,
                title,
                startsAt,
                TimeSpan.FromHours(1),
                "Athens",
                "Created by the live backend test"));
        Assert.Equal(HttpStatusCode.Created, invitationResponse.StatusCode);
        var invitation = await ReadAsync<MeetingInvitationResponse>(invitationResponse);
        var realtimeNotification = await liveNotification.Task.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.Equal("New meeting request", realtimeNotification.Title);

        var acceptInvitation = await inviteePlanning.PostAsync(
            $"/api/meeting-invitations/{invitation.Id}/accept",
            content: null);
        Assert.Equal(HttpStatusCode.OK, acceptInvitation.StatusCode);
        var calendarEvent = await ReadAsync<CalendarEventResponse>(acceptInvitation);
        Assert.Contains(organizer.Id, calendarEvent.ParticipantIds);
        Assert.Contains(invitee.Id, calendarEvent.ParticipantIds);

        var from = Uri.EscapeDataString(startsAt.AddHours(-1).ToString("O"));
        var to = Uri.EscapeDataString(startsAt.AddHours(2).ToString("O"));
        var organizerCalendar = await organizerPlanning.GetFromJsonAsync<List<CalendarEventResponse>>(
            $"/api/calendar?from={from}&to={to}");
        var inviteeCalendar = await inviteePlanning.GetFromJsonAsync<List<CalendarEventResponse>>(
            $"/api/calendar?from={from}&to={to}");
        Assert.Contains(organizerCalendar!, item => item.InvitationId == invitation.Id);
        Assert.Contains(inviteeCalendar!, item => item.InvitationId == invitation.Id);

        var concurrentStart = DateTimeOffset.UtcNow.AddHours(5);
        var concurrentInvitationResponse = await organizerPlanning.PostAsJsonAsync(
            "/api/meeting-invitations/",
            new CreateMeetingInvitationRequest(
                invitee.Id,
                $"Concurrent E2E meeting {suffix[..8]}",
                concurrentStart,
                TimeSpan.FromHours(1),
                "Athens",
                "Used to verify serializable acceptance"));
        Assert.Equal(HttpStatusCode.Created, concurrentInvitationResponse.StatusCode);
        var concurrentInvitation = await ReadAsync<MeetingInvitationResponse>(concurrentInvitationResponse);
        var concurrentAccepts = await Task.WhenAll(
            inviteePlanning.PostAsync($"/api/meeting-invitations/{concurrentInvitation.Id}/accept", null),
            inviteePlanning.PostAsync($"/api/meeting-invitations/{concurrentInvitation.Id}/accept", null));
        Assert.Single(concurrentAccepts, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(concurrentAccepts, response => response.StatusCode == HttpStatusCode.Conflict);
        foreach (var response in concurrentAccepts) response.Dispose();

        var concurrentFrom = Uri.EscapeDataString(concurrentStart.AddMinutes(-1).ToString("O"));
        var concurrentTo = Uri.EscapeDataString(concurrentStart.AddHours(2).ToString("O"));
        var concurrentCalendar = await inviteePlanning.GetFromJsonAsync<List<CalendarEventResponse>>(
            $"/api/calendar?from={concurrentFrom}&to={concurrentTo}");
        Assert.Single(concurrentCalendar!, item => item.InvitationId == concurrentInvitation.Id);

        using var inviteeNotifications = CreateClient(NotificationsUri, inviteeToken);
        var notification = await PollAsync(async () =>
        {
            var items = await inviteeNotifications.GetFromJsonAsync<List<NotificationResponse>>(
                "/api/notifications/?take=50");
            return items?.FirstOrDefault(item =>
                item.Type == "meeting-invitation.changed");
        });
        Assert.NotNull(notification);

        using var inviteeMessaging = CreateClient(MessagingUri, inviteeToken);
        var conversation = await PollAsync(async () =>
        {
            var items = await inviteeMessaging.GetFromJsonAsync<List<ConversationResponse>>(
                "/api/conversations/");
            return items?.FirstOrDefault(item =>
                item.ParticipantIds.Contains(organizer.Id) && item.ParticipantIds.Contains(invitee.Id));
        });
        Assert.NotNull(conversation);

        await using var hub = new HubConnectionBuilder()
            .WithUrl(new Uri(MessagingUri, "/hubs/chat"), options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(inviteeToken);
            })
            .Build();
        await hub.StartAsync();
        await hub.InvokeAsync("Heartbeat");

        using var organizerMessaging = CreateClient(MessagingUri, organizerToken);
        var onlinePresence = await organizerMessaging.GetFromJsonAsync<PresenceResponse>(
            $"/api/conversations/presence/{invitee.Id}");
        Assert.True(onlinePresence?.IsOnline);

        await hub.StopAsync();
        var offlinePresence = await PollAsync(async () =>
        {
            var presence = await organizerMessaging.GetFromJsonAsync<PresenceResponse>(
                $"/api/conversations/presence/{invitee.Id}");
            return presence is { IsOnline: false, LastSeenAt: not null } ? presence : null;
        });
        Assert.NotNull(offlinePresence);

        var sessions = await inviteeIdentity.GetFromJsonAsync<List<DeviceSessionResponse>>(
            "/api/auth/sessions/");
        var currentSession = Assert.Single(sessions!, session => session.IsCurrent);
        var revokeSession = await inviteeIdentity.DeleteAsync($"/api/auth/sessions/{currentSession.Id}");
        Assert.Equal(HttpStatusCode.NoContent, revokeSession.StatusCode);
        var revokedTokenRequest = await inviteeIdentity.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, revokedTokenRequest.StatusCode);
    }

    private static async Task<UserSummary> RegisterAsync(
        HttpClient client,
        string email,
        string password,
        string displayName)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterUserRequest(email, password, displayName, null));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadAsync<UserSummary>(response);
    }

    private static async Task<string> AcquireTokenAsync(
        string email,
        string password,
        string deviceId,
        string deviceName)
    {
        using var client = CreateClient(IdentityUri);
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(64));
        var challenge = Base64Url(SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(verifier)));
        var authorizePath = "/connect/authorize" +
            "?client_id=nevma-mobile" +
            "&response_type=code" +
            $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
            $"&scope={Uri.EscapeDataString("openid profile email offline_access nevma_api")}" +
            $"&code_challenge={Uri.EscapeDataString(challenge)}" +
            "&code_challenge_method=S256" +
            $"&device_id={Uri.EscapeDataString(deviceId)}" +
            $"&device_name={Uri.EscapeDataString(deviceName)}" +
            "&device_platform=Android";

        var challengeResponse = await client.GetAsync(authorizePath);
        Assert.Equal(HttpStatusCode.Redirect, challengeResponse.StatusCode);
        var loginLocation = RequireLocation(challengeResponse);

        var loginPage = await client.GetAsync(loginLocation);
        loginPage.EnsureSuccessStatusCode();
        var html = await loginPage.Content.ReadAsStringAsync();
        var antiforgery = ReadHiddenInput(html, "__RequestVerificationToken");
        var returnUrl = ReadHiddenInput(html, "ReturnUrl");

        var loginResponse = await client.PostAsync(
            "/account/login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = email,
                ["Password"] = password,
                ["ReturnUrl"] = returnUrl,
                ["__RequestVerificationToken"] = antiforgery
            }));
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

        var authorizationResponse = await client.GetAsync(RequireLocation(loginResponse));
        Assert.Equal(HttpStatusCode.Redirect, authorizationResponse.StatusCode);
        var callback = RequireLocation(authorizationResponse);
        Assert.StartsWith(RedirectUri, callback.OriginalString, StringComparison.Ordinal);
        var code = ReadQueryValue(callback.Query, "code");

        var tokenResponse = await client.PostAsync(
            "/connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = "nevma-mobile",
                ["redirect_uri"] = RedirectUri,
                ["code"] = code,
                ["code_verifier"] = verifier
            }));
        tokenResponse.EnsureSuccessStatusCode();
        using var tokenJson = await JsonDocument.ParseAsync(await tokenResponse.Content.ReadAsStreamAsync());
        return tokenJson.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Identity did not return an access token.");
    }

    private static HttpClient CreateClient(Uri baseAddress, string? accessToken = null)
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            CookieContainer = new CookieContainer(),
            ServerCertificateCustomValidationCallback = (request, _, _, errors) =>
                errors == System.Net.Security.SslPolicyErrors.None || request.RequestUri?.IsLoopback == true
        };
        var client = new HttpClient(handler) { BaseAddress = baseAddress };
        if (accessToken is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>()
        ?? throw new InvalidOperationException($"The response did not contain {typeof(T).Name}.");

    private static Uri RequireLocation(HttpResponseMessage response) =>
        response.Headers.Location
        ?? throw new InvalidOperationException("The expected redirect did not include a Location header.");

    private static string ReadHiddenInput(string html, string name)
    {
        var match = Regex.Match(
            html,
            $"name=\"{Regex.Escape(name)}\" value=\"(?<value>[^\"]*)\"",
            RegexOptions.CultureInvariant);
        return match.Success
            ? WebUtility.HtmlDecode(match.Groups["value"].Value)
            : throw new InvalidOperationException($"Login form input '{name}' was not found.");
    }

    private static string ReadQueryValue(string query, string name)
    {
        foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (Uri.UnescapeDataString(pair[0]) == name)
                return Uri.UnescapeDataString(pair.Length == 2 ? pair[1] : string.Empty);
        }

        throw new InvalidOperationException($"Callback query value '{name}' was not found.");
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static async Task<T?> PollAsync<T>(Func<Task<T?>> query) where T : class
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var value = await query();
            if (value is not null)
                return value;
            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        return null;
    }
}
