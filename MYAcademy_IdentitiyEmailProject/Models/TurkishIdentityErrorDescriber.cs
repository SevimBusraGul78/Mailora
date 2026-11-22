using Microsoft.AspNetCore.Identity;

namespace MYAcademy_IdentitiyEmailProject.Models
{
    public class TurkishIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DuplicateUserName(string userName)
            => new() { Code = nameof(DuplicateUserName), Description = $"'{userName}' kullanıcı adı zaten alınmış." };

        public override IdentityError DuplicateEmail(string email)
            => new() { Code = nameof(DuplicateEmail), Description = $"'{email}' adresiyle zaten bir hesap var." };

        public override IdentityError InvalidUserName(string? userName)
            => new() { Code = nameof(InvalidUserName), Description = "Kullanıcı adı sadece harf ve rakam içerebilir." };

        public override IdentityError InvalidEmail(string? email)
            => new() { Code = nameof(InvalidEmail), Description = "Geçerli bir e-posta adresi girin." };

        public override IdentityError PasswordTooShort(int length)
            => new() { Code = nameof(PasswordTooShort), Description = $"Şifre en az {length} karakter olmalı." };

        public override IdentityError PasswordRequiresUpper()
            => new() { Code = nameof(PasswordRequiresUpper), Description = "Şifre en az bir büyük harf içermeli." };

        public override IdentityError PasswordRequiresLower()
            => new() { Code = nameof(PasswordRequiresLower), Description = "Şifre en az bir küçük harf içermeli." };

        public override IdentityError PasswordRequiresDigit()
            => new() { Code = nameof(PasswordRequiresDigit), Description = "Şifre en az bir rakam içermeli." };

        public override IdentityError PasswordRequiresNonAlphanumeric()
            => new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Şifre en az bir sembol içermeli (! . * gibi)." };

        public override IdentityError PasswordMismatch()
            => new() { Code = nameof(PasswordMismatch), Description = "Mevcut şifre hatalı." };

        public override IdentityError InvalidToken()
            => new() { Code = nameof(InvalidToken), Description = "Bağlantı geçersiz ya da süresi dolmuş." };
    }
}