namespace GestionPeliculas.api.Models
{
    public class Movie
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public string? Genero { get; set; }

        public string? Director { get; set; }

        public int? Anio { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
