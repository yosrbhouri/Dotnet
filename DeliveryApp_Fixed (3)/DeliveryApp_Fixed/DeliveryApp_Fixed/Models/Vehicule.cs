using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliveryApp.Models
{
    /// <summary>
    /// Classe de base pour tous les véhicules (TPH - Table Per Hierarchy)
    /// Chaque véhicule appartient à une seule entreprise de livraison (Livreur)
    /// </summary>
    public abstract class Vehicule
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La couleur est obligatoire")]
        [StringLength(50, MinimumLength = 2)]
        [Display(Name = "Couleur")]
        public string Couleur { get; set; } = string.Empty;

        [Required(ErrorMessage = "La marque est obligatoire")]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Marque")]
        public string Marque { get; set; } = string.Empty;

        [Required(ErrorMessage = "La matricule est obligatoire")]
        [StringLength(20)]
        [RegularExpression(@"^[A-Z0-9\s\-]+$", ErrorMessage = "Format de matricule invalide")]
        [Display(Name = "Matricule")]
        public string Matricule { get; set; } = string.Empty;

        [Required]
        [Range(50, 300, ErrorMessage = "La vitesse limite doit être entre 50 et 300 km/h")]
        [Display(Name = "Vitesse Limite (km/h)")]
        public int VitesseLimite { get; set; }

        // FK vers Livreur (entreprise propriétaire du véhicule)
        [Display(Name = "Entreprise")]
        public int? LivreurId { get; set; }

        [ForeignKey("LivreurId")]
        public Livreur? Livreur { get; set; }
    }
}
