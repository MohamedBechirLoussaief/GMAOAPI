using GMAOAPI.Models.Enumerations;

namespace GMAOAPI.Models.Entities
{
    public class Audit
    {
        public int Id { get; set; }
        public string ActionEffectuee { get; set; }
        public ActionType type { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string EntityName { get; set; }
        public string EntityId { get; set; }
        public string UtilisateurId { get; set; }
        public Utilisateur Utilisateur { get; set; }
        public string UserName { get; set; }
        public string? IpAddress { get; set; }
        public string? BrowserInfo { get; set; }


    }
}
