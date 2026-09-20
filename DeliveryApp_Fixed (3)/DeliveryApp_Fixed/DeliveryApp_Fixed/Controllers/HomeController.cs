using DeliveryApp.Data;
using DeliveryApp.Models;
using DeliveryApp.Services;
using DeliveryApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeliveryApp.Controllers
{
    // ============================================================
    // CONTROLEUR HOME - Tableau de bord (Dashboard)
    // Accès : tous les utilisateurs connectés
    // Affichage :
    //   - Admin  → statistiques globales + tous les colis récents
    //   - Client → ses propres statistiques + ses colis
    //   - Livreur → ses livraisons + statistiques personnelles
    // ============================================================
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IColisService _colisService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;

        public HomeController(IColisService colisService, UserManager<ApplicationUser> userManager, AppDbContext context)
        {
            _colisService = colisService;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Récupérer l'utilisateur connecté
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            // ── ADMIN : afficher toutes les statistiques globales ──
            if (isAdmin)
            {
                var stats = await _colisService.GetStatistiquesAsync();
                var recent = await _colisService.GetAllAsync();

                return View(new DashboardViewModel
                {
                    TotalColis = stats.TotalColis,
                    ColisEnAttente = stats.ColisEnAttente,
                    ColisEnCours = stats.ColisEnCours,
                    ColisLivres = stats.ColisLivres,
                    ColisAnnules = stats.ColisAnnules,
                    MontantTotal = stats.MontantTotal,
                    MontantMoyen = stats.MontantMoyen,
                    TotalClients = stats.TotalClients,
                    TotalLivreurs = stats.TotalLivreurs,
                    TotalVehicules = stats.TotalVehicules,
                    RecentColis = recent.Take(5),
                    IsAdmin = true,
                    FullName = user.FullName
                });
            }

            // ── CLIENT / LIVREUR : afficher seulement ses propres colis ──
            List<Colis> userColis;

            if (user.TypeUtilisateur == UserType.Client && user.ClientId.HasValue)
            {
                // Client → ses commandes
                userColis = await _context.Colis
                    .Include(c => c.Client)
                    .Include(c => c.Livreur)
                    .Where(c => c.ClientId == user.ClientId.Value)
                    .OrderByDescending(c => c.DateCreation)
                    .ToListAsync();
            }
            else if (user.TypeUtilisateur == UserType.Livreur && user.LivreurId.HasValue)
            {
                // Livreur → ses livraisons assignées
                userColis = await _context.Colis
                    .Include(c => c.Client)
                    .Include(c => c.Livreur)
                    .Where(c => c.LivreurId == user.LivreurId.Value)
                    .OrderByDescending(c => c.DateCreation)
                    .ToListAsync();
            }
            else
            {
                userColis = new List<Colis>();
            }

            // Calculer les statistiques personnelles
            return View(new DashboardViewModel
            {
                TotalColis = userColis.Count,
                ColisEnAttente = userColis.Count(c => c.Statut == StatutColis.EnAttente),
                ColisEnCours = userColis.Count(c => c.Statut == StatutColis.EnCours),
                ColisLivres = userColis.Count(c => c.Statut == StatutColis.Livré),
                ColisAnnules = userColis.Count(c => c.Statut == StatutColis.Annulé),
                MontantTotal = userColis.Sum(c => c.Montant),
                RecentColis = userColis.Take(10),
                IsAdmin = false,
                TypeUtilisateur = user.TypeUtilisateur,
                FullName = user.FullName
            });
        }
    }
}
