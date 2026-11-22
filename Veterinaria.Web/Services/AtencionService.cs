using Veterinaria.Web.Data;

namespace Veterinaria.Web.Services;

public class AtencionService
{
    private readonly AppDbContext _db;
    public AtencionService(AppDbContext db) => _db = db;

    // Actualiza el campo UltimaAtencion de la mascota cada vez que se agrega un registro al expediente.
    public async Task RegistrarAsync(int mascotaId)
    {
        var mascota = await _db.Mascotas.FindAsync(mascotaId);
        if (mascota == null) return;

        mascota.UltimaAtencion = DateTime.Now;
        await _db.SaveChangesAsync();
    }
}