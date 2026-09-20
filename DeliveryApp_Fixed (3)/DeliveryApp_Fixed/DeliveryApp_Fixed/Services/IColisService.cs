using DeliveryApp.Models;

namespace DeliveryApp.Services
{
    public interface IColisService
    {
        Task<IEnumerable<Colis>> GetAllAsync();
        Task<Colis?> GetByIdAsync(int id);
        Task<IEnumerable<Colis>> SearchAsync(string? description, double? minMontant, double? maxMontant, StatutColis? statut);
        Task<Colis> CreateAsync(Colis colis);
        Task<Colis> UpdateAsync(Colis colis);
        Task DeleteAsync(int id);
        Task<ColisStatistiques> GetStatistiquesAsync();
    }

    public class ColisStatistiques
    {
        public int TotalColis { get; set; }
        public int ColisEnAttente { get; set; }
        public int ColisEnCours { get; set; }
        public int ColisLivres { get; set; }
        public int ColisAnnules { get; set; }
        public double MontantTotal { get; set; }
        public double MontantMoyen { get; set; }
        public int TotalClients { get; set; }
        public int TotalLivreurs { get; set; }
        public int TotalVehicules { get; set; }
    }
}
