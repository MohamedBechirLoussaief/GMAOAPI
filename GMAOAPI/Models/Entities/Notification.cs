using GMAOAPI.Models.Enumerations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GMAOAPI.Models.Entities
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Message { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [ForeignKey("Utilisateur")]
        public string DestinataireId { get; set; }
        public Utilisateur? Destinataire { get; set; }

        [Required]
        public StatutNotification Statut { get; set; } = StatutNotification.Envoyee;

    }
}