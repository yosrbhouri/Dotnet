using System.ComponentModel.DataAnnotations;

namespace DeliveryApp.Models
{
    public class Livreur
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le CIN est obligatoire")]
        [StringLength(20)]
        [Display(Name = "CIN")]
        public string CIN { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le code postal est obligatoire")]
        [StringLength(10)]
        [Display(Name = "Code Postal")]
        public string CodePostal { get; set; } = string.Empty;

        [Required(ErrorMessage = "La raison sociale est obligatoire")]
        [StringLength(200, MinimumLength = 2)]
        [Display(Name = "Raison Sociale")]
        public string RaisonSociale { get; set; } = string.Empty;

        [Required(ErrorMessage = "La ville est obligatoire")]
        [StringLength(100)]
        [Display(Name = "Ville")]
        public string Ville { get; set; } = string.Empty;

        // Navigation : véhicules appartenant à cette entreprise
        public ICollection<Vehicule> Vehicules { get; set; } = new List<Vehicule>();

        // Navigation : colis assignés à ce livreur
        public ICollection<Colis> Colis { get; set; } = new List<Colis>();
    }
}
