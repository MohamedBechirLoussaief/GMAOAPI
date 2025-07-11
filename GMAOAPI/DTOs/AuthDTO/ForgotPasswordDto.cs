using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.DTOs.AuthDTO
{
    public class ForgotPasswordDto
    {
            [Required, EmailAddress]
            public string Email { get; set; }
    }
}
