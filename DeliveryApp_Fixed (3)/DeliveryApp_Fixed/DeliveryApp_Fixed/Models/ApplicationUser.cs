using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliveryApp.Models
{
    public enum UserType
    {
        Client,
        Livreur
    }

    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        [Display(Name = "Nom complet")]
        public string? FullName { get; set; }

        [Display(Name = "Date d'inscription")]
        public DateTime DateInscription { get; set; } = DateTime.Now;

        [Display(Name = "Actif")]
        public bool IsActive { get; set; } = true;

        [StringLength(255)]
        [Display(Name = "Photo de profil")]
        public string? ProfilePicturePath { get; set; }

        [Display(Name = "Type d'utilisateur")]
        public UserType? TypeUtilisateur { get; set; }

        public int? ClientId { get; set; }
        [ForeignKey("ClientId")]
        public Client? Client { get; set; }

        public int? LivreurId { get; set; }
        [ForeignKey("LivreurId")]
        public Livreur? Livreur { get; set; }
    }
}
