using System.ComponentModel.DataAnnotations;

namespace DeliveryApp.Models
{
    public enum StatutColis
    {
        EnAttente,
        EnCours,
        Livré,
        Annulé
    }

    public class Colis
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La date de livraison est obligatoire")]
        [Display(Name = "Date de Livraison")]
        [DataType(DataType.DateTime)]
        public DateTime DateLivraison { get; set; }

        [Required]
        [Range(0.01, 100000, ErrorMessage = "Le montant doit être positif")]
        [Display(Name = "Montant (DT)")]
        [DataType(DataType.Currency)]
        public double Montant { get; set; }

        [Required]
        [Range(0.001, 10000, ErrorMessage = "Le poids doit être positif")]
        [Display(Name = "Poids (kg)")]
        public double Poids { get; set; }

        [Required]
        [Range(0.001, 100, ErrorMessage = "Le volume doit être positif")]
        [Display(Name = "Volume (m³)")]
        public double Volume { get; set; }

        [StringLength(200)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Statut")]
        public StatutColis Statut { get; set; } = StatutColis.EnAttente;

        // FK
        public int ClientId { get; set; }
        public Client? Client { get; set; }

        public int? LivreurId { get; set; }
        public Livreur? Livreur { get; set; }

        [Display(Name = "Date de Création")]
        public DateTime DateCreation { get; set; } = DateTime.Now;
    }
}
