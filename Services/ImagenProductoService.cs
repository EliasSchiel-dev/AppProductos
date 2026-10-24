using System.Security.Cryptography;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace AppProductos.Services;

/// <summary>Resultado de intentar guardar una imagen.</summary>
public record ResultadoImagen(bool Ok, string? Error, string? Url, long Bytes, string? Sha256)
{
    public static ResultadoImagen Fallo(string error) => new(false, error, null, 0, null);
}

public interface IImagenProductoService
{
    Task<ResultadoImagen> GuardarAsync(IFormFile archivo, CancellationToken ct = default);
    void Eliminar(string? imagenUrl);
}

/// <summary>
/// Guarda las imágenes de los productos en wwwroot/uploads.
///
/// Por qué no alcanza con copiar los bytes que llegan (lo que hacía la versión anterior):
///  1. Solo se miraba la extensión: un archivo dañado o que no era una imagen se guardaba igual
///     y recién se notaba al intentar mostrarlo.
///  2. Las fotos de celular son enormes (varios MB) y vienen rotadas por metadatos EXIF.
///
/// Ahora el archivo se DECODIFICA de verdad (si está dañado, falla en el momento y con un mensaje claro),
/// se endereza, se reduce a 1024 px como máximo y se guarda como WebP. Además se escribe primero en un
/// archivo temporal y recién se renombra cuando está completo, así nunca se sirve una imagen a medias.
/// </summary>
public class ImagenProductoService : IImagenProductoService
{
    private const long TamanoMaximoBytes = 5L * 1024 * 1024;   // 5 MB
    private const int LadoMaximoPx = 1024;
    private const long PixelesMaximos = 25_000_000;             // ~25 megapíxeles (evita agotar la memoria del hosting)
    private static readonly string[] FormatosPermitidos = { "JPEG", "PNG", "WEBP" };

    private readonly string _carpeta;
    private readonly ILogger<ImagenProductoService> _logger;

    public ImagenProductoService(IWebHostEnvironment env, ILogger<ImagenProductoService> logger)
    {
        var raiz = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        _carpeta = Path.Combine(raiz, "uploads");
        Directory.CreateDirectory(_carpeta);
        _logger = logger;
    }

    public async Task<ResultadoImagen> GuardarAsync(IFormFile archivo, CancellationToken ct = default)
    {
        if (archivo.Length == 0)
            return ResultadoImagen.Fallo("No se envió ningún archivo.");
        if (archivo.Length > TamanoMaximoBytes)
            return ResultadoImagen.Fallo("La imagen supera el máximo de 5 MB.");

        // Pasamos todo a memoria (máx. 5 MB): así el flujo se puede leer más de una vez.
        using var memoria = new MemoryStream();
        await archivo.CopyToAsync(memoria, ct);

        // Si lo recibido no coincide con lo que el cliente dijo que enviaba, la subida se cortó.
        if (memoria.Length != archivo.Length)
        {
            _logger.LogWarning("Subida incompleta: se esperaban {Esperados} bytes y llegaron {Recibidos}.",
                archivo.Length, memoria.Length);
            return ResultadoImagen.Fallo("La subida llegó incompleta. Intenta nuevamente.");
        }

        var rutaTemporal = string.Empty;
        try
        {
            // 1) Identificar el formato REAL mirando el contenido (no la extensión)
            memoria.Position = 0;
            var info = await Image.IdentifyAsync(memoria, ct);
            var formato = info.Metadata.DecodedImageFormat?.Name;

            if (formato is null || !FormatosPermitidos.Contains(formato, StringComparer.OrdinalIgnoreCase))
                return ResultadoImagen.Fallo("Formato no permitido. Usa JPG, PNG o WebP.");

            if ((long)info.Width * info.Height > PixelesMaximos)
                return ResultadoImagen.Fallo("La imagen tiene demasiados píxeles. Usa una más chica.");

            // 2) Decodificar completa: si el archivo está dañado, falla acá
            memoria.Position = 0;
            using var imagen = await Image.LoadAsync(memoria, ct);

            // 3) Enderezar (las fotos de celular guardan la rotación en metadatos) y reducir si hace falta
            imagen.Mutate(x => x.AutoOrient());
            if (imagen.Width > LadoMaximoPx || imagen.Height > LadoMaximoPx)
            {
                imagen.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(LadoMaximoPx, LadoMaximoPx)
                }));
            }

            // 4) Guardar primero en un temporal y renombrar al terminar (escritura "atómica")
            var nombre = $"{Guid.NewGuid():N}.webp";
            var rutaFinal = Path.Combine(_carpeta, nombre);
            rutaTemporal = rutaFinal + ".tmp";

            await imagen.SaveAsync(rutaTemporal, new WebpEncoder { Quality = 80 }, ct);
            File.Move(rutaTemporal, rutaFinal);
            rutaTemporal = string.Empty;

            // 5) Tamaño y huella (SHA-256) del archivo YA guardado. Sirven para comparar
            //    "lo que quedó en el servidor" contra "lo que se descarga" si algo se rompe.
            long bytes;
            string sha256;
            await using (var guardado = File.OpenRead(rutaFinal))
            {
                bytes = guardado.Length;
                sha256 = Convert.ToHexString(await SHA256.HashDataAsync(guardado, ct)).ToLowerInvariant();
            }

            _logger.LogInformation("Imagen guardada: {Nombre} ({Bytes} bytes, sha256 {Sha})", nombre, bytes, sha256);
            return new ResultadoImagen(true, null, $"/uploads/{nombre}", bytes, sha256);
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException or NotSupportedException)
        {
            _logger.LogWarning(ex, "Imagen inválida o dañada: {Archivo} ({Bytes} bytes)", archivo.FileName, archivo.Length);
            return ResultadoImagen.Fallo("El archivo no es una imagen válida o está dañado.");
        }
        finally
        {
            // Si algo falló a mitad de camino, no dejamos basura en el disco
            if (rutaTemporal.Length > 0 && File.Exists(rutaTemporal))
            {
                try { File.Delete(rutaTemporal); }
                catch (IOException) { /* si no se puede borrar ahora, no es grave */ }
            }
        }
    }

    public void Eliminar(string? imagenUrl)
    {
        if (string.IsNullOrWhiteSpace(imagenUrl)) return;

        // GetFileName descarta cualquier carpeta ("../../x"): solo se puede borrar dentro de uploads
        var nombre = Path.GetFileName(imagenUrl);
        if (string.IsNullOrEmpty(nombre)) return;

        var ruta = Path.Combine(_carpeta, nombre);
        try
        {
            if (File.Exists(ruta)) File.Delete(ruta);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "No se pudo borrar la imagen anterior {Ruta}", ruta);
        }
    }
}
