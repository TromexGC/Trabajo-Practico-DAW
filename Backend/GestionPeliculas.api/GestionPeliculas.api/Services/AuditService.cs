using GestionPeliculas.api.Data;
using GestionPeliculas.api.Models;

namespace GestionPeliculas.api.Services
{
    public class AuditService
    {
        private readonly AppDbContext _context;

        public AuditService(AppDbContext context)
        {
            _context = context;
        }

        public async Task RegistrarAccion(string usuario, string accion)
        {
            var log = new AuditLog
            {
                FechaHora = DateTime.Now,
                Usuario = usuario,
                Accion = accion
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}