using GMAOAPI.Models.Entities;
using GMAOAPI.Models.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.DTOs.CreateDTOs
{
    public class RapportCreateDto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Titre { get; set; }

        [Required]
        public string Contenu { get; set; }

        public int InterventionId { get; set; }

        public DateTime DateDebut { get; set; }
        [Required]

        public DateTime DateFin { get; set; }

        [Required]
        public ResultatIntervention Resultat { get; set; }

    }
}
