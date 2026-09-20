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
    // API COLIS - Endpoints REST pour les colis
    // Route : /api/colisapi
    // Sécurité :
    //   - GET (liste, détail) → filtré par rôle (comme le contrôleur MVC)
    //   - POST, PUT, DELETE, PATCH → ADMIN uniquement
    // ============================================================
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ColisApiController : ControllerBase
    {
        private readonly IColisService _colisService;
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ColisApiController(IColisService colisService, AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _colisService = colisService;
            _context = context;
            _userManager = userManager;
        }

        // Filtre les colis selon le rôle (même logique que ColisController)
        private async Task<IQueryable<Colis>> GetFilteredQuery()
        {
            var query = _context.Colis
                .Include(c => c.Client)
                .Include(c => c.Livreur)
                .AsQueryable();

            // Admin = tous les colis
            if (User.IsInRole("Admin")) return query;

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return query.Where(c => false);

            // Client = ses colis uniquement
            if (user.TypeUtilisateur == UserType.Client && user.ClientId.HasValue)
                return query.Where(c => c.ClientId == user.ClientId.Value);

            // Livreur = ses livraisons uniquement
            if (user.TypeUtilisateur == UserType.Livreur && user.LivreurId.HasValue)
                return query.Where(c => c.LivreurId == user.LivreurId.Value);

            return query.Where(c => false);
        }

        // ── GET /api/colisapi → Liste des colis (filtrée par rôle) ──
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var query = await GetFilteredQuery();
            var colis = await query.OrderByDescending(c => c.DateCreation).ToListAsync();
            return Ok(colis.Select(c => new
            {
                c.Id, c.Description, c.Montant, c.Poids, c.Volume,
                c.Statut, c.DateLivraison, c.DateCreation,
                Client = c.Client == null ? null : new { c.Client.Nom, c.Client.Prenom, c.Client.Ville },
                Livreur = c.Livreur == null ? null : new { c.Livreur.RaisonSociale }
            }));
        }

        // ── GET /api/colisapi/5 → Détail d'un colis (vérifie l'accès) ──
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var colis = await _colisService.GetByIdAsync(id);
            if (colis == null) return NotFound(new { message = $"Colis #{id} introuvable" });

            // Vérifier que l'utilisateur a le droit de voir ce colis
            if (!User.IsInRole("Admin"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Forbid();

                bool ok = false;
                if (user.TypeUtilisateur == UserType.Client && user.ClientId.HasValue)
                    ok = colis.ClientId == user.ClientId.Value;
                else if (user.TypeUtilisateur == UserType.Livreur && user.LivreurId.HasValue)
                    ok = colis.LivreurId == user.LivreurId.Value;

                if (!ok) return Forbid();
            }

            return Ok(colis);
        }

        // ── GET /api/colisapi/stats → Statistiques globales (ADMIN) ──
        [HttpGet("stats")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetStats()
        {
            return Ok(await _colisService.GetStatistiquesAsync());
        }

        // ── POST /api/colisapi → Créer un colis (ADMIN) ──
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] Colis colis)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var created = await _colisService.CreateAsync(colis);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // ── PUT /api/colisapi/5 → Modifier un colis (ADMIN) ──
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] Colis colis)
        {
            if (id != colis.Id) return BadRequest();
            if (!ModelState.IsValid) return BadRequest(ModelState);
            return Ok(await _colisService.UpdateAsync(colis));
        }

        // ── DELETE /api/colisapi/5 → Supprimer un colis (ADMIN) ──
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _colisService.GetByIdAsync(id);
            if (existing == null) return NotFound();
            await _colisService.DeleteAsync(id);
            return NoContent();
        }

        // ── PATCH /api/colisapi/5/statut → Changer le statut (ADMIN) ──
        [HttpPatch("{id}/statut")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatut(int id, [FromBody] StatutUpdateDto dto)
        {
            var colis = await _context.Colis.FindAsync(id);
            if (colis == null) return NotFound();
            colis.Statut = dto.Statut;
            await _context.SaveChangesAsync();
            return Ok(new { id, statut = colis.Statut.ToString() });
        }
    }

    // ============================================================
    // API CLIENTS - Endpoints REST pour les clients
    // Route : /api/clientsapi
    // Accès : ADMIN uniquement (données confidentielles)
    // ============================================================
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ClientsApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClientsApiController(AppDbContext context) => _context = context;

        // ── GET /api/clientsapi → Liste de tous les clients ──
        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _context.Clients
                .Select(c => new { c.Id, c.Nom, c.Prenom, c.Ville, c.Email, c.Telephone, NbColis = c.Colis.Count })
                .ToListAsync());

        // ── GET /api/clientsapi/5 → Détail d'un client ──
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var c = await _context.Clients.Include(x => x.Colis).FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) return NotFound();
            return Ok(c);
        }
    }

    // ════════════════════════════════════════════════════════
    // DTO pour la mise à jour du statut
    // ════════════════════════════════════════════════════════
    public class StatutUpdateDto
    {
        public StatutColis Statut { get; set; }
    }
}
