namespace GestionPeliculas.api.DTOs
{
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public List<string> Privileges { get; set; } = new();
    }
}
