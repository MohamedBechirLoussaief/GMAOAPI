using GMAOAPI.Models.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.DTOs.UpdateDTOs
{
    public class InterventionUpdateDto
    {
        public string Description { get; set; }

        public DateTime? DateDebut { get; set; }

        public DateTime? DateFin { get; set; }


        public int? RapportId { get; set; }

    }
}
