using GMAOAPI.DTOs.AuthDTO;
using GMAOAPI.Models.Enumerations;

namespace GMAOAPI.DTOs.ReadDTOs
{
    public class InterventionDto
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public DateTime? DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public StatutIntervention Statut { get; set; }
        public TypeIntervention Type { get; set; }
        public int? RapportId { get; set; }
        public ICollection<UtilisateurDto> Techniciens { get; set; }
        public ICollection<InterventionPieceDetacheeDto> PieceDetachees { get; set; }
        public UtilisateurDto CreateurDto { get; set; }
        public EquipementDto EquipementDto { get; set; }

        public bool IsArchived { get; set; }

    }
}
