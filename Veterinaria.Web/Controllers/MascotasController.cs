// QUÉ HACE: listado y búsqueda de mascotas del módulo de veterinaria.
// RUTAS:    GET /Mascotas/Index
//           GET /Mascotas/Index?q=texto
// REGLAS:   - Solo Veterinario y Administrador.
//           - La búsqueda es por nombre de la mascota o por cédula de su dueño.
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

    // Muestra el listado de mascotas. Si llega "q", filtra por nombre de la mascota o cédula del dueño.
    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.Mascotas
            .Include(m => m.Especie)
            .Include(m => m.Raza)
            .Include(m => m.Dueno)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(m => m.Nombre.Contains(q) || m.Dueno!.Cedula.Contains(q));
        }

        ViewBag.Q = q;

        var mascotas = await query
            .OrderByDescending(m => m.UltimaAtencion.HasValue) // las que tienen atención van primero
            .ThenByDescending(m => m.UltimaAtencion)           // de la más reciente a la más antigua
            .ThenByDescending(m => m.FechaCreacion)            // las que no tienen atención, por fecha de creación
            .ToListAsync();

        return View(mascotas);
    }
}