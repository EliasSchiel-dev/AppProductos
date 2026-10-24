using AppProductos.Components;
using AppProductos.Data;
using AppProductos.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllers();

// ---------------------------------------------------------------------------
// Configuración obligatoria. Los valores reales NO están en el código:
//  - Desarrollo: "dotnet user-secrets" (ver docs/como-correr-el-proyecto.md)
//  - Producción: appsettings.Production.json copiado a mano al servidor
// Si falta algo, la app se detiene con un mensaje claro en vez de fallar más adelante.
// ---------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "Falta la cadena de conexión 'ConnectionStrings:DefaultConnection'. " +
        "Cárgala con 'dotnet user-secrets set' (desarrollo) o en appsettings.Production.json (servidor).");

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException(
        "Falta 'Jwt:Key' o es demasiado corta (mínimo 32 caracteres). " +
        "Cárgala con 'dotnet user-secrets set' (desarrollo) o en appsettings.Production.json (servidor).");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Servicio que valida, reduce y guarda las imágenes de los productos
builder.Services.AddSingleton<IImagenProductoService, ImagenProductoService>();


builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirTodo", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Crea el administrador inicial si todavía no existe ninguno.
// Si la base no está lista, la app arranca igual y deja el error en el log.
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await DbSeeder.SembrarAdminAsync(db, builder.Configuration, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "No se pudo verificar/crear el usuario administrador inicial.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Archivos estáticos (wwwroot). Las imágenes subidas tienen nombre único (GUID) y nunca cambian,
// por eso se pueden cachear una semana: la app móvil no las vuelve a descargar cada vez.
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (ctx.Context.Request.Path.StartsWithSegments("/uploads"))
            ctx.Context.Response.Headers.CacheControl = "public,max-age=604800,immutable";
    }
});

app.UseAntiforgery();

app.UseCors("PermitirTodo");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
