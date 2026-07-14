using System.Net.Http.Headers;
using System.Net.Http.Json;
using Nevma.Contracts.Files;
using Nevma.Messaging.Api.Application.Messages;

namespace Nevma.Messaging.Api.Infrastructure.Messages;

public sealed class HttpFileAttachmentAuthorizer(IHttpClientFactory clientFactory)
    : IFileAttachmentAuthorizer
{
    public async Task<bool> AuthorizeAsync(
        IReadOnlyCollection<Guid> fileIds,
        Guid senderId,
        IReadOnlyCollection<Guid> participantIds,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken)) return false;
        var client = clientFactory.CreateClient("Files");
        foreach (var fileId in fileIds)
        {
            using var metadata = new HttpRequestMessage(HttpMethod.Get, $"/api/files/{fileId}");
            metadata.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var metadataResponse = await client.SendAsync(metadata, cancellationToken);
            if (!metadataResponse.IsSuccessStatusCode) return false;

            foreach (var participantId in participantIds.Where(id => id != senderId))
            {
                using var grant = new HttpRequestMessage(HttpMethod.Post, $"/api/files/{fileId}/grants")
                {
                    Content = JsonContent.Create(new GrantFileAccessRequest(participantId))
                };
                grant.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                using var grantResponse = await client.SendAsync(grant, cancellationToken);
                if (!grantResponse.IsSuccessStatusCode) return false;
            }
        }
        return true;
    }
}
