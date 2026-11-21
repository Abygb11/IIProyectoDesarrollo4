using Microsoft.EntityFrameworkCore;
using Veterinaria.Web.Models;

namespace Veterinaria.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Especie> Especies => Set<Especie>();
    public DbSet<Raza> Razas => Set<Raza>();
    public DbSet<ItemClinico> ItemsClinicos => Set<ItemClinico>();
    public DbSet<Veterinario> Veterinarios => Set<Veterinario>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Mascota> Mascotas => Set<Mascota>();
    public DbSet<Vacuna> Vacunas => Set<Vacuna>();
    public DbSet<MascotaItem> MascotaItems => Set<MascotaItem>();
    public DbSet<Examen> Examenes => Set<Examen>();
    public DbSet<NotaClinica> Notas => Set<NotaClinica>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Ninguna relación borra en cascada (regla: lo que está en uso no se elimina)
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;

        b.Entity<Usuario>().HasIndex(u => u.Cedula).IsUnique();
        b.Entity<Usuario>().HasIndex(u => u.Correo).IsUnique();
        b.Entity<Usuario>().Property(u => u.Perfil).HasConversion<string>();
        b.Entity<ItemClinico>().Property(i => i.Tipo).HasConversion<string>();
        b.Entity<Mascota>().Property(m => m.Sexo).HasConversion<string>();
    }
}