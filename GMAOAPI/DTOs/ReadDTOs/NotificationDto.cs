using GMAOAPI.Models.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.DTOs.ReadDTOs
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public DateTime Date { get; set; }

        public string DistinataireUserName { get; set; }

        public StatutNotification Statut { get; set; }


    }
}
