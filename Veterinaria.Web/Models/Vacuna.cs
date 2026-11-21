namespace Veterinaria.Web.Models;

public class Vacuna
{
    public int Id { get; set; }
    public int MascotaId { get; set; }
    public Mascota? Mascota { get; set; }
    public string Nombre { get; set; } = "";
    public DateTime FechaAplicacion { get; set; }
    public DateTime FechaProximaDosis { get; set; }
    public int AutorId { get; set; }
    public Usuario? Autor { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}