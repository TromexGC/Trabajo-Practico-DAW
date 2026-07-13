namespace GestionPeliculas.api.DTOs
{
    public class CreateUserRequest
    {
        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public List<string> Privileges { get; set; } = new();
    }
}
