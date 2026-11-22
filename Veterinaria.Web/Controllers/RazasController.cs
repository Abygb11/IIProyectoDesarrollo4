using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Web.Data;
using Veterinaria.Web.Models;

namespace Veterinaria.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class RazasController : Controller
{
    private readonly AppDbContext _db;
    public RazasController(AppDbContext db) => _db = db;

    // Carga la lista de especies para el <select> de los formularios y del filtro
    private async Task CargarEspecies(int? seleccionada = null)
    {
        var especies = await _db.Especies.OrderBy(e => e.Nombre).ToListAsync();
        ViewBag.Especies = new SelectList(especies, "Id", "Nombre", seleccionada);
    }

    // Método para listar y buscar por nombre de raza y/o por especie
    public async Task<IActionResult> Index(string? q, int? especieId)
    {
        var query = _db.Razas.Include(r => r.Especie).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(r => r.Nombre.Contains(q));
        if (especieId.HasValue)
            query = query.Where(r => r.EspecieId == especieId.Value);

        ViewBag.Q = q;
        await CargarEspecies(especieId);

        return View(await query
            .OrderBy(r => r.Especie!.Nombre)
            .ThenBy(r => r.Nombre)
            .ToListAsync());
    }

    // Agregar nueva raza
    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        await CargarEspecies();
        return View(new Raza());
    }

    // Crear nueva raza
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear([Bind("Nombre,EspecieId")] Raza raza)
    {
        raza.Nombre = (raza.Nombre ?? "").Trim();
        await Validar(raza, null);

        if (!ModelState.IsValid)
        {
            await CargarEspecies(raza.EspecieId);
            return View(raza);
        }

        _db.Razas.Add(raza);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Raza agregada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // Método para editar una raza específica. Se reutiliza la vista de Crear, 
    // pero con los datos de la raza a modificar.
    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var raza = await _db.Razas.FindAsync(id);
        if (raza == null) return NotFound();

        await CargarEspecies(raza.EspecieId);
        return View(raza);
    }


    // Editar raza con los datos enviados desde el formulario. Se valida que no 
    // exista otra raza con el mismo nombre y especie.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, [Bind("Nombre,EspecieId")] Raza datos)
    {
        var raza = await _db.Razas.FindAsync(id);
        if (raza == null) return NotFound();

        datos.Nombre = (datos.Nombre ?? "").Trim();
        await Validar(datos, id);

        if (!ModelState.IsValid)
        {
            datos.Id = id;
            await CargarEspecies(datos.EspecieId);
            return View(datos);
        }

        raza.Nombre = datos.Nombre;
        raza.EspecieId = datos.EspecieId;
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Raza modificada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // Método para eliminar una raza específica (Mantiene la regla de que lo que está en uso no se elimina)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var raza = await _db.Razas.FindAsync(id);
        if (raza == null) return NotFound();

        if (await _db.Mascotas.AnyAsync(m => m.RazaId == id))
        {
            TempData["Error"] = "No se puede eliminar: la raza tiene mascotas asociadas.";
            return RedirectToAction(nameof(Index));
        }

        _db.Razas.Remove(raza);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Raza eliminada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // Validaciones compartidas por Crear y Editar
    private async Task Validar(Raza raza, int? idActual)
    {
        if (string.IsNullOrWhiteSpace(raza.Nombre))
            ModelState.AddModelError("Nombre", "El nombre es obligatorio.");

        if (!await _db.Especies.AnyAsync(e => e.Id == raza.EspecieId))
        {
            ModelState.AddModelError("EspecieId", "Seleccione una especie válida.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(raza.Nombre) &&
            await _db.Razas.AnyAsync(r => r.Nombre == raza.Nombre
                                       && r.EspecieId == raza.EspecieId
                                       && r.Id != idActual))
            ModelState.AddModelError("Nombre", "Ya existe esa raza en la especie seleccionada.");
    }
}