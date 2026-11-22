// Clase para registrar y consultar los resultados de exámenes de una mascota 
// (módulo de veterinaria).

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Web.Data;
using Veterinaria.Web.Models;
using Veterinaria.Web.Services;
using Veterinaria.Web.ViewModels;

namespace Veterinaria.Web.Controllers;

[Authorize(Roles = "Administrador,Veterinario")]
public class ExamenesController : Controller
{
    private readonly AppDbContext _db;
    private readonly ArchivoService _archivos;
    private readonly AtencionService _atencion;

    public ExamenesController(AppDbContext db, ArchivoService archivos, AtencionService atencion)
    {
        _db = db;
        _archivos = archivos;
        _atencion = atencion;
    }

    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Arma el modelo de la pantalla (mascota, lista de exámenes y formulario vacío)
    private async Task<ExamenesVM?> Armar(int mascotaId)
    {
        var mascota = await _db.Mascotas.FindAsync(mascotaId);
        if (mascota == null) return null;

        return new ExamenesVM
        {
            Mascota = mascota,
            Examenes = await _db.Examenes
                .Include(e => e.Autor)
                .Where(e => e.MascotaId == mascotaId)
                .OrderByDescending(e => e.FechaRegistro)
                .ToListAsync(),
            Form = new ExamenForm { MascotaId = mascotaId }
        };
    }

    // Muestra la lista de exámenes de la mascota y el formulario para agregar uno nuevo.
    public async Task<IActionResult> Index(int mascotaId)
    {
        var vm = await Armar(mascotaId);
        if (vm == null) return NotFound();
        return View(vm);
    }

    // Valida el archivo y la descripción, guarda el examen y actualiza la última atención.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(ExamenForm form, IFormFile? archivo)
    {
        if (!await _db.Mascotas.AnyAsync(m => m.Id == form.MascotaId)) return NotFound();

        if (archivo == null || archivo.Length == 0)
            ModelState.AddModelError("archivo", "Seleccione el archivo del examen (imagen o PDF).");

        // Si algo falla, se vuelve a mostrar la pantalla conservando lo que escribió el usuario
        async Task<IActionResult> VolverConError()
        {
            var vmError = await Armar(form.MascotaId);
            vmError!.Form = form;
            return View("Index", vmError);
        }

        if (!ModelState.IsValid) return await VolverConError();

        // Solo se guarda el archivo si el resto de los datos ya es válido
        var resultado = await _archivos.GuardarExamenAsync(archivo);
        if (!resultado.Ok)
        {
            ModelState.AddModelError("archivo", resultado.Error!);
            return await VolverConError();
        }

        try
        {
            _db.Examenes.Add(new Examen
            {
                MascotaId = form.MascotaId,
                Descripcion = form.Descripcion.Trim(),
                ArchivoRuta = resultado.Ruta!, // solo el nombre del archivo 
                NombreOriginal = resultado.NombreOriginal!,
                TipoContenido = resultado.TipoContenido!,
                AutorId = UsuarioId, // autoría
                FechaRegistro = DateTime.Now
            });
            await _db.SaveChangesAsync();
        }
        catch
        {
            _archivos.EliminarExamen(resultado.Ruta!);        
            throw;
        }

        await _atencion.RegistrarAsync(form.MascotaId);

        TempData["Ok"] = "Examen registrado.";
        return RedirectToAction(nameof(Index), new { mascotaId = form.MascotaId });
    }

    // Entrega el archivo de un examen. Sin "descargar" se abre en el navegador; con "descargar=true" se baja.
    public async Task<IActionResult> Descargar(int id, bool descargar = false)
    {
        var examen = await _db.Examenes.FirstOrDefaultAsync(e => e.Id == id);
        if (examen == null) return NotFound();

        var ruta = _archivos.ObtenerRutaExamen(examen.ArchivoRuta);
        if (ruta == null) return NotFound();

        // Evita que el navegador "adivine" otro tipo de contenido distinto al guardado
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        return descargar
            ? PhysicalFile(ruta, examen.TipoContenido, examen.NombreOriginal)  // con nombre se descarga
            : PhysicalFile(ruta, examen.TipoContenido); // sin nombre se muestra
    }
}