using DeliveryApp.Data;
using DeliveryApp.Models;
using DeliveryApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeliveryApp.Controllers
{
    // ============================================================
    // CONTROLEUR PLANNING - Calendrier de livraisons
    // Accès : tous les utilisateurs connectés (lecture)
    // Actions d'écriture (déplacer, assigner, changer statut) : ADMIN uniquement
    // Sécurité :
    //   - Admin  → voit tous les colis dans le calendrier
    //   - Client → voit seulement ses colis
    //   - Livreur → voit seulement ses livraisons assignées
    // ============================================================
    [Authorize]
    public class PlanningController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notif;
        private readonly UserManager<ApplicationUser> _userManager;

        public PlanningController(AppDbContext context, INotificationService notif,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _notif = notif;
            _userManager = userManager;
        }

        // ────────────────────────────────────────────────────────
        // PAGE CALENDRIER
        // ────────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewBag.Livreurs = await _context.Livreurs
                .OrderBy(l => l.RaisonSociale)
                .ToListAsync();
            return View();
        }

        // ────────────────────────────────────────────────────────
        // API : Récupérer les événements du calendrier
        // Retourne les colis filtrés par rôle et par plage de dates
        // ────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetEvents(string? start, string? end)
        {
            var query = _context.Colis
                .Include(c => c.Client)
                .Include(c => c.Livreur)
                .Where(c => c.Statut != StatutColis.Annulé);

            // Filtrer selon le rôle de l'utilisateur
            if (!User.IsInRole("Admin"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user?.TypeUtilisateur == UserType.Client && user.ClientId.HasValue)
                    query = query.Where(c => c.ClientId == user.ClientId.Value);
                else if (user?.TypeUtilisateur == UserType.Livreur && user.LivreurId.HasValue)
                    query = query.Where(c => c.LivreurId == user.LivreurId.Value);
                else
                    query = query.Where(c => false);
            }

            // Filtrer par plage de dates
            if (DateTime.TryParse(start, out var dateStart))
                query = query.Where(c => c.DateLivraison >= dateStart);
            if (DateTime.TryParse(end, out var dateEnd))
                query = query.Where(c => c.DateLivraison <= dateEnd);

            var colis = await query.ToListAsync();

            // Transformer en événements pour le calendrier (avec couleur par statut)
            var events = colis.Select(c => new
            {
                id = c.Id,
                title = $"#{c.Id} — {c.Description ?? "Colis"}",
                start = c.DateLivraison.ToString("yyyy-MM-dd"),
                color = c.Statut switch
                {
                    StatutColis.EnAttente => "#f59e0b",  // Jaune
                    StatutColis.EnCours => "#3b82f6",    // Bleu
                    StatutColis.Livré => "#10b981",      // Vert
                    _ => "#6b7280"                        // Gris
                },
                extendedProps = new
                {
                    statut = c.Statut.ToString(),
                    client = c.Client != null ? $"{c.Client.Nom} {c.Client.Prenom}" : "—",
                    livreur = c.Livreur?.RaisonSociale ?? "Non assigné",
                    livreurId = c.LivreurId,
                    montant = c.Montant,
                    poids = c.Poids,
                    description = c.Description ?? "—"
                }
            });

            return Json(events);
        }

        // ────────────────────────────────────────────────────────
        // API : Déplacer un colis (drag & drop) - ADMIN SEULEMENT
        // Change la date de livraison d'un colis
        // ────────────────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MoveEvent([FromBody] MoveEventDto dto)
        {
            var colis = await _context.Colis.Include(c => c.Client).FirstOrDefaultAsync(c => c.Id == dto.Id);
            if (colis == null) return NotFound(new { message = "Colis introuvable" });

            colis.DateLivraison = dto.NewDate;
            await _context.SaveChangesAsync();

            // Notifier le client du changement de date
            await _notif.NotifyClientByEmailAsync(
                colis.Client?.Email,
                "Date de livraison modifiée",
                $"La livraison de votre colis #{colis.Id} a été reprogrammée au {colis.DateLivraison:dd/MM/yyyy}.",
                NotificationType.Info,
                $"/Colis/Details/{colis.Id}");

            return Ok(new { message = "Date mise à jour", date = colis.DateLivraison.ToString("dd/MM/yyyy") });
        }

        // ────────────────────────────────────────────────────────
        // API : Assigner un livreur à un colis - ADMIN SEULEMENT
        // Si assigné, passe automatiquement le statut à "EnCours"
        // ────────────────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignLivreur([FromBody] AssignLivreurDto dto)
        {
            var colis = await _context.Colis.Include(c => c.Client).FirstOrDefaultAsync(c => c.Id == dto.ColisId);
            if (colis == null) return NotFound(new { message = "Colis introuvable" });

            colis.LivreurId = dto.LivreurId == 0 ? null : dto.LivreurId;

            // Passage automatique en "EnCours" si livreur assigné
            if (dto.LivreurId != 0 && colis.Statut == StatutColis.EnAttente)
                colis.Statut = StatutColis.EnCours;

            await _context.SaveChangesAsync();

            // Notifier le client
            var livreur = dto.LivreurId != 0
                ? await _context.Livreurs.FindAsync(dto.LivreurId)
                : null;

            if (livreur != null)
            {
                await _notif.NotifyClientByEmailAsync(
                    colis.Client?.Email,
                    "Livreur assigné à votre colis",
                    $"Le livreur « {livreur.RaisonSociale} » a été assigné à votre colis #{colis.Id}.",
                    NotificationType.Success,
                    $"/Colis/Details/{colis.Id}");
            }

            return Ok(new { message = "Livreur affecté", livreur = livreur?.RaisonSociale ?? "Non assigné" });
        }

        // ────────────────────────────────────────────────────────
        // API : Changer le statut d'un colis - ADMIN SEULEMENT
        // ────────────────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeStatut([FromBody] ChangeStatutDto dto)
        {
            var colis = await _context.Colis.Include(c => c.Client).FirstOrDefaultAsync(c => c.Id == dto.ColisId);
            if (colis == null) return NotFound(new { message = "Colis introuvable" });

            if (Enum.TryParse<StatutColis>(dto.Statut, out var statut))
            {
                colis.Statut = statut;
                await _context.SaveChangesAsync();

                // Déterminer le message de notification selon le statut
                var (label, ntype) = statut switch
                {
                    StatutColis.EnCours => ("En cours de livraison", NotificationType.Info),
                    StatutColis.Livré => ("Livré avec succès", NotificationType.Success),
                    StatutColis.Annulé => ("Livraison annulée", NotificationType.Danger),
                    _ => ("En attente", NotificationType.Warning)
                };

                await _notif.NotifyClientByEmailAsync(
                    colis.Client?.Email,
                    $"Statut de votre colis : {label}",
                    $"Le statut de votre colis #{colis.Id} est passé à « {label} ».",
                    ntype,
                    $"/Colis/Details/{colis.Id}");

                return Ok(new { message = "Statut mis à jour" });
            }
            return BadRequest(new { message = "Statut invalide" });
        }

        // ────────────────────────────────────────────────────────
        // API : Statistiques d'un jour précis - ADMIN SEULEMENT
        // ────────────────────────────────────────────────────────
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> StatsJour(string date)
        {
            if (!DateTime.TryParse(date, out var jour))
                return BadRequest();

            var colis = await _context.Colis
                .Where(c => c.DateLivraison.Date == jour.Date)
                .ToListAsync();

            return Json(new
            {
                total = colis.Count,
                enAttente = colis.Count(c => c.Statut == StatutColis.EnAttente),
                enCours = colis.Count(c => c.Statut == StatutColis.EnCours),
                livres = colis.Count(c => c.Statut == StatutColis.Livré),
                ca = colis.Sum(c => c.Montant)
            });
        }
    }

    // ════════════════════════════════════════════════════════
    // DTOs (objets de transfert pour les requêtes JSON)
    // ════════════════════════════════════════════════════════

    public class MoveEventDto
    {
        public int Id { get; set; }
        public DateTime NewDate { get; set; }
    }

    public class AssignLivreurDto
    {
        public int ColisId { get; set; }
        public int LivreurId { get; set; }
    }

    public class ChangeStatutDto
    {
        public int ColisId { get; set; }
        public string Statut { get; set; } = string.Empty;
    }
}