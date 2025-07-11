using GMAOAPI.DTOs.AuthDTO;
using GMAOAPI.Models.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.DTOs.ReadDTOs
{
    public class RapportDto
    {
        public int Id { get; set; }

        public string Titre { get; set; }

        public string Contenu { get; set; }

        public DateTime DateCreation { get; set; }

        public InterventionDto InterventionDto { get; set; }

        public UtilisateurDto CreateurDto { get; set; }

        public UtilisateurDto ValideurDto { get; set; }

        public ResultatIntervention Resultat { get; set; }

        public bool IsValid { get; set; }
        public bool IsArchived { get; set; }


    }

}
