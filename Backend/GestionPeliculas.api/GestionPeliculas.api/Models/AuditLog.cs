namespace GestionPeliculas.api.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public DateTime FechaHora { get; set; } = DateTime.Now;

        public string Accion { get; set; } = string.Empty;

        public string Usuario { get; set; } = string.Empty;
    }
}
