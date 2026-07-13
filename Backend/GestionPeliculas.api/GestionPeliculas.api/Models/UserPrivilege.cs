namespace GestionPeliculas.api.Models
{
    public class UserPrivilege
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public User User { get; set; } = null!;

        public int PrivilegeId { get; set; }

        public Privilege Privilege { get; set; } = null!;
    }
}
