using GMAOAPI.Models.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.DTOs.ReadDTOs
{
    public class PlanificationDto
    {
        public int Id { get; set; }

        public DateTime DateDebut { get; set; }

        public DateTime DateFin { get; set; }

        public FrequencePlanification Frequence { get; set; }

        public InterventionDto? InterventionDto { get; set; }
        public bool IsArchived { get; set; }
        public DateTime? ProchaineGeneration { get; set; }
        public bool IsRecurring { get; set; } 
    }
}
