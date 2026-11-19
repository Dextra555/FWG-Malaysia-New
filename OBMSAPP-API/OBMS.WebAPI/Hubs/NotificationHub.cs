using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace OBMS.WebAPI.Hubs
{
    /// <summary>
    /// SignalR hub for real-time notification delivery.
    ///
    /// Connection is authenticated via JWT Bearer token passed as the
    /// access_token query-string parameter (Angular client pattern).
    ///
    /// Users join their own private group on connect so that
    /// IHubContext.Clients.Group(username) targets exactly one user,
    /// even if the same user is open in multiple browser tabs.
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Called automatically when a client establishes a SignalR connection.
        /// The user joins a group named after their username so server-side code
        /// can push to a specific user without broadcasting to everyone.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            // Context.User is populated from the JWT via [Authorize]
            var username = Context.User?.Identity?.Name;

            if (!string.IsNullOrEmpty(username))
            {
                // Each user joins a group named after their username (case-sensitive)
                await Groups.AddToGroupAsync(Context.ConnectionId, username);
                _logger.LogInformation(
                    "SignalR: {Username} connected (ConnectionId={ConnectionId}).",
                    username, Context.ConnectionId);
            }
            else
            {
                _logger.LogWarning(
                    "SignalR: Unauthenticated connection attempt (ConnectionId={ConnectionId}). " +
                    "Connection will be rejected by [Authorize].",
                    Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects (graceful or network failure).
        /// The framework automatically removes the connection from all groups,
        /// but we log it for observability.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var username = Context.User?.Identity?.Name;

            if (exception is null)
                _logger.LogInformation(
                    "SignalR: {Username} disconnected gracefully (ConnectionId={ConnectionId}).",
                    username, Context.ConnectionId);
            else
                _logger.LogWarning(
                    "SignalR: {Username} disconnected with error (ConnectionId={ConnectionId}): {Error}",
                    username, Context.ConnectionId, exception.Message);

            await base.OnDisconnectedAsync(exception);
        }
    }
}
