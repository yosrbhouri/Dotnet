using DeliveryApp.Data;
using DeliveryApp.Models;
using DeliveryApp.Services;
using DeliveryApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DeliveryApp.Controllers
{
    // ============================================================
    // CONTROLEUR COLIS - Gestion des colis (envois/livraisons)
    // Accès : tous les utilisateurs connectés
    // Sécurité :
    //   - Admin  → voit TOUS les colis
    //   - Client → voit seulement SES colis
    //   - Livreur → voit seulement les colis qui LUI sont assignés
    // ============================================================
    [Authorize]
    public class ColisController : Controller
    {
        private readonly IColisService _colisService;
        private readonly AppDbContext _context;
        private readonly INotificationService _notif;
        private readonly UserManager<ApplicationUser> _userManager;

        public ColisController(IColisService colisService, AppDbContext context,
            INotificationService notif, UserManager<ApplicationUser> userManager)
        {
            _colisService = colisService;
            _context = context;
            _notif = notif;
            _userManager = userManager;
        }

        // ────────────────────────────────────────────────────────
        // METHODE PRIVEE : Filtre les colis selon le rôle
        // - Admin  → retourne tous les colis
        // - Client → retourne les colis où ClientId = son ID
        // - Livreur → retourne les colis où LivreurId = son ID
        // ────────────────────────────────────────────────────────
        private async Task<IQueryable<Colis>> GetUserColisQuery()
        {
            var query = _context.Colis
                .Include(c => c.Client)
                .Include(c => c.Livreur)
                .AsQueryable();

            // Admin = accès total
            if (User.IsInRole("Admin"))
                return query;

            // Récupérer l'utilisateur connecté
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return query.Where(c => false);

            // Client = seulement ses colis
            if (user.TypeUtilisateur == UserType.Client && user.ClientId.HasValue)
                return query.Where(c => c.ClientId == user.ClientId.Value);

            // Livreur = seulement ses livraisons
            if (user.TypeUtilisateur == UserType.Livreur && user.LivreurId.HasValue)
                return query.Where(c => c.LivreurId == user.LivreurId.Value);

            // Aucun accès par défaut
            return query.Where(c => false);
        }

        // ────────────────────────────────────────────────────────
        // LISTE DES COLIS (avec filtre optionnel par statut)
        // ────────────────────────────────────────────────────────
        public async Task<IActionResult> Index(StatutColis? statut)
        {
            var query = await GetUserColisQuery();

            // Filtre par statut si demandé
            if (statut.HasValue)
                query = query.Where(c => c.Statut == statut.Value);

            var colis = await query.OrderByDescending(c => c.DateCreation).ToListAsync();
            ViewBag.SelectedStatut = statut;
            return View(colis);
        }

        // ────────────────────────────────────────────────────────
        // RECHERCHE AVANCEE (description, montant, statut)
        // ────────────────────────────────────────────────────────
        public async Task<IActionResult> Search(string? description, double? minMontant, double? maxMontant, StatutColis? statut)
        {
            var query = await GetUserColisQuery();

            // Appliquer les filtres de recherche
            if (!string.IsNullOrWhiteSpace(description))
                query = query.Where(c => c.Description != null && c.Description.Contains(description));
            if (minMontant.HasValue)
                query = query.Where(c => c.Montant >= minMontant.Value);
            if (maxMontant.HasValue)
                query = query.Where(c => c.Montant <= maxMontant.Value);
            if (statut.HasValue)
                query = query.Where(c => c.Statut == statut.Value);

            var vm = new ColisSearchViewModel
            {
                Description = description,
                MinMontant = minMontant,
                MaxMontant = maxMontant,
                Statut = statut,
                Resultats = await query.OrderByDescending(c => c.DateCreation).ToListAsync()
            };
            return View(vm);
        }

        // ────────────────────────────────────────────────────────
        // DETAILS D'UN COLIS
        // Sécurité : vérifie que le colis appartient à l'utilisateur
        // ────────────────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var colis = await _colisService.GetByIdAsync(id);
            if (colis == null) return NotFound();

            // Admin = accès direct
            if (!User.IsInRole("Admin"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Forbid();

                // Vérifier que ce colis lui appartient
                bool authorized = false;
                if (user.TypeUtilisateur == UserType.Client && user.ClientId.HasValue)
                    authorized = colis.ClientId == user.ClientId.Value;
                else if (user.TypeUtilisateur == UserType.Livreur && user.LivreurId.HasValue)
                    authorized = colis.LivreurId == user.LivreurId.Value;

                if (!authorized) return Forbid();
            }

            return View(colis);
        }

        // ────────────────────────────────────────────────────────
        // CREER UN COLIS - Réservé aux CLIENTS uniquement
        // Le livreur ne peut PAS créer de colis
        // Le ClientId est forcé automatiquement (sécurité)
        // ────────────────────────────────────────────────────────
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);

            // Bloquer si ce n'est pas un client
            if (user?.TypeUtilisateur != UserType.Client || !user.ClientId.HasValue)
                return Forbid();

            var colis = new Colis
            {
                DateLivraison = DateTime.Now.AddDays(1),
                ClientId = user.ClientId.Value
            };

            ViewBag.IsClient = true;
            LoadDropdowns(user);
            return View(colis);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Create(Colis colis)
        {
            var user = await _userManager.GetUserAsync(User);

            // Bloquer si ce n'est pas un client
            if (user?.TypeUtilisateur != UserType.Client || !user.ClientId.HasValue)
                return Forbid();

            // Forcer le ClientId (empêche la manipulation du formulaire)
            colis.ClientId = user.ClientId.Value;

            // Forcer le statut à EnAttente (le client ne choisit pas le statut)
            colis.Statut = StatutColis.EnAttente;

            if (ModelState.IsValid)
            {
                await _colisService.CreateAsync(colis);

                // Notifier le client
                var client = await _context.Clients.FindAsync(colis.ClientId);
                await _notif.NotifyClientByEmailAsync(
                    client?.Email,
                    "Nouveau colis enregistré",
                    $"Votre colis #{colis.Id} a été enregistré. Livraison prévue le {colis.DateLivraison:dd/MM/yyyy}.",
                    NotificationType.Info,
                    $"/Colis/Details/{colis.Id}");

                TempData["Success"] = "Colis créé avec succès !";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.IsClient = true;
            LoadDropdowns(user);
            return View(colis);
        }

        // ────────────────────────────────────────────────────────
        // MODIFIER UN COLIS - Réservé à l'ADMIN
        // ────────────────────────────────────────────────────────
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var colis = await _colisService.GetByIdAsync(id);
            if (colis == null) return NotFound();
            LoadDropdowns();
            return View(colis);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Colis colis)
        {
            if (id != colis.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                await _colisService.UpdateAsync(colis);

                // Notifier le client de la mise à jour
                var client = await _context.Clients.FindAsync(colis.ClientId);
                await _notif.NotifyClientByEmailAsync(
                    client?.Email,
                    "Colis mis à jour",
                    $"Les informations de votre colis #{colis.Id} ont été mises à jour.",
                    NotificationType.Info,
                    $"/Colis/Details/{colis.Id}");

                TempData["Success"] = "Colis mis à jour avec succès !";
                return RedirectToAction(nameof(Index));
            }
            LoadDropdowns();
            return View(colis);
        }

        // ────────────────────────────────────────────────────────
        // SUPPRIMER UN COLIS - Réservé à l'ADMIN
        // ────────────────────────────────────────────────────────
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var colis = await _colisService.GetByIdAsync(id);
            if (colis == null) return NotFound();
            return View(colis);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _colisService.DeleteAsync(id);
            TempData["Success"] = "Colis supprimé avec succès !";
            return RedirectToAction(nameof(Index));
        }

        // ────────────────────────────────────────────────────────
        // METHODE PRIVEE : Charger les listes déroulantes
        // - Client → voit seulement lui-même dans la liste
        // - Admin → voit tous les clients
        // ────────────────────────────────────────────────────────
        private void LoadDropdowns(ApplicationUser? user = null)
        {
            if (user?.TypeUtilisateur == UserType.Client && user.ClientId.HasValue)
                ViewBag.Clients = new SelectList(
                    _context.Clients.Where(c => c.Id == user.ClientId.Value), "Id", "Nom");
            else
                ViewBag.Clients = new SelectList(
                    _context.Clients.OrderBy(c => c.Nom), "Id", "Nom");

            ViewBag.Livreurs = new SelectList(
                _context.Livreurs.OrderBy(l => l.RaisonSociale), "Id", "RaisonSociale");
        }
    }
}