using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Web.Data;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Veterinaria.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
    
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<AtencionService>();
builder.Services.AddScoped<ArchivoService>();

//el esquema es un identificador que se le da a la autenticación, en este caso se le da el nombre Bearer,
// que es el esquema que se usa para los tokens JWT
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Cuenta/Login";
        o.AccessDeniedPath = "/Cuenta/Denegado";
    })
    .AddJwtBearer("Bearer", o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ClockSkew = TimeSpan.Zero
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // Web: página de error. API: JSON con 500, sin redirecciones.
    app.UseWhen(c => !c.Request.Path.StartsWithSegments("/api"),
        b => b.UseExceptionHandler("/Home/Error"));

    app.UseWhen(c => c.Request.Path.StartsWithSegments("/api"),
        b => b.UseExceptionHandler(a => a.Run(async c =>
        {
            c.Response.StatusCode = 500;
            await c.Response.WriteAsJsonAsync(new { mensaje = "Error del servidor." });
        })));

    app.UseHsts();
}

app.UseHttpsRedirection();
    app.UseStaticFiles();   
app.UseRouting();

app.UseAuthentication(); 
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
