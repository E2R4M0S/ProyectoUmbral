# Skill: EF Core + PostgreSQL — UMBRAL

## Cómo está configurado en el proyecto

- **`EnsureCreated()`** al startup — no hay migraciones. Si el esquema cambia, el contenedor de DB debe recrearse.
- **Una instancia PostgreSQL por microservicio** — nunca un DbContext accede a la DB de otro servicio.
- Solo `trivia-db` expone puerto al host (5432). Las demás solo son accesibles desde Docker.

## Estructura en cada servicio

```
<Servicio>.Infrastructure/
  Persistence/
    <Servicio>DbContext.cs
    DependencyInjection.cs      ← registra el DbContext
    Configurations/
      <Entidad>Configuration.cs ← IEntityTypeConfiguration<T>
  Repositories/
    <Entidad>Repository.cs
```

## Template: DbContext

```csharp
public class MissionsDbContext : DbContext
{
    public MissionsDbContext(DbContextOptions<MissionsDbContext> options) : base(options) { }

    public DbSet<Mission> Missions => Set<Mission>();
    public DbSet<MissionStage> MissionStages => Set<MissionStage>();
    public DbSet<MissionClue> MissionClues => Set<MissionClue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MissionsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

## Template: Entity Configuration

```csharp
public class MissionConfiguration : IEntityTypeConfiguration<Mission>
{
    public void Configure(EntityTypeBuilder<Mission> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Description)
            .HasMaxLength(1000);

        builder.Property(m => m.Difficulty)
            .HasConversion<string>();  // guarda el enum como string

        builder.Property(m => m.Status)
            .HasConversion<string>();

        builder.Property(m => m.Type)
            .HasConversion<string>();

        // Relación 1:N con MissionStage
        builder.HasMany(m => m.Stages)
            .WithOne()
            .HasForeignKey(s => s.MissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("missions");
    }
}
```

## Guardar Value Objects como JSONB (Sessions.Service)

```csharp
// SessionConfiguration.cs
public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.HasKey(s => s.Id);

        // List<SessionStage> guardado como JSONB
        builder.Property(s => s.Stages)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<List<SessionStage>>(v, JsonSerializerOptions.Default) ?? new List<SessionStage>()
            );

        builder.Property(s => s.Status)
            .HasConversion<string>();

        // Relación 1:N con SessionParticipant
        builder.HasMany(s => s.Participants)
            .WithOne()
            .HasForeignKey(p => p.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("sessions");
    }
}
```

## Template: Repository

```csharp
public class MissionRepository : IMissionRepository
{
    private readonly MissionsDbContext _db;

    public MissionRepository(MissionsDbContext db) => _db = db;

    public async Task<Mission?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Missions
            .Include(m => m.Stages)
            .ThenInclude(s => s.Clues)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task AddAsync(Mission mission, CancellationToken ct = default)
    {
        await _db.Missions.AddAsync(mission, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Mission mission, CancellationToken ct = default)
    {
        _db.Missions.Update(mission);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsByTitleAsync(string title, CancellationToken ct = default)
        => await _db.Missions.AnyAsync(m => m.Title == title, ct);
}
```

## Registro en DependencyInjection

```csharp
// Infrastructure/DependencyInjection.cs
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.AddDbContext<MissionsDbContext>(options =>
        options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

    // EnsureCreated en startup
    services.AddScoped<IMissionRepository, MissionRepository>();

    return services;
}

// En Program.cs, después de Build():
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MissionsDbContext>();
    db.Database.EnsureCreated();
}
```

## Connection string en docker-compose

```yaml
environment:
  - ConnectionStrings__DefaultConnection=Host=missions-db;Port=5432;Database=missions;Username=missions;Password=missions123
```

## Gotchas importantes

1. **`EnsureCreated()` no aplica cambios** a una DB existente. Si cambias el modelo, debes:
   ```bash
   docker compose stop missions.service missions-db
   docker compose rm -f missions-db
   docker compose up -d missions-db missions.service
   ```

2. **Usar `HasConversion<string>()` para enums** — guarda como texto legible, no como número.

3. **JSONB en PostgreSQL** requiere `Npgsql.EntityFrameworkCore.PostgreSQL` y la conversión explícita (ver SessionStage arriba).

4. **Siempre `AsNoTracking()` en Queries** si no vas a modificar la entidad:
   ```csharp
   return await _db.Missions.AsNoTracking()
       .Select(m => new MissionListItemDto(m.Id, m.Title, ...))
       .ToListAsync(ct);
   ```
