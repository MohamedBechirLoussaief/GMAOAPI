using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.DTOs.AuthDTO
{
    public class LoginDto
    {
        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
