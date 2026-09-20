using DeliveryApp.Models;
using DeliveryApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DeliveryApp.Controllers
{
    // ============================================================
    // CONTROLEUR NOTIFICATIONS
    // Accès : tous les utilisateurs connectés
    // Sécurité : chaque utilisateur voit UNIQUEMENT ses notifications
    // (filtré par userId automatiquement)
    // ============================================================
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notifService;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationController(INotificationService notifService, UserManager<ApplicationUser> userManager)
        {
            _notifService = notifService;
            _userManager = userManager;
        }

        // ────────────────────────────────────────────────────────
        // PAGE : Liste de toutes les notifications de l'utilisateur
        // ────────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var list = await _notifService.GetForUserAsync(userId, 100);
            return View(list);
        }

        // ────────────────────────────────────────────────────────
        // API : Nombre de notifications non lues (pour le badge)
        // ────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Json(new { count = 0 });
            var count = await _notifService.GetUnreadCountAsync(userId);
            return Json(new { count });
        }

        // ────────────────────────────────────────────────────────
        // API : 10 dernières notifications (pour le dropdown)
        // ────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Recent()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Json(new { items = new object[0], unread = 0 });

            var list = await _notifService.GetForUserAsync(userId, 10);
            var unread = await _notifService.GetUnreadCountAsync(userId);

            return Json(new
            {
                unread,
                items = list.Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    message = n.Message,
                    type = n.Type.ToString(),
                    link = n.Link,
                    isRead = n.IsRead,
                    createdAt = n.CreatedAt,
                    timeAgo = TimeAgo(n.CreatedAt)
                })
            });
        }

        // ────────────────────────────────────────────────────────
        // ACTION : Marquer une notification comme lue
        // ────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            await _notifService.MarkAsReadAsync(id, userId);
            return Ok(new { success = true });
        }

        // ────────────────────────────────────────────────────────
        // ACTION : Marquer toutes les notifications comme lues
        // ────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            await _notifService.MarkAllAsReadAsync(userId);
            return Ok(new { success = true });
        }

        // ────────────────────────────────────────────────────────
        // ACTION : Supprimer une notification
        // ────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            await _notifService.DeleteAsync(id, userId);
            TempData["Success"] = "Notification supprimée";
            return RedirectToAction(nameof(Index));
        }

        // ────────────────────────────────────────────────────────
        // UTILITAIRE : Calcul du temps écoulé ("il y a 5 min")
        // ────────────────────────────────────────────────────────
        private static string TimeAgo(DateTime date)
        {
            var span = DateTime.Now - date;
            if (span.TotalSeconds < 60) return "à l'instant";
            if (span.TotalMinutes < 60) return $"il y a {(int)span.TotalMinutes} min";
            if (span.TotalHours < 24) return $"il y a {(int)span.TotalHours} h";
            if (span.TotalDays < 7) return $"il y a {(int)span.TotalDays} j";
            return date.ToString("dd/MM/yyyy");
        }
    }
}
