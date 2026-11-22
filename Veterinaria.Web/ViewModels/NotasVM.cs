// QUÉ HACE: clases que llevan datos del historial clínico entre el controlador y la vista.
using Veterinaria.Web.Models;

namespace Veterinaria.Web.ViewModels;

public class NotaForm
{
    public int MascotaId { get; set; }

    // HTML que genera el editor de texto enriquecido (se sanitiza en el servidor)
    public string? ContenidoHtml { get; set; }
}

public class NotasVM
{
    public Mascota Mascota { get; set; } = null!;
    public List<NotaClinica> Notas { get; set; } = new();
    public NotaForm Form { get; set; } = new();
}