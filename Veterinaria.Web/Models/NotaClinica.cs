namespace Veterinaria.Web.Models;

public class NotaClinica
{
    public int Id { get; set; }
    public int MascotaId { get; set; }
    public Mascota? Mascota { get; set; }
    public string ContenidoHtml { get; set; } = "";
    public DateTime FechaHora { get; set; } = DateTime.Now;
    public int AutorId { get; set; }
    public Usuario? Autor { get; set; }
}