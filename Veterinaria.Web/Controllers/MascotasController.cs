// QUÉ HACE: listado de mascotas del módulo de veterinaria.
// RUTAS:    GET /Mascotas/Index
// REGLAS:   - Solo Veterinario y Administrador.
//           - Orden por última atención, de la más reciente a la menos reciente.
//           - Las mascotas sin atenciones quedan al final, ordenadas por fecha de creación.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Web.Data;

namespace Veterinaria.Web.Controllers;

[Authorize(Roles = "Administrador,Veterinario")]
public class MascotasController : Controller
{
    private readonly AppDbContext _db;
    public MascotasController(AppDbContext db) => _db = db;

    // Muestra el listado de mascotas ordenado por la fecha de su última atención.
    public async Task<IActionResult> Index()
    {
        var mascotas = await _db.Mascotas
            .Include(m => m.Especie)
            .Include(m => m.Raza)
            .Include(m => m.Dueno)
            .OrderByDescending(m => m.UltimaAtencion.HasValue) // las que tienen atención van primero
            .ThenByDescending(m => m.UltimaAtencion)           // de la más reciente a la más antigua
            .ThenByDescending(m => m.FechaCreacion)            // las que no tienen atención, por fecha de creación
            .ToListAsync();

        return View(mascotas);
    }
}