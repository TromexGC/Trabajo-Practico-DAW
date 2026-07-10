namespace GestionPeliculas.api.Models
{
    public class Privilege
    {
        public int Id { get; set; }

        public string Description { get; set; } = string.Empty;

        public List<UserPrivilege> UserPrivileges { get; set; } = new();
    }
}
