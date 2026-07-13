using GestionPeliculas.api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionPeliculas.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AuditController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuditController(AppDbContext context)
        {
            _context = context;
        }

        private bool HasPrivilege(string privilege)
        {
            return User.Claims.Any(c => c.Type == "privilege" && c.Value == privilege);
        }

        [HttpGet]
        public async Task<IActionResult> GetAuditLogs()
        {
            if (!HasPrivilege("USERS_MANAGE"))
            {
                return Forbid();
            }

            var logs = await _context.AuditLogs
                .OrderByDescending(x => x.FechaHora)
                .ToListAsync();

            return Ok(logs);
        }
    }
}