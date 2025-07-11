using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using GMAOAPI.Models.Enumerations;

namespace GMAOAPI.DTOs.CreateDTOs
{
    public class NotificationCreateDto
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Message { get; set; }
        [Required]
        public DateTime Date { get; set; }
        [Required]
        public string DestinataireId { get; set; }

    }
}
