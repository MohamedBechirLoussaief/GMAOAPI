using Microsoft.AspNetCore.Identity;

namespace GMAOAPI.Services
{

    public class FrenchIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DuplicateUserName(string userName)
            => new IdentityError
            {
                Code = nameof(DuplicateUserName),
                Description = $"Utilisateur existe déjà."
            };

        public override IdentityError DuplicateEmail(string email)
            => new IdentityError
            {
                Code = nameof(DuplicateEmail),
                Description = $"L'adresse e-mail '{email}' est déjà utilisée."
            };

        public override IdentityError InvalidUserName(string userName) =>
          new IdentityError { Code = nameof(InvalidUserName), Description = $"Le nom d'utilisateur '{userName}' est invalide." };

        public override IdentityError PasswordTooShort(int length) =>
            new IdentityError { Code = nameof(PasswordTooShort), Description = $"Le mot de passe doit contenir au moins {length} caractères." };

        public override IdentityError PasswordRequiresDigit() =>
            new IdentityError { Code = nameof(PasswordRequiresDigit), Description = "Le mot de passe doit contenir au moins un chiffre." };

        public override IdentityError PasswordRequiresUpper() =>
            new IdentityError { Code = nameof(PasswordRequiresUpper), Description = "Le mot de passe doit contenir au moins une lettre majuscule." };

        public override IdentityError PasswordRequiresLower() =>
            new IdentityError { Code = nameof(PasswordRequiresLower), Description = "Le mot de passe doit contenir au moins une lettre minuscule." };

        public override IdentityError PasswordRequiresNonAlphanumeric() =>
            new IdentityError { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Le mot de passe doit contenir au moins un caractère spécial." };
    }
}
