namespace GestionPeliculas.api.Models
{
    public class User
    {
        public int Id { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Salt { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public List<UserPrivilege> UserPrivileges { get; set; } = new();

        public List<RefreshToken> RefreshTokens { get; set; } = new();
    }
}
