namespace Veterinaria.Web.Services;

// Resultado de una subida, con el cual el controlador decide qué mensaje mostrar
public record ResultadoArchivo(
    bool Ok,
    string? Error,
    string? Ruta = null,
    string? NombreOriginal = null,
    string? TipoContenido = null);

public class ArchivoService
{
    private const long TamanoMaximo = 5 * 1024 * 1024; // 5 MB

    // Extensión permitida y tipo de contenido decidido por el servidor (no confiar en lo que diga el cliente)
    private static readonly Dictionary<string, string> TiposImagen =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png"
        };

    private static readonly Dictionary<string, string> TiposExamen =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".pdf"] = "application/pdf"
        };

    private readonly IWebHostEnvironment _env;
    public ArchivoService(IWebHostEnvironment env) => _env = env;

    private string CarpetaExamenes => Path.Combine(_env.ContentRootPath, "App_Data", "examenes");

    // Fotos (veterinarios, mascotas), que se guardan en wwwroot/uploads/<carpeta>
    public Task<ResultadoArchivo> GuardarFotoAsync(IFormFile? archivo, string carpeta)
        => GuardarAsync(archivo, TiposImagen,
            Path.Combine(_env.WebRootPath, "uploads", carpeta),
            $"uploads/{carpeta}/");

    // Exámenes (imagen o PDF), que se guardan en App_Data/examenes
    public Task<ResultadoArchivo> GuardarExamenAsync(IFormFile? archivo)
        => GuardarAsync(archivo, TiposExamen, CarpetaExamenes, "");

    // Ruta física de un examen para entregarlo (null si no existe)
    public string? ObtenerRutaExamen(string nombreArchivo)
    {
        
        var ruta = Path.Combine(CarpetaExamenes, Path.GetFileName(nombreArchivo));
        return File.Exists(ruta) ? ruta : null;
    }

    // Elimina una foto de wwwroot/uploads, si existe y está dentro de esa carpeta
    public void EliminarFoto(string? rutaRelativa)
    {
        if (string.IsNullOrWhiteSpace(rutaRelativa)) return;

        var raiz = Path.GetFullPath(Path.Combine(_env.WebRootPath, "uploads"))
                   + Path.DirectorySeparatorChar;
        var completa = Path.GetFullPath(Path.Combine(
            _env.WebRootPath, rutaRelativa.Replace('/', Path.DirectorySeparatorChar)));

        // Solo se borra dentro de wwwroot/uploads
        if (!completa.StartsWith(raiz, StringComparison.OrdinalIgnoreCase)) return;
        if (File.Exists(completa)) File.Delete(completa);
    }

    // Elimina un examen de App_Data/examenes, si existe
    public void EliminarExamen(string nombreArchivo)
    {
        var ruta = ObtenerRutaExamen(nombreArchivo);
        if (ruta != null) File.Delete(ruta);
    }

    // Lógica común de validación y guardado
    private static async Task<ResultadoArchivo> GuardarAsync(
        IFormFile? archivo,
        Dictionary<string, string> permitidos,
        string carpetaFisica,
        string prefijoRuta)
    {
        if (archivo == null || archivo.Length == 0)
            return new(false, "Seleccione un archivo.");

        if (archivo.Length > TamanoMaximo)
            return new(false, "El archivo supera el tamaño máximo de 5 MB.");

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!permitidos.TryGetValue(extension, out var tipoContenido))
            return new(false, "Formato no permitido. Use: " + string.Join(", ", permitidos.Keys) + ".");

        // Se lee el inicio del archivo para comprobar que realmente es lo que dice ser
        var cabecera = new byte[8];
        int leidos;
        await using (var lectura = archivo.OpenReadStream())
        {
            leidos = await lectura.ReadAtLeastAsync(cabecera, cabecera.Length, throwOnEndOfStream: false);
        }
        if (!FirmaValida(extension, cabecera, leidos))
            return new(false, "El contenido del archivo no corresponde a su formato.");

        Directory.CreateDirectory(carpetaFisica);

        // Nombre generado por nosotros: el nombre del cliente nunca se usa en el disco
        var nombreGuardado = Guid.NewGuid().ToString("N") + extension;
        var rutaFisica = Path.Combine(carpetaFisica, nombreGuardado);

        await using (var origen = archivo.OpenReadStream())
        await using (var destino = new FileStream(rutaFisica, FileMode.Create))
        {
            await origen.CopyToAsync(destino);
        }

        var nombreOriginal = Path.GetFileName(archivo.FileName);
        if (nombreOriginal.Length > 150) nombreOriginal = nombreOriginal[..150];

        return new(true, null, prefijoRuta + nombreGuardado, nombreOriginal, tipoContenido);
    }

    // Compara los primeros bytes (la firma) con los de cada formato
    private static bool FirmaValida(string extension, byte[] c, int n) => extension switch
    {
        ".jpg" or ".jpeg" => n >= 3 && c[0] == 0xFF && c[1] == 0xD8 && c[2] == 0xFF,
        ".png" => n >= 8 && c[0] == 0x89 && c[1] == 0x50 && c[2] == 0x4E && c[3] == 0x47
                         && c[4] == 0x0D && c[5] == 0x0A && c[6] == 0x1A && c[7] == 0x0A,
        ".pdf" => n >= 4 && c[0] == 0x25 && c[1] == 0x50 && c[2] == 0x44 && c[3] == 0x46,
        _ => false
    };
}