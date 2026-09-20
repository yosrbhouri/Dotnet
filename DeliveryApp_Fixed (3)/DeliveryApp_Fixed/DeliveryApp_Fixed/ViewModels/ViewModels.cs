using System.ComponentModel.DataAnnotations;
using DeliveryApp.Models;

namespace DeliveryApp.ViewModels
{
    // ─── Authentification ────────────────────────────────────────────────────

    public class LoginViewModel
    {
        [Required(ErrorMessage = "L'email est obligatoire")]
        [EmailAddress(ErrorMessage = "Email invalide")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est obligatoire")]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Se souvenir de moi")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Le nom complet est obligatoire")]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Nom complet")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est obligatoire")]
        [EmailAddress(ErrorMessage = "Email invalide")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est obligatoire")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmer le mot de passe")]
        [Compare("Password", ErrorMessage = "Les mots de passe ne correspondent pas")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // ─── Type d'utilisateur ─────────────────────────────────────────────
        [Required(ErrorMessage = "Veuillez choisir votre type de compte")]
        [Display(Name = "Type de compte")]
        public UserType TypeUtilisateur { get; set; }

        // ─── Champs Client ──────────────────────────────────────────────────
        [Display(Name = "Téléphone")]
        [Phone(ErrorMessage = "Numéro invalide")]
        public string? Telephone { get; set; }

        [Display(Name = "Ville")]
        public string? Ville { get; set; }

        [Display(Name = "Code Postal")]
        public string? CodePostal { get; set; }

        // ─── Champs Livreur ─────────────────────────────────────────────────
        [Display(Name = "CIN")]
        public string? CIN { get; set; }

        [Display(Name = "Raison Sociale / Nom entreprise")]
        public string? RaisonSociale { get; set; }
    }

    // ─── Dashboard Stats ────────────────────────────────────────────────────

    public class DashboardViewModel
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
        public IEnumerable<Colis> RecentColis { get; set; } = new List<Colis>();
        public UserType? TypeUtilisateur { get; set; }
        public string? FullName { get; set; }
        public bool IsAdmin { get; set; }
    }

    // ─── Search ─────────────────────────────────────────────────────────────

    public class ColisSearchViewModel
    {
        public string? Description { get; set; }
        public double? MinMontant { get; set; }
        public double? MaxMontant { get; set; }
        public StatutColis? Statut { get; set; }
        public IEnumerable<Colis> Resultats { get; set; } = new List<Colis>();
    }
}
