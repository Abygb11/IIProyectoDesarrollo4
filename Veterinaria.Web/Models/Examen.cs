namespace Veterinaria.Web.Models;

public class Examen
{
    public int Id { get; set; }
    public int MascotaId { get; set; }
    public Mascota? Mascota { get; set; }
    public string Descripcion { get; set; } = "";
    public string ArchivoRuta { get; set; } = "";
    public string NombreOriginal { get; set; } = "";
    public string TipoContenido { get; set; } = "";
    public int AutorId { get; set; }
    public Usuario? Autor { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}