using GMAOAPI.Models.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.DTOs.CreateDTOs
{
    public class PlanificationCreateDto
    {

        [Required]
        public DateTime DateDebut { get; set; }

        [Required]
        public DateTime DateFin { get; set; }

        [Required]
        public FrequencePlanification Frequence { get; set; }
 
        public int InterventionId { get; set; }

        public InterventionCreateDto interventionCreateDto { get; set; }
        public bool IsRecurring { get; set; }
    }
}
