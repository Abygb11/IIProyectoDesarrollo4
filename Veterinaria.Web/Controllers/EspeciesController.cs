using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Web.Data;
using Veterinaria.Web.Models;

namespace Veterinaria.Web.Controllers;

// Se establece que solamente los usuarios que tengan el rol de Administrador 
// pueden acceder a este controlador.
[Authorize(Roles = "Administrador")]
public class EspeciesController : Controller
{
    private readonly AppDbContext _db; // Variable que permite acceder a la BD
    public EspeciesController(AppDbContext db) => _db = db; // recibe el contexto de la base de datos


    // Listar y buscar especies: Este método muestra todas las especies 
    // registradas y las busca por su nombre.
    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.Especies.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(e => e.Nombre.Contains(q));

        ViewBag.Q = q;
        return View(await query.OrderBy(e => e.Nombre).ToListAsync());
    }

    // Método para agregar una nueva especie
    [HttpGet]
    public IActionResult Crear() => View(new Especie());

    // Con este método se recibe la información del formulario y realiza las 
    // validaciones necesarias antes de guardar la especie.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear([Bind("Nombre")] Especie especie)
    {
        especie.Nombre = (especie.Nombre ?? "").Trim();

        if (string.IsNullOrWhiteSpace(especie.Nombre))
            ModelState.AddModelError("Nombre", "El nombre es obligatorio.");
        else if (await _db.Especies.AnyAsync(e => e.Nombre == especie.Nombre))
            ModelState.AddModelError("Nombre", "Ya existe una especie con ese nombre.");

        if (!ModelState.IsValid) return View(especie);

        _db.Especies.Add(especie);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Especie agregada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // Método para modificar una especie existente y mostrar sus datos en un formulario
    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var especie = await _db.Especies.FindAsync(id);
        if (especie == null) return NotFound();
        return View(especie);
    }

    // Este método recibe los nuevos datos y actualiza la especie correspondiente
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, [Bind("Nombre")] Especie datos)
    {
        var especie = await _db.Especies.FindAsync(id);
        if (especie == null) return NotFound();

        datos.Nombre = (datos.Nombre ?? "").Trim();

        if (string.IsNullOrWhiteSpace(datos.Nombre))
            ModelState.AddModelError("Nombre", "El nombre es obligatorio.");
        else if (await _db.Especies.AnyAsync(e => e.Nombre == datos.Nombre && e.Id != id))
            ModelState.AddModelError("Nombre", "Ya existe una especie con ese nombre.");

        if (!ModelState.IsValid)
        {
            datos.Id = id;
            return View(datos);
        }

        especie.Nombre = datos.Nombre;
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Especie modificada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // Este método permite eliminar una especie, pero primero comprueba que 
    // no tenga información relacionada en otras tablas. 
    // (Cumple con la regla de que lo que está en uso no se elimina)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var especie = await _db.Especies.FindAsync(id);
        if (especie == null) return NotFound();

        if (await _db.Razas.AnyAsync(r => r.EspecieId == id))
        {
            TempData["Error"] = "No se puede eliminar: la especie tiene razas asociadas.";
            return RedirectToAction(nameof(Index));
        }
        if (await _db.Mascotas.AnyAsync(m => m.EspecieId == id))
        {
            TempData["Error"] = "No se puede eliminar: la especie tiene mascotas asociadas.";
            return RedirectToAction(nameof(Index));
        }

        _db.Especies.Remove(especie);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Especie eliminada correctamente.";
        return RedirectToAction(nameof(Index));
    }
}