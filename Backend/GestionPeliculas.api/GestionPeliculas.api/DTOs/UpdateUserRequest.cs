namespace GestionPeliculas.api.DTOs
{
    public class UpdateUserRequest
    {
        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
