using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Web.Data;
using Veterinaria.Web.Models;

namespace Veterinaria.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class CatalogoClinicoController : Controller
{
    private readonly AppDbContext _db;
    public CatalogoClinicoController(AppDbContext db) => _db = db;

    // Listar y buscar por nombre y/o por tipo
    public async Task<IActionResult> Index(string? q, TipoItem? tipo)
    {
        var query = _db.ItemsClinicos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(i => i.Nombre.Contains(q));
        if (tipo.HasValue)
            query = query.Where(i => i.Tipo == tipo.Value);

        ViewBag.Q = q;
        ViewBag.Tipo = tipo;

        return View(await query
            .OrderBy(i => i.Tipo)
            .ThenBy(i => i.Nombre)
            .ToListAsync());
    }

    // Agregar nuevo ítem clínico
    [HttpGet]
    public IActionResult Crear() => View(new ItemClinico());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear([Bind("Nombre,Tipo,Descripcion")] ItemClinico item)
    {
        Limpiar(item);
        await Validar(item, null);

        if (!ModelState.IsValid) return View(item);

        _db.ItemsClinicos.Add(item);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Ítem agregado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // Método para mostrar el formulario de edición y para la edición de un ítem clínico existente
    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var item = await _db.ItemsClinicos.FindAsync(id);
        if (item == null) return NotFound();
        return View(item);
    }

    // Guardar los cambios realizados en un ítem clínico existente
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, [Bind("Nombre,Tipo,Descripcion")] ItemClinico datos)
    {
        var item = await _db.ItemsClinicos.FindAsync(id);
        if (item == null) return NotFound();

        Limpiar(datos);
        await Validar(datos, id);

        if (!ModelState.IsValid)
        {
            datos.Id = id;
            return View(datos);
        }

        item.Nombre = datos.Nombre;
        item.Tipo = datos.Tipo;
        item.Descripcion = datos.Descripcion;
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Ítem modificado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // Eliminar ítem clínico (regla: lo que está en uso no se elimina)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var item = await _db.ItemsClinicos.FindAsync(id);
        if (item == null) return NotFound();

        if (await _db.MascotaItems.AnyAsync(mi => mi.ItemClinicoId == id))
        {
            TempData["Error"] = "No se puede eliminar: ya fue asignado a una o más mascotas.";
            return RedirectToAction(nameof(Index));
        }

        _db.ItemsClinicos.Remove(item);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Ítem eliminado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // Quita espacios sobrantes y evita nulls en Descripcion
    private static void Limpiar(ItemClinico item)
    {
        item.Nombre = (item.Nombre ?? "").Trim();
        item.Descripcion = (item.Descripcion ?? "").Trim();
    }

    // Validaciones compartidas por Crear y Editar
    private async Task Validar(ItemClinico item, int? idActual)
    {
        if (string.IsNullOrWhiteSpace(item.Nombre))
            ModelState.AddModelError("Nombre", "El nombre es obligatorio.");

        if (!Enum.IsDefined(typeof(TipoItem), item.Tipo))
            ModelState.AddModelError("Tipo", "Seleccione un tipo válido.");

        if (string.IsNullOrWhiteSpace(item.Descripcion))
            ModelState.AddModelError("Descripcion", "La descripción es obligatoria.");

        if (!string.IsNullOrWhiteSpace(item.Nombre) &&
            await _db.ItemsClinicos.AnyAsync(i => i.Nombre == item.Nombre
                                               && i.Tipo == item.Tipo
                                               && i.Id != idActual))
            ModelState.AddModelError("Nombre", "Ya existe un ítem con ese nombre y tipo.");
    }
}