using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Web.Data;
using Veterinaria.Web.Models;
using Veterinaria.Web.Services;

namespace Veterinaria.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class VeterinariosController : Controller
{
    private readonly AppDbContext _db;
    private readonly ArchivoService _archivos;

    public VeterinariosController(AppDbContext db, ArchivoService archivos)
    {
        _db = db;
        _archivos = archivos;
    }

    // Listar y buscar los veterinarios (la búsqueda es por nombre o número de colegiado)
    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.Veterinarios.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(v => v.NombreCompleto.Contains(q) || v.NumeroColegiado.Contains(q));

        ViewBag.Q = q;
        return View(await query.OrderBy(v => v.NombreCompleto).ToListAsync());
    }

    // Agregar un nuevo veterinario (la foto es obligatoria, y el número de colegiado debe ser único)
    [HttpGet]
    public IActionResult Crear() => View(new Veterinario());

    // Al guardar, se valida que el nombre completo y el número de colegiado no estén vacíos, 
    // que el número de colegiado sea único, y que la foto esté presente.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        [Bind("NombreCompleto,NumeroColegiado")] Veterinario vet, IFormFile? foto)
    {
        ModelState.Clear(); // las validaciones las hacemos nosotros, con mensajes en español
        Limpiar(vet);
        await Validar(vet, null);

        if (foto == null || foto.Length == 0)
            ModelState.AddModelError("foto", "La fotografía es obligatoria.");

        if (!ModelState.IsValid) return View(vet);

        // Solo se guarda el archivo si el resto de los datos ya es válido
        var resultado = await _archivos.GuardarFotoAsync(foto, "veterinarios");
        if (!resultado.Ok)
        {
            ModelState.AddModelError("foto", resultado.Error!);
            return View(vet);
        }

        vet.FotoRuta = resultado.Ruta;
        _db.Veterinarios.Add(vet);
        await _db.SaveChangesAsync();

        TempData["Ok"] = "Veterinario agregado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // Modificar un veterinario existente (la foto es opcional, y el número de colegiado debe ser único)
    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var vet = await _db.Veterinarios.FindAsync(id);
        if (vet == null) return NotFound();
        return View(vet);
    }

    // Método para guardar los cambios de un veterinario existente. La foto es opcional, y si se sube una nueva, la anterior se borra.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        int id, [Bind("NombreCompleto,NumeroColegiado")] Veterinario datos, IFormFile? foto)
    {
        ModelState.Clear();
        var vet = await _db.Veterinarios.FindAsync(id);
        if (vet == null) return NotFound();

        Limpiar(datos);
        await Validar(datos, id);

        // Si hay un error, la vista vuelve a mostrar la foto que ya tenía
        datos.Id = id;
        datos.FotoRuta = vet.FotoRuta;

        if (!ModelState.IsValid) return View(datos);

        string? fotoAnterior = null;
        if (foto != null && foto.Length > 0) // en edición la foto es opcional
        {
            var resultado = await _archivos.GuardarFotoAsync(foto, "veterinarios");
            if (!resultado.Ok)
            {
                ModelState.AddModelError("foto", resultado.Error!);
                return View(datos);
            }
            fotoAnterior = vet.FotoRuta;
            vet.FotoRuta = resultado.Ruta;
        }

        vet.NombreCompleto = datos.NombreCompleto;
        vet.NumeroColegiado = datos.NumeroColegiado;
        await _db.SaveChangesAsync();

        _archivos.EliminarFoto(fotoAnterior); // se borra la foto vieja solo si la BD ya se actualizó

        TempData["Ok"] = "Veterinario modificado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // Eliminar un veterinario, solo si no tiene un usuario vinculado. Se borra también la foto del veterinario.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var vet = await _db.Veterinarios.FindAsync(id);
        if (vet == null) return NotFound();

        // Quien actúa en el expediente es un Usuario vinculado al veterinario,
        // así que si hay un usuario vinculado, el veterinario está en uso.
        if (await _db.Usuarios.AnyAsync(u => u.VeterinarioId == id))
        {
            TempData["Error"] = "No se puede eliminar: el veterinario está vinculado a un usuario.";
            return RedirectToAction(nameof(Index));
        }

        _db.Veterinarios.Remove(vet);
        await _db.SaveChangesAsync();
        _archivos.EliminarFoto(vet.FotoRuta);

        TempData["Ok"] = "Veterinario eliminado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private static void Limpiar(Veterinario v)
    {
        v.NombreCompleto = (v.NombreCompleto ?? "").Trim();
        v.NumeroColegiado = (v.NumeroColegiado ?? "").Trim();
    }

    // Valida que el nombre completo y el número de colegiado no estén vacíos, y que el número de colegiado sea único 
    private async Task Validar(Veterinario v, int? idActual)
    {
        if (string.IsNullOrWhiteSpace(v.NombreCompleto))
            ModelState.AddModelError("NombreCompleto", "El nombre completo es obligatorio.");

        if (string.IsNullOrWhiteSpace(v.NumeroColegiado))
            ModelState.AddModelError("NumeroColegiado", "El número de colegiado es obligatorio.");
        else if (await _db.Veterinarios.AnyAsync(x => x.NumeroColegiado == v.NumeroColegiado
                                                   && x.Id != idActual))
            ModelState.AddModelError("NumeroColegiado", "Ya existe un veterinario con ese número de colegiado.");
    }
}