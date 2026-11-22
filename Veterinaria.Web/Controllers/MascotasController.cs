// QUÉ HACE: módulo de veterinaria para mascotas: listado con búsqueda, expediente,
//           registro y edición, vinculando cada mascota con su dueño.
// RUTAS:    GET  /Mascotas/Index?q=texto
//           GET  /Mascotas/Expediente/1
//           GET  /Mascotas/Crear          POST /Mascotas/Crear
//           GET  /Mascotas/Editar/1       POST /Mascotas/Editar/1
//           GET  /Mascotas/RazasPorEspecie?especieId=1   (JSON)
//           GET  /Mascotas/BuscarDueno?cedula=300000001   (JSON)
// REGLAS:   - Solo Veterinario y Administrador.
//           - La búsqueda es por nombre de la mascota o por cédula de su dueño.
//           - Orden por última atención, de la más reciente a la menos reciente.
//           - La raza debe pertenecer a la especie elegida.
//           - Si la cédula del dueño no existe, se crea un dueño nuevo (perfil fijo: Dueño).
//           - Una mascota nueva no tiene última atención hasta que se le agregue un registro.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Web.Data;
using Veterinaria.Web.Models;
using Veterinaria.Web.Services;
using Veterinaria.Web.ViewModels;

namespace Veterinaria.Web.Controllers;

[Authorize(Roles = "Administrador,Veterinario")]
public class MascotasController : Controller
{
    private readonly AppDbContext _db;
    private readonly ArchivoService _archivos;

    public MascotasController(AppDbContext db, ArchivoService archivos)
    {
        _db = db;
        _archivos = archivos;
    }

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

    // Muestra el expediente de la mascota: datos generales y pestañas con vacunas,
    // medicamentos y tratamientos, exámenes e historial clínico.
    public async Task<IActionResult> Expediente(int id)
    {
        var mascota = await _db.Mascotas
            .Include(m => m.Especie)
            .Include(m => m.Raza)
            .Include(m => m.Dueno)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (mascota == null) return NotFound();

        var items = await _db.MascotaItems
            .Include(i => i.ItemClinico)
            .Include(i => i.AutorInicio)
            .Include(i => i.AutorSuspension)
            .Where(i => i.MascotaId == id)
            .OrderByDescending(i => i.FechaInicio)
            .ToListAsync();

        var vm = new MascotaExpedienteVM
        {
            Mascota = mascota,
            Vacunas = await _db.Vacunas
                .Include(v => v.Autor)
                .Where(v => v.MascotaId == id)
                .OrderByDescending(v => v.FechaAplicacion)
                .ToListAsync(),
            Vigentes = items.Where(i => i.FechaSuspension == null).ToList(),
            Suspendidos = items.Where(i => i.FechaSuspension != null).ToList(),
            Examenes = await _db.Examenes
                .Include(e => e.Autor)
                .Where(e => e.MascotaId == id)
                .OrderByDescending(e => e.FechaRegistro)
                .ToListAsync(),
            Notas = await _db.Notas
                .Include(n => n.Autor)
                .Where(n => n.MascotaId == id)
                .OrderByDescending(n => n.FechaHora)
                .Take(5)
                .ToListAsync()
        };

        return View(vm);
    }

    // ---------- Registro ----------

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        await CargarListas(0);
        return View(new MascotaFormVM());
    }

    // Valida, guarda la foto (si hay) y registra la mascota. Si el dueño no existe, lo crea.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(MascotaFormVM vm)
    {
        await ValidarAsync(vm);
        var fotoRuta = ModelState.IsValid ? await GuardarFotoAsync(vm) : null;

        if (!ModelState.IsValid)
        {
            await CargarListas(vm.EspecieId);
            return View(vm);
        }

        var mascota = new Mascota
        {
            Nombre = vm.Nombre.Trim(),
            EspecieId = vm.EspecieId,
            RazaId = vm.RazaId,
            FechaNacimiento = vm.FechaNacimiento!.Value,
            Sexo = vm.Sexo,
            FotoRuta = fotoRuta,
            Dueno = await ResolverDuenoAsync(vm),
            FechaCreacion = DateTime.Now
            // UltimaAtencion queda en null hasta que se agregue un registro al expediente
        };
        _db.Mascotas.Add(mascota);
        await _db.SaveChangesAsync();

        TempData["Ok"] = "Mascota registrada.";
        return RedirectToAction(nameof(Expediente), new { id = mascota.Id });
    }

    // ---------- Edición ----------

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var m = await _db.Mascotas.Include(x => x.Dueno).FirstOrDefaultAsync(x => x.Id == id);
        if (m == null) return NotFound();

        var vm = new MascotaFormVM
        {
            Id = m.Id,
            Nombre = m.Nombre,
            EspecieId = m.EspecieId,
            RazaId = m.RazaId,
            FechaNacimiento = m.FechaNacimiento,
            Sexo = m.Sexo,
            FotoRuta = m.FotoRuta,
            CedulaDueno = m.Dueno!.Cedula
        };
        await CargarListas(vm.EspecieId);
        return View(vm);
    }

    // Valida y actualiza la mascota. Si se sube una foto nueva, reemplaza la anterior.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, MascotaFormVM vm)
    {
        var mascota = await _db.Mascotas.FirstOrDefaultAsync(m => m.Id == id);
        if (mascota == null) return NotFound();

        vm.Id = id;
        await ValidarAsync(vm);
        var fotoNueva = ModelState.IsValid ? await GuardarFotoAsync(vm) : null;

        if (!ModelState.IsValid)
        {
            vm.FotoRuta = mascota.FotoRuta;   // para seguir mostrando la foto actual
            await CargarListas(vm.EspecieId);
            return View(vm);
        }

        mascota.Nombre = vm.Nombre.Trim();
        mascota.EspecieId = vm.EspecieId;
        mascota.RazaId = vm.RazaId;
        mascota.FechaNacimiento = vm.FechaNacimiento!.Value;
        mascota.Sexo = vm.Sexo;
        mascota.Dueno = await ResolverDuenoAsync(vm);

        string? fotoAnterior = null;
        if (fotoNueva != null)
        {
            fotoAnterior = mascota.FotoRuta;
            mascota.FotoRuta = fotoNueva;
        }

        await _db.SaveChangesAsync();
        _archivos.EliminarFoto(fotoAnterior);   // borra la foto vieja solo si se guardó la nueva

        TempData["Ok"] = "Mascota actualizada.";
        return RedirectToAction(nameof(Expediente), new { id });
    }

    // ---------- Endpoints auxiliares (los usa el JavaScript del formulario) ----------

    // Devuelve las razas de una especie para recargar el select de razas.
    [HttpGet]
    public async Task<IActionResult> RazasPorEspecie(int especieId)
    {
        var razas = await _db.Razas
            .Where(r => r.EspecieId == especieId)
            .OrderBy(r => r.Nombre)
            .Select(r => new { id = r.Id, nombre = r.Nombre })
            .ToListAsync();
        return Json(razas);
    }

    // Indica si existe un usuario con esa cédula y si es dueño.
    [HttpGet]
    public async Task<IActionResult> BuscarDueno(string? cedula)
    {
        cedula = (cedula ?? "").Trim();
        var u = await _db.Usuarios.FirstOrDefaultAsync(x => x.Cedula == cedula);
        if (u == null) return Json(new { existe = false });
        return Json(new { existe = true, esDueno = u.Perfil == Perfil.Dueno, nombre = u.Nombre });
    }

    // ---------- Métodos privados ----------

    // Carga las listas de especies y razas (las razas son solo las de la especie elegida).
    private async Task CargarListas(int especieId)
    {
        ViewBag.Especies = new SelectList(
            await _db.Especies.OrderBy(e => e.Nombre).ToListAsync(), "Id", "Nombre", especieId);

        var razas = especieId > 0
            ? await _db.Razas.Where(r => r.EspecieId == especieId).OrderBy(r => r.Nombre).ToListAsync()
            : new List<Raza>();
        ViewBag.Razas = new SelectList(razas, "Id", "Nombre");
    }

    // Reglas del formulario que no cubren los atributos del ViewModel.
    private async Task ValidarAsync(MascotaFormVM vm)
    {
        if (vm.FechaNacimiento.HasValue && vm.FechaNacimiento.Value.Date > DateTime.Today)
            ModelState.AddModelError(nameof(vm.FechaNacimiento), "La fecha de nacimiento no puede ser futura.");

        if (vm.EspecieId > 0 && vm.RazaId > 0 &&
            !await _db.Razas.AnyAsync(r => r.Id == vm.RazaId && r.EspecieId == vm.EspecieId))
            ModelState.AddModelError(nameof(vm.RazaId), "La raza no pertenece a la especie elegida.");

        var cedula = (vm.CedulaDueno ?? "").Trim();
        if (cedula == "") return;   // el atributo [Required] ya marca este error

        var dueno = await _db.Usuarios.FirstOrDefaultAsync(u => u.Cedula == cedula);
        if (dueno != null)
        {
            if (dueno.Perfil != Perfil.Dueno)
                ModelState.AddModelError(nameof(vm.CedulaDueno), "Esa cédula pertenece a un usuario que no es dueño.");
            return;
        }

        // La cédula no existe: se necesitan los datos para crear al dueño nuevo
        if (string.IsNullOrWhiteSpace(vm.NombreDueno))
            ModelState.AddModelError(nameof(vm.NombreDueno), "Escribe el nombre del dueño nuevo.");

        if (string.IsNullOrWhiteSpace(vm.CorreoDueno))
            ModelState.AddModelError(nameof(vm.CorreoDueno), "Escribe el correo del dueño nuevo.");
        else if (await _db.Usuarios.AnyAsync(u => u.Correo == vm.CorreoDueno.Trim()))
            ModelState.AddModelError(nameof(vm.CorreoDueno), "Ya existe un usuario con ese correo.");

        if (string.IsNullOrWhiteSpace(vm.PasswordDueno) || vm.PasswordDueno.Length < 6)
            ModelState.AddModelError(nameof(vm.PasswordDueno), "La contraseña debe tener al menos 6 caracteres.");
    }

    // Guarda la foto si el usuario subió una. Devuelve la ruta, o null si no hay foto o hubo error.
    private async Task<string?> GuardarFotoAsync(MascotaFormVM vm)
    {
        if (vm.Foto == null || vm.Foto.Length == 0) return null;

        var resultado = await _archivos.GuardarFotoAsync(vm.Foto, "mascotas");
        if (!resultado.Ok)
        {
            ModelState.AddModelError(nameof(vm.Foto), resultado.Error ?? "No se pudo guardar la foto.");
            return null;
        }
        return resultado.Ruta;
    }

    // Devuelve el dueño existente con esa cédula, o crea uno nuevo con perfil Dueño.
    // El perfil se fija aquí en el servidor, nunca viene del formulario.
    private async Task<Usuario> ResolverDuenoAsync(MascotaFormVM vm)
    {
        var cedula = vm.CedulaDueno.Trim();
        var dueno = await _db.Usuarios.FirstOrDefaultAsync(u => u.Cedula == cedula);
        if (dueno != null) return dueno;

        return new Usuario
        {
            Cedula = cedula,
            Nombre = vm.NombreDueno!.Trim(),
            Correo = vm.CorreoDueno!.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.PasswordDueno!),
            Perfil = Perfil.Dueno,
            Bloqueado = false
        };
    }
}