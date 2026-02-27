using System.Net.Http.Json;
using System.Text.Json;
using AuraWellness.Domain.Interfaces;

namespace AuraWellness.Infrastructure.Http;

public class ChatServiceClient(HttpClient httpClient) : IChatServiceClient
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public async Task<Guid> CreateWorkspaceAsync(Guid buId, Guid companyId, string name, CancellationToken ct = default)
    {
        var payload = new { bu_id = buId, company_id = companyId, name };
        var response = await httpClient.PostAsJsonAsync("/api/workspaces", payload, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<WorkspaceResponse>(JsonOpts, ct)
            ?? throw new InvalidOperationException("Invalid response from chat service.");
        return result.Id;
    }

    public async Task AddWorkspaceMemberAsync(Guid workspaceId, Guid personId, string role, CancellationToken ct = default)
    {
        var payload = new { person_id = personId, role };
        var response = await httpClient.PostAsJsonAsync($"/api/workspaces/{workspaceId}/members", payload, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateMemberAccessAsync(Guid workspaceId, Guid personId, bool hasAccess, CancellationToken ct = default)
    {
        var payload = new { has_access = hasAccess };
        var response = await httpClient.PutAsJsonAsync($"/api/workspaces/{workspaceId}/members/{personId}", payload, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<ChatWorkspaceDto?> GetWorkspaceByBuIdAsync(Guid buId, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/api/workspaces/bu/{buId}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<WorkspaceResponse>(JsonOpts, ct);
        if (result is null) return null;
        return new ChatWorkspaceDto(result.Id, result.BuId, result.CompanyId, result.Name, result.CreatedAt);
    }

    public async Task<List<ChatMemberDto>> GetWorkspaceMembersAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/api/workspaces/{workspaceId}/members", ct);
        response.EnsureSuccessStatusCode();

        var results = await response.Content.ReadFromJsonAsync<List<MemberResponse>>(JsonOpts, ct) ?? [];
        return results.Select(m => new ChatMemberDto(m.Id, m.WorkspaceId, m.PersonId, m.Role, m.HasAccess, m.CreatedAt)).ToList();
    }

    private record WorkspaceResponse(Guid Id, Guid BuId, Guid CompanyId, string Name, DateTime CreatedAt);
    private record MemberResponse(Guid Id, Guid WorkspaceId, Guid PersonId, string Role, bool HasAccess, DateTime CreatedAt);
}
