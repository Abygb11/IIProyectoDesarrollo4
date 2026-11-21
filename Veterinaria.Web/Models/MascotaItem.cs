namespace Veterinaria.Web.Models;

public class MascotaItem
{
    public int Id { get; set; }
    public int MascotaId { get; set; }
    public Mascota? Mascota { get; set; }
    public int ItemClinicoId { get; set; }
    public ItemClinico? ItemClinico { get; set; }
    public DateTime FechaInicio { get; set; } = DateTime.Now;
    public int AutorInicioId { get; set; }
    public Usuario? AutorInicio { get; set; }
    public DateTime? FechaSuspension { get; set; }
    public int? AutorSuspensionId { get; set; }
    public Usuario? AutorSuspension { get; set; }
}