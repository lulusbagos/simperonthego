using Microsoft.AspNetCore.SignalR;

namespace SimperSecureOnlineTestSystem.Hubs;

public class MonitoringHub : Hub
{
    public Task JoinAdminGroup()
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, "admins");
    }

    public Task JoinSessionGroup(string token)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, $"session:{token}");
    }

    public Task SendCameraFrame(string token, string imageDataUrl)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(imageDataUrl) || !imageDataUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        if (imageDataUrl.Length > 500_000)
        {
            return Task.CompletedTask;
        }

        return Clients.Group("admins").SendAsync("CameraFrameUpdated", new
        {
            token,
            imageDataUrl,
            capturedAtUtc = DateTime.UtcNow
        });
    }
}
