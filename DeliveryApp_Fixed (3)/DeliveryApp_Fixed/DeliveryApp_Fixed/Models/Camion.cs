using System.ComponentModel.DataAnnotations;

namespace DeliveryApp.Models
{
    public class Camion : Vehicule
    {
        [Required]
        [Range(1, 50000, ErrorMessage = "La capacité doit être entre 1 et 50000 kg")]
        [Display(Name = "Capacité (kg)")]
        public int Capacite { get; set; }

        [Required]
        [Range(1, 10, ErrorMessage = "Le nombre d'essieux doit être entre 1 et 10")]
        [Display(Name = "Nombre d'essieux")]
        public int NbrEssieux { get; set; }
    }

    public class Voiture : Vehicule
    {
        [Required]
        [Range(2, 9, ErrorMessage = "Le nombre de places doit être entre 2 et 9")]
        [Display(Name = "Nombre de places")]
        public int NbrPlaces { get; set; }
    }
}
