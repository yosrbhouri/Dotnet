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
    // CONTROLEUR ADMIN - Panel d'administration
    // Accès : UNIQUEMENT les administrateurs (role "Admin")
    // Fonctionnalités :
    //   - Statistiques générales
    //   - CRUD Clients, Livreurs, Véhicules
    //   - Gestion des comptes utilisateurs
    // ============================================================
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IColisService _colisService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(AppDbContext context, IColisService colisService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _colisService = colisService;
            _userManager = userManager;
        }

        // ────────────────────────────────────────────────────────
        // PAGE D'ACCUEIL ADMIN - Statistiques + derniers colis
        // ────────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewBag.Stats = await _colisService.GetStatistiquesAsync();
            ViewBag.RecentColis = await _context.Colis
                .Include(c => c.Client)
                .Include(c => c.Livreur)
                .OrderByDescending(c => c.DateCreation)
                .Take(10)
                .ToListAsync();
            return View();
        }

        // ════════════════════════════════════════════════════════
        //                    GESTION DES CLIENTS
        // ════════════════════════════════════════════════════════

        // Liste de tous les clients
        public async Task<IActionResult> Clients() =>
            View(await _context.Clients.Include(c => c.Colis).ToListAsync());

        // Formulaire de création
        public IActionResult CreateClient() => View();

        // Enregistrer un nouveau client
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClient(Client client)
        {
            if (ModelState.IsValid)
            {
                _context.Clients.Add(client);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Client créé avec succès !";
                return RedirectToAction(nameof(Clients));
            }
            return View(client);
        }

        // Formulaire de modification
        public async Task<IActionResult> EditClient(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null) return NotFound();
            return View(client);
        }

        // Enregistrer les modifications
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditClient(Client client)
        {
            if (ModelState.IsValid)
            {
                _context.Clients.Update(client);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Client mis à jour !";
                return RedirectToAction(nameof(Clients));
            }
            return View(client);
        }

        // Supprimer un client
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClient(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client != null)
            {
                _context.Clients.Remove(client);
                await _context.SaveChangesAsync();
            }
            TempData["Success"] = "Client supprimé !";
            return RedirectToAction(nameof(Clients));
        }

        // ════════════════════════════════════════════════════════
        //                   GESTION DES LIVREURS
        // ════════════════════════════════════════════════════════

        // Liste de tous les livreurs (avec leurs véhicules et colis)
        public async Task<IActionResult> Livreurs() =>
            View(await _context.Livreurs.Include(l => l.Vehicules).Include(l => l.Colis).ToListAsync());

        // Formulaire de création
        public IActionResult CreateLivreur() => View();

        // Enregistrer un nouveau livreur
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLivreur(Livreur livreur)
        {
            if (ModelState.IsValid)
            {
                _context.Livreurs.Add(livreur);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Livreur créé avec succès !";
                return RedirectToAction(nameof(Livreurs));
            }
            return View(livreur);
        }

        // Formulaire de modification
        public async Task<IActionResult> EditLivreur(int id)
        {
            var livreur = await _context.Livreurs.FindAsync(id);
            if (livreur == null) return NotFound();
            return View(livreur);
        }

        // Enregistrer les modifications
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLivreur(Livreur livreur)
        {
            if (ModelState.IsValid)
            {
                _context.Livreurs.Update(livreur);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Livreur mis à jour !";
                return RedirectToAction(nameof(Livreurs));
            }
            return View(livreur);
        }

        // Supprimer un livreur
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLivreur(int id)
        {
            var livreur = await _context.Livreurs.FindAsync(id);
            if (livreur != null)
            {
                _context.Livreurs.Remove(livreur);
                await _context.SaveChangesAsync();
            }
            TempData["Success"] = "Livreur supprimé !";
            return RedirectToAction(nameof(Livreurs));
        }

        // ════════════════════════════════════════════════════════
        //                  GESTION DES VEHICULES
        // Chaque véhicule appartient à une entreprise (Livreur)
        // La matricule doit être UNIQUE dans toute la BDD
        // ════════════════════════════════════════════════════════

        // Liste de tous les véhicules (avec l'entreprise propriétaire)
        public async Task<IActionResult> Vehicules() =>
            View(await _context.Vehicules.Include(v => v.Livreur).ToListAsync());

        // Formulaire de création
        public async Task<IActionResult> CreateVehicule()
        {
            ViewBag.Livreurs = await _context.Livreurs.OrderBy(l => l.RaisonSociale).ToListAsync();
            return View();
        }

        // Créer un véhicule (Camion ou Voiture selon le type choisi)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVehicule(string type, string couleur,
            string marque, string matricule, int vitesseLimite,
            int? capacite, int? nbrEssieux, int? nbrPlaces, int? livreurId)
        {
            // Validation matricule unique
            if (await _context.Vehicules.AnyAsync(v => v.Matricule == matricule))
            {
                TempData["Error"] = $"La matricule \"{matricule}\" existe déjà. Chaque véhicule doit avoir une matricule unique.";
                ViewBag.Livreurs = await _context.Livreurs.OrderBy(l => l.RaisonSociale).ToListAsync();
                return View();
            }

            // Créer le bon type de véhicule selon le choix
            Vehicule vehicule = type == "Camion"
                ? new Camion
                {
                    Couleur = couleur, Marque = marque, Matricule = matricule,
                    VitesseLimite = vitesseLimite, Capacite = capacite ?? 0,
                    NbrEssieux = nbrEssieux ?? 2, LivreurId = livreurId
                }
                : new Voiture
                {
                    Couleur = couleur, Marque = marque, Matricule = matricule,
                    VitesseLimite = vitesseLimite, NbrPlaces = nbrPlaces ?? 5,
                    LivreurId = livreurId
                };

            _context.Vehicules.Add(vehicule);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Véhicule créé avec succès !";
            return RedirectToAction(nameof(Vehicules));
        }

        // Formulaire de modification d'un véhicule
        public async Task<IActionResult> EditVehicule(int id)
        {
            var vehicule = await _context.Vehicules.FindAsync(id);
            if (vehicule == null) return NotFound();
            ViewBag.Livreurs = await _context.Livreurs.OrderBy(l => l.RaisonSociale).ToListAsync();
            return View(vehicule);
        }

        // Enregistrer les modifications du véhicule
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVehicule(int id, string couleur,
            string marque, string matricule, int vitesseLimite,
            int? capacite, int? nbrEssieux, int? nbrPlaces, int? livreurId)
        {
            var vehicule = await _context.Vehicules.FindAsync(id);
            if (vehicule == null) return NotFound();

            // Validation matricule unique (exclure le véhicule en cours de modification)
            if (await _context.Vehicules.AnyAsync(v => v.Matricule == matricule && v.Id != id))
            {
                TempData["Error"] = $"La matricule \"{matricule}\" est déjà utilisée par un autre véhicule.";
                ViewBag.Livreurs = await _context.Livreurs.OrderBy(l => l.RaisonSociale).ToListAsync();
                return View(vehicule);
            }

            // Mettre à jour les propriétés communes
            vehicule.Couleur = couleur;
            vehicule.Marque = marque;
            vehicule.Matricule = matricule;
            vehicule.VitesseLimite = vitesseLimite;
            vehicule.LivreurId = livreurId;

            // Mettre à jour les propriétés spécifiques au type
            if (vehicule is Camion camion)
            {
                camion.Capacite = capacite ?? camion.Capacite;
                camion.NbrEssieux = nbrEssieux ?? camion.NbrEssieux;
            }
            else if (vehicule is Voiture voiture)
            {
                voiture.NbrPlaces = nbrPlaces ?? voiture.NbrPlaces;
            }

            _context.Vehicules.Update(vehicule);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Véhicule mis à jour !";
            return RedirectToAction(nameof(Vehicules));
        }

        // Supprimer un véhicule
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVehicule(int id)
        {
            var vehicule = await _context.Vehicules.FindAsync(id);
            if (vehicule != null)
            {
                _context.Vehicules.Remove(vehicule);
                await _context.SaveChangesAsync();
            }
            TempData["Success"] = "Véhicule supprimé !";
            return RedirectToAction(nameof(Vehicules));
        }

        // ════════════════════════════════════════════════════════
        //              GESTION DES UTILISATEURS
        // ════════════════════════════════════════════════════════

        // Liste de tous les utilisateurs avec leurs rôles
        public async Task<IActionResult> Users()
        {
            var users = _userManager.Users.ToList();
            var userRoles = new Dictionary<string, IList<string>>();
            foreach (var u in users)
                userRoles[u.Id] = await _userManager.GetRolesAsync(u);
            ViewBag.UserRoles = userRoles;
            return View(users);
        }

        // ────────────────────────────────────────────────────────
        // ACTIVER/DESACTIVER un compte utilisateur
        // Sécurité : mot de passe admin requis + impossible de désactiver un admin
        // ────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string userId, string currentPassword)
        {
            var target = await _userManager.FindByIdAsync(userId);
            if (target == null)
            {
                TempData["Error"] = "Utilisateur introuvable.";
                return RedirectToAction(nameof(Users));
            }

            // Vérifier que l'admin a entré son mot de passe
            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                TempData["Error"] = "Le mot de passe est requis pour confirmer cette action.";
                return RedirectToAction(nameof(Users));
            }

            // Vérifier le mot de passe de l'admin connecté
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !await _userManager.CheckPasswordAsync(currentUser, currentPassword))
            {
                TempData["Error"] = "Mot de passe incorrect. Action annulée.";
                return RedirectToAction(nameof(Users));
            }

            // PROTECTION : impossible de désactiver un admin
            var targetRoles = await _userManager.GetRolesAsync(target);
            if (targetRoles.Contains("Admin"))
            {
                TempData["Error"] = "Un compte administrateur ne peut pas être désactivé.";
                return RedirectToAction(nameof(Users));
            }

            // Basculer le statut actif/inactif
            target.IsActive = !target.IsActive;
            await _userManager.UpdateAsync(target);

            TempData["Success"] = target.IsActive ? "Compte activé." : "Compte désactivé.";
            return RedirectToAction(nameof(Users));
        }

        // ────────────────────────────────────────────────────────
        // SUPPRIMER un compte utilisateur
        // Sécurité : mot de passe admin requis + impossible de supprimer un admin
        // ────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId, string currentPassword)
        {
            var target = await _userManager.FindByIdAsync(userId);
            if (target == null)
            {
                TempData["Error"] = "Utilisateur introuvable.";
                return RedirectToAction(nameof(Users));
            }

            // PROTECTION : impossible de supprimer un admin
            var targetRoles = await _userManager.GetRolesAsync(target);
            if (targetRoles.Contains("Admin"))
            {
                TempData["Error"] = "Impossible de supprimer un compte administrateur.";
                return RedirectToAction(nameof(Users));
            }

            // Vérifier le mot de passe de l'admin connecté
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !await _userManager.CheckPasswordAsync(currentUser, currentPassword ?? ""))
            {
                TempData["Error"] = "Mot de passe incorrect. Suppression annulée.";
                return RedirectToAction(nameof(Users));
            }

            await _userManager.DeleteAsync(target);
            TempData["Success"] = "Utilisateur supprimé.";
            return RedirectToAction(nameof(Users));
        }
    }
}
