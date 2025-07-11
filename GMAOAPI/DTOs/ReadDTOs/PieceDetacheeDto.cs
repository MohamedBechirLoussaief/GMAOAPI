using GMAOAPI.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.DTOs.ReadDTOs
{
    public class PieceDetacheeDto
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public string Reference { get; set; }
        public int QuantiteStock { get; set; }
        public FournisseurDto FournisseurDto { get; set; }
        public double Cout { get; set; }
        public bool IsArchived { get; set; }

    }
}
