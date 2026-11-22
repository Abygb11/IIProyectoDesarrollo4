using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Web.Data;
using Veterinaria.Web.Dtos;

namespace Veterinaria.Web.Controllers.Api;

[ApiController]
[Route("api/mascotas")]
[Authorize(AuthenticationSchemes = "Bearer", Roles = "Dueno")]
public class MascotasApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    public MascotasApiController(AppDbContext db, IWebHostEnvironment env) { _db = db; _env = env; }

    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Solo devuelve la mascota si pertenece al dueño del token
    private Task<bool> EsMia(int mascotaId) =>
        _db.Mascotas.AnyAsync(m => m.Id == mascotaId && m.DuenoId == UsuarioId);

    [HttpGet]
    public async Task<ActionResult<List<MascotaDto>>> Listar()
    {
        var lista = await _db.Mascotas
            .Where(m => m.DuenoId == UsuarioId)
            .OrderBy(m => m.Nombre)
            .Select(m => new MascotaDto(m.Id, m.Nombre, m.Especie!.Nombre, m.Raza!.Nombre, m.FotoRuta != null))
            .ToListAsync();
        return Ok(lista);
    }

    [HttpGet("{id}/vacunas")]
    public async Task<IActionResult> Vacunas(int id)
    {
        if (!await EsMia(id)) return NotFound();

        var hoy = DateTime.Today;
        var vacunas = await _db.Vacunas
            .Where(v => v.MascotaId == id)
            .OrderByDescending(v => v.FechaAplicacion)
            .Select(v => new VacunaDto(v.Id, v.Nombre, v.FechaAplicacion, v.FechaProximaDosis,
                v.FechaProximaDosis < hoy, v.Autor!.Nombre))
            .ToListAsync();
        return Ok(vacunas);
    }

    [HttpGet("{id}/examenes")]
    public async Task<IActionResult> Examenes(int id)
    {
        if (!await EsMia(id)) return NotFound();

        var examenes = await _db.Examenes
            .Where(e => e.MascotaId == id)
            .OrderByDescending(e => e.FechaRegistro)
            .Select(e => new ExamenDto(e.Id, e.Descripcion, e.FechaRegistro, e.NombreOriginal, e.TipoContenido, e.Autor!.Nombre))
            .ToListAsync();
        return Ok(examenes);
    }

    [HttpGet("{id}/examenes/{examenId}/archivo")]
    public async Task<IActionResult> Archivo(int id, int examenId)
    {
        if (!await EsMia(id)) return NotFound();

        var ex = await _db.Examenes.FirstOrDefaultAsync(e => e.Id == examenId && e.MascotaId == id);
        if (ex == null) return NotFound();

        var ruta = Path.Combine(_env.ContentRootPath, "App_Data", "examenes", ex.ArchivoRuta);
        if (!System.IO.File.Exists(ruta)) return NotFound();

        return PhysicalFile(ruta, ex.TipoContenido, ex.NombreOriginal);
    }

    [HttpGet("{id}/foto")]
    public async Task<IActionResult> Foto(int id)
    {
        var m = await _db.Mascotas.FirstOrDefaultAsync(x => x.Id == id && x.DuenoId == UsuarioId);
        if (m == null || m.FotoRuta == null) return NotFound();

        var ruta = Path.Combine(_env.WebRootPath, m.FotoRuta.TrimStart('/'));
        if (!System.IO.File.Exists(ruta)) return NotFound();

        var tipo = Path.GetExtension(ruta).ToLower() == ".png" ? "image/png" : "image/jpeg";
        return PhysicalFile(ruta, tipo);
    }
}