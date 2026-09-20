using DeliveryApp.Data;
using DeliveryApp.Models;
using Microsoft.EntityFrameworkCore;

namespace DeliveryApp.Services
{
    public class ColisService : IColisService
    {
        private readonly AppDbContext _context;

        public ColisService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Colis>> GetAllAsync()
        {
            return await _context.Colis
                .Include(c => c.Client)
                .Include(c => c.Livreur)
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();
        }

        public async Task<Colis?> GetByIdAsync(int id)
        {
            return await _context.Colis
                .Include(c => c.Client)
                .Include(c => c.Livreur)
                    .ThenInclude(l => l!.Vehicules)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Colis>> SearchAsync(
            string? description,
            double? minMontant,
            double? maxMontant,
            StatutColis? statut)
        {
            var query = _context.Colis
                .Include(c => c.Client)
                .Include(c => c.Livreur)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(description))
                query = query.Where(c => c.Description != null &&
                    c.Description.Contains(description));

            if (minMontant.HasValue)
                query = query.Where(c => c.Montant >= minMontant.Value);

            if (maxMontant.HasValue)
                query = query.Where(c => c.Montant <= maxMontant.Value);

            if (statut.HasValue)
                query = query.Where(c => c.Statut == statut.Value);

            return await query.OrderByDescending(c => c.DateCreation).ToListAsync();
        }

        public async Task<Colis> CreateAsync(Colis colis)
        {
            colis.DateCreation = DateTime.Now;
            await _context.Colis.AddAsync(colis);
            await _context.SaveChangesAsync();
            return colis;
        }

        public async Task<Colis> UpdateAsync(Colis colis)
        {
            _context.Colis.Update(colis);
            await _context.SaveChangesAsync();
            return colis;
        }

        public async Task DeleteAsync(int id)
        {
            var colis = await _context.Colis.FindAsync(id);
            if (colis != null)
            {
                _context.Colis.Remove(colis);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<ColisStatistiques> GetStatistiquesAsync()
        {
            var colis = await _context.Colis.ToListAsync();
            return new ColisStatistiques
            {
                TotalColis = colis.Count,
                ColisEnAttente = colis.Count(c => c.Statut == StatutColis.EnAttente),
                ColisEnCours = colis.Count(c => c.Statut == StatutColis.EnCours),
                ColisLivres = colis.Count(c => c.Statut == StatutColis.Livré),
                ColisAnnules = colis.Count(c => c.Statut == StatutColis.Annulé),
                MontantTotal = colis.Sum(c => c.Montant),
                MontantMoyen = colis.Count > 0 ? colis.Average(c => c.Montant) : 0,
                TotalClients = await _context.Clients.CountAsync(),
                TotalLivreurs = await _context.Livreurs.CountAsync(),
                TotalVehicules = await _context.Vehicules.CountAsync()
            };
        }
    }
}
