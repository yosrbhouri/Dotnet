using System.ComponentModel.DataAnnotations;

namespace DeliveryApp.Models
{
    public class Client
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le code postal est obligatoire")]
        [StringLength(10)]
        [Display(Name = "Code Postal")]
        public string CodePostal { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom est obligatoire")]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Nom")]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est obligatoire")]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Prénom")]
        public string Prenom { get; set; } = string.Empty;

        [Required(ErrorMessage = "La ville est obligatoire")]
        [StringLength(100)]
        [Display(Name = "Ville")]
        public string Ville { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email invalide")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Numéro de téléphone invalide")]
        [Display(Name = "Téléphone")]
        public string? Telephone { get; set; }

        // Navigation
        public ICollection<Colis> Colis { get; set; } = new List<Colis>();
    }
}
