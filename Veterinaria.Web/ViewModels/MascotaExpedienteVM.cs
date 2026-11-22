// QUÉ HACE: clase que lleva los datos del expediente de una mascota hacia la vista.
using Veterinaria.Web.Models;

namespace Veterinaria.Web.ViewModels;

public class MascotaExpedienteVM
{
    public Mascota Mascota { get; set; } = null!;
    public List<Vacuna> Vacunas { get; set; } = new();
    public List<MascotaItem> Vigentes { get; set; } = new();
    public List<MascotaItem> Suspendidos { get; set; } = new();
    public List<Examen> Examenes { get; set; } = new();
}