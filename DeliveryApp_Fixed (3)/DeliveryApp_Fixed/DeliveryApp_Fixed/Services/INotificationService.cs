using DeliveryApp.Models;

namespace DeliveryApp.Services
{
    public interface INotificationService
    {
        Task CreateAsync(string userId, string title, string message, NotificationType type = NotificationType.Info, string? link = null);
        Task NotifyClientByEmailAsync(string? clientEmail, string title, string message, NotificationType type = NotificationType.Info, string? link = null);
        Task NotifyAllUsersAsync(string title, string message, NotificationType type = NotificationType.Info, string? link = null);
        Task<List<Notification>> GetForUserAsync(string userId, int take = 50);
        Task<int> GetUnreadCountAsync(string userId);
        Task MarkAsReadAsync(int id, string userId);
        Task MarkAllAsReadAsync(string userId);
        Task DeleteAsync(int id, string userId);
    }
}
