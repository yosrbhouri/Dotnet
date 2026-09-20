using DeliveryApp.Data;
using DeliveryApp.Hubs;
using DeliveryApp.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DeliveryApp.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hub;

        public NotificationService(AppDbContext context, IHubContext<NotificationHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        private static string TimeAgo(DateTime date)
        {
            var span = DateTime.Now - date;
            if (span.TotalSeconds < 60) return "à l'instant";
            if (span.TotalMinutes < 60) return $"il y a {(int)span.TotalMinutes} min";
            if (span.TotalHours < 24) return $"il y a {(int)span.TotalHours} h";
            if (span.TotalDays < 7) return $"il y a {(int)span.TotalDays} j";
            return date.ToString("dd/MM/yyyy");
        }

        private async Task PushAsync(Notification n)
        {
            var unread = await _context.Notifications.CountAsync(x => x.UserId == n.UserId && !x.IsRead);
            await _hub.Clients.User(n.UserId).SendAsync("ReceiveNotification", new
            {
                id = n.Id,
                title = n.Title,
                message = n.Message,
                type = n.Type.ToString(),
                link = n.Link,
                isRead = n.IsRead,
                createdAt = n.CreatedAt,
                timeAgo = TimeAgo(n.CreatedAt),
                unread
            });
        }

        public async Task CreateAsync(string userId, string title, string message, NotificationType type = NotificationType.Info, string? link = null)
        {
            if (string.IsNullOrWhiteSpace(userId)) return;

            var notif = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Link = link,
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notif);
            await _context.SaveChangesAsync();
            await PushAsync(notif);
        }

        public async Task NotifyClientByEmailAsync(string? clientEmail, string title, string message, NotificationType type = NotificationType.Info, string? link = null)
        {
            if (string.IsNullOrWhiteSpace(clientEmail)) return;

            var users = await _context.Users
                .Where(u => u.Email == clientEmail)
                .Select(u => u.Id)
                .ToListAsync();

            foreach (var uid in users)
                await CreateAsync(uid, title, message, type, link);
        }

        public async Task NotifyAllUsersAsync(string title, string message, NotificationType type = NotificationType.Info, string? link = null)
        {
            var users = await _context.Users.Select(u => u.Id).ToListAsync();
            foreach (var uid in users)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = uid,
                    Title = title,
                    Message = message,
                    Type = type,
                    Link = link,
                    CreatedAt = DateTime.Now
                });
            }
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetForUserAsync(string userId, int take = 50)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task MarkAsReadAsync(int id, string userId)
        {
            var n = await _context.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (n != null && !n.IsRead)
            {
                n.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var list = await _context.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
            foreach (var n in list) n.IsRead = true;
            if (list.Count > 0) await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id, string userId)
        {
            var n = await _context.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (n != null)
            {
                _context.Notifications.Remove(n);
                await _context.SaveChangesAsync();
            }
        }
    }
}
