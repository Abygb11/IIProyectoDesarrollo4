namespace Veterinaria.Web.Models;

public class Mascota
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public int EspecieId { get; set; }
    public Especie? Especie { get; set; }
    public int RazaId { get; set; }
    public Raza? Raza { get; set; }
    public DateTime FechaNacimiento { get; set; }
    public Sexo Sexo { get; set; }
    public string? FotoRuta { get; set; }
    public int DuenoId { get; set; }
    public Usuario? Dueno { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime? UltimaAtencion { get; set; }

    public List<Vacuna> Vacunas { get; set; } = new();
    public List<MascotaItem> Items { get; set; } = new();
    public List<Examen> Examenes { get; set; } = new();
    public List<NotaClinica> Notas { get; set; } = new();
}