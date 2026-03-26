using Microsoft.AspNetCore.SignalR;
using TechUnited_AiStudio.Data;
using TechUnited_AiStudio.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace TechUnited_AiStudio.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        public ChatHub(IDbContextFactory<ApplicationDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task SendPrivateMessage(string recipientUserId, string message)
        {
            // 1. Get IDs - prioritizing the Claim for accuracy
            var senderId = Context.UserIdentifier ??
                           Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(recipientUserId))
            {
                Console.WriteLine("--> [Hub Error]: Sender or Recipient ID is null.");
                return;
            }

            try
            {
                // 2. Persist to Database
                using (var db = await _dbFactory.CreateDbContextAsync())
                {
                    var newMessage = new PrivateMessage
                    {
                        SenderId = senderId,
                        ReceiverId = recipientUserId,
                        Content = message,
                        Timestamp = DateTime.UtcNow
                    };

                    db.PrivateMessages.Add(newMessage);
                    await db.SaveChangesAsync();
                }

                // 3. Broadcast to both Users
                // Clients.User() depends on the IUserIdProvider we added to Program.cs
                await Clients.User(recipientUserId).SendAsync("ReceiveMessage", senderId, message);
                await Clients.User(senderId).SendAsync("ReceiveMessage", senderId, message);
            }
            catch (Exception ex)
            {
                // This will output to your Visual Studio 'Debug' or 'Output' window
                Console.WriteLine($"--> [Database Exception]: {ex.Message}");
            }
        }
    }
}