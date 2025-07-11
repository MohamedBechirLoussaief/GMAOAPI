using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.DTOs.ReadDTOs
{
    public class FournisseurDto
    {
        public int Id { get; set; }

        public string Nom { get; set; }

        public string Adresse { get; set; }

        public string Contact { get; set; }
        public bool IsArchived { get; set; }

    }
}
