using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.DTOs.AuthDTO
{
    public class ResetPasswordDto
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Token { get; set; }

        [Required, MinLength(6)]
        public string Password { get; set; }
    }
}
