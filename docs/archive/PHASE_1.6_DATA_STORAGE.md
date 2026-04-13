# Phase 1.6: SQLite Data Storage Migration

## Prerequisites

**Must complete first:**
- Phase 1.5: Core library extraction, .NET 8 migration, abstractions in place
- Verify: Scanner still works correctly after Phase 1.5 changes

## Problem Statement

### Current Issues with Flat JSON Files

**inventorylists/*.json files have multiple problems:**

1. **Staleness after v5.5.0 encryption**
   - HoYoverse encrypted game data, breaking automated extraction
   - All Dimbreath-based sources stopped updating
   - Manual maintenance required for new characters/weapons/artifacts

2. **Difficult community contribution**
   - Contributors need to understand JSON schema
   - Easy to introduce syntax errors or schema violations
   - No validation until runtime (scanner crashes or fails to match)
   - GitHub PRs for data updates require code review

3. **Poor data integrity**
   - No foreign key constraints (can reference non-existent elements)
   - No enum validation (weapon type can be any int, not just 0-4)
   - No uniqueness enforcement (duplicates possible)
   - No audit trail (when was data added? by whom? from which source?)

4. **Limited query capability**
   - Must load entire JSON file to find one character
   - Can't filter "all Pyro Catalyst users" without parsing everything
   - No indexing for performance

5. **Schema evolution is manual**
   - Adding new fields requires editing every entry
   - No migration system
   - Breaking changes require rewriting entire files

## Solution: SQLite + EF Core

### Why SQLite?

- **Zero infrastructure** - Single .db file, ships with app
- **Cross-platform** - Works on Windows, macOS, Linux
- **Performant** - Faster than parsing JSON for each scan
- **Transactional** - ACID guarantees, no partial writes
- **Self-contained** - No separate database server required
- **Reliable** - Used by browsers, mobile apps, embedded systems

### Why EF Core?

- **.NET 8 native ORM** - First-class support
- **Migrations** - Schema evolution handled automatically
- **Type safety** - Compile-time checking of queries
- **LINQ support** - Expressive query syntax
- **Code-first** - Models define schema, DB generated from code
- **Validation** - Data annotations enforce constraints

## Database Schema

### Core Tables

```csharp
// Characters
public class Character
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string InternalKey { get; set; }  // "hutao", "kaedeharakazuha"

    [Required, MaxLength(50)]
    public string GoodName { get; set; }  // "HuTao", "KaedeharaKazuha"

    [Required]
    public ElementType Element { get; set; }  // Enum: Pyro, Hydro, etc.

    [Required]
    public WeaponType WeaponType { get; set; }  // Enum: 0-4

    [Required]
    public int Rarity { get; set; }  // 4 or 5

    // Navigation properties
    public ICollection<CharacterConstellation> Constellations { get; set; }
    public ICollection<CharacterTalent> Talents { get; set; }
    public ICollection<TravelerElement> TravelerElements { get; set; }  // Only populated for Traveler

    // Metadata
    public DateTime AddedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public string DataSource { get; set; }  // "dvaJi", "manual", "dimbreath"
    public string GameVersion { get; set; }  // "6.4", "6.5"
}

// Special case: Traveler multi-element
public class TravelerElement
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CharacterId { get; set; }  // FK to Character (Traveler)

    [Required]
    public ElementType Element { get; set; }

    [Required]
    public string ConstellationOrder { get; set; }  // JSON: ["skill", "burst"]

    // Navigation
    public Character Character { get; set; }
}

// Character Constellations
public class CharacterConstellation
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CharacterId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; }  // "Princeps Cretaceus"

    [Required]
    public int Level { get; set; }  // 1-6

    // Navigation
    public Character Character { get; set; }
}

// Weapons
public class Weapon
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string InternalKey { get; set; }  // "skyridergreatsword"

    [Required, MaxLength(50)]
    public string GoodName { get; set; }  // "SkyriderGreatsword"

    [Required]
    public WeaponType Type { get; set; }

    [Required, Range(1, 5)]
    public int Rarity { get; set; }

    // Metadata
    public DateTime AddedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public string DataSource { get; set; }
    public string GameVersion { get; set; }
}

// Artifact Sets
public class ArtifactSet
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string InternalKey { get; set; }  // "crimsonwitchofflames"

    [Required, MaxLength(50)]
    public string GoodName { get; set; }  // "CrimsonWitchOfFlames"

    [Required, Range(1, 5)]
    public int MaxRarity { get; set; }

    // Metadata
    public DateTime AddedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public string DataSource { get; set; }
    public string GameVersion { get; set; }
}

// Materials
public class Material
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string InternalKey { get; set; }

    [Required, MaxLength(50)]
    public string GoodName { get; set; }

    [Required]
    public MaterialType Type { get; set; }  // CharacterDevelopment, WeaponAscension, etc.

    [Required, Range(1, 5)]
    public int Rarity { get; set; }

    // Metadata
    public DateTime AddedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public string DataSource { get; set; }
    public string GameVersion { get; set; }
}

// Enums
public enum ElementType
{
    Pyro = 0,
    Hydro = 1,
    Anemo = 2,
    Electro = 3,
    Dendro = 4,
    Cryo = 5,
    Geo = 6
}

public enum WeaponType
{
    Sword = 0,
    Claymore = 1,
    Polearm = 2,
    Bow = 3,
    Catalyst = 4
}

public enum MaterialType
{
    CharacterDevelopment,
    WeaponAscension,
    CommonAscension,
    LocalSpecialty,
    Cooking
}
```

### Database Context

```csharp
public class GenshinDbContext : DbContext
{
    public DbSet<Character> Characters { get; set; }
    public DbSet<CharacterConstellation> Constellations { get; set; }
    public DbSet<TravelerElement> TravelerElements { get; set; }
    public DbSet<Weapon> Weapons { get; set; }
    public DbSet<ArtifactSet> ArtifactSets { get; set; }
    public DbSet<Material> Materials { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={GetDatabasePath()}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Unique constraints
        modelBuilder.Entity<Character>()
            .HasIndex(c => c.InternalKey)
            .IsUnique();

        modelBuilder.Entity<Weapon>()
            .HasIndex(w => w.InternalKey)
            .IsUnique();

        // Foreign keys
        modelBuilder.Entity<CharacterConstellation>()
            .HasOne(cc => cc.Character)
            .WithMany(c => c.Constellations)
            .HasForeignKey(cc => cc.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private string GetDatabasePath()
    {
        // Place database in same location as current JSON files
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "inventorylists", "genshin_data.db");
    }
}
```

## Repository Implementation

### Interface (from Phase 1.5 Core)

```csharp
// InventoryKamera.Core/Data/IGameDataRepository.cs
public interface IGameDataRepository
{
    // Characters
    Task<Character?> GetCharacterByNameAsync(string name);
    Task<IEnumerable<Character>> GetAllCharactersAsync();
    Task<IEnumerable<Character>> GetCharactersByElementAsync(ElementType element);

    // Weapons
    Task<Weapon?> GetWeaponByNameAsync(string name);
    Task<IEnumerable<Weapon>> GetAllWeaponsAsync();
    Task<IEnumerable<Weapon>> GetWeaponsByTypeAsync(WeaponType type);

    // Artifacts
    Task<ArtifactSet?> GetArtifactSetByNameAsync(string name);
    Task<IEnumerable<ArtifactSet>> GetAllArtifactSetsAsync();

    // Materials
    Task<Material?> GetMaterialByNameAsync(string name);
    Task<IEnumerable<Material>> GetAllMaterialsAsync();
    Task<IEnumerable<Material>> GetMaterialsByTypeAsync(MaterialType type);
}
```

### SQLite Implementation

```csharp
// InventoryKamera.Infrastructure/Data/SqliteGameDataRepository.cs
public class SqliteGameDataRepository : IGameDataRepository
{
    private readonly GenshinDbContext _context;

    public SqliteGameDataRepository(GenshinDbContext context)
    {
        _context = context;
    }

    public async Task<Character?> GetCharacterByNameAsync(string name)
    {
        // Try internal key first
        var character = await _context.Characters
            .Include(c => c.Constellations)
            .FirstOrDefaultAsync(c => c.InternalKey == name.ToLower());

        // Fallback to GOOD name
        if (character == null)
        {
            character = await _context.Characters
                .Include(c => c.Constellations)
                .FirstOrDefaultAsync(c => c.GoodName.ToLower() == name.ToLower());
        }

        return character;
    }

    public async Task<IEnumerable<Character>> GetAllCharactersAsync()
    {
        return await _context.Characters
            .Include(c => c.Constellations)
            .OrderBy(c => c.GoodName)
            .ToListAsync();
    }

    // ... similar implementations for weapons, artifacts, materials
}
```

### Backwards Compatibility: JSON Repository

```csharp
// Keep existing JSON implementation for Phase 1.5 compatibility
public class JsonGameDataRepository : IGameDataRepository
{
    // Current implementation that reads from JSON files
    // Unchanged from Phase 1.5
}
```

## Migration Strategy

### Step 1: One-time JSON → SQLite Import

```csharp
public class DataMigrationService
{
    private readonly GenshinDbContext _dbContext;

    public async Task MigrateFromJsonAsync(string jsonDirectory)
    {
        // Import characters.json
        var charactersJson = File.ReadAllText(Path.Combine(jsonDirectory, "characters.json"));
        var characterDict = JsonSerializer.Deserialize<Dictionary<string, CharacterJson>>(charactersJson);

        foreach (var kvp in characterDict)
        {
            var character = new Character
            {
                InternalKey = kvp.Key,
                GoodName = kvp.Value.GOOD,
                Element = Enum.Parse<ElementType>(kvp.Value.Element[0], ignoreCase: true),
                WeaponType = (WeaponType)kvp.Value.WeaponType,
                Rarity = kvp.Value.Rarity,
                AddedDate = DateTime.UtcNow,
                DataSource = "json_migration",
                GameVersion = "legacy"
            };

            // Handle constellations
            if (kvp.Value.ConstellationName != null)
            {
                for (int i = 0; i < kvp.Value.ConstellationName.Count; i++)
                {
                    character.Constellations.Add(new CharacterConstellation
                    {
                        Name = kvp.Value.ConstellationName[i],
                        Level = i + 1
                    });
                }
            }

            _dbContext.Characters.Add(character);
        }

        await _dbContext.SaveChangesAsync();

        // Similar for weapons, artifacts, materials...
    }
}
```

### Step 2: Application Startup Logic

```csharp
// On first launch with Phase 1.6
public async Task InitializeDataStorage()
{
    using var dbContext = new GenshinDbContext();

    // Ensure database exists
    await dbContext.Database.EnsureCreatedAsync();

    // Check if migration needed
    if (!await dbContext.Characters.AnyAsync())
    {
        // No data in DB, migrate from JSON
        var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "inventorylists");

        if (Directory.Exists(jsonPath))
        {
            var migrationService = new DataMigrationService(dbContext);
            await migrationService.MigrateFromJsonAsync(jsonPath);

            // Backup JSON files
            var backupPath = Path.Combine(jsonPath, "_json_backup");
            Directory.CreateDirectory(backupPath);
            foreach (var file in Directory.GetFiles(jsonPath, "*.json"))
            {
                File.Copy(file, Path.Combine(backupPath, Path.GetFileName(file)));
            }

            Logger.Info("Successfully migrated JSON data to SQLite");
        }
    }
}
```

### Step 3: Update DatabaseManager

```csharp
// Modified to upsert into SQLite instead of writing JSON
public class DatabaseManager
{
    private readonly GenshinDbContext _dbContext;
    private readonly HttpClient _httpClient;

    public async Task UpdateAllAsync()
    {
        await UpdateCharactersAsync();
        await UpdateWeaponsAsync();
        await UpdateArtifactsAsync();
        await UpdateMaterialsAsync();
    }

    private async Task UpdateCharactersAsync()
    {
        // Fetch from dvaJi
        var charactersData = await FetchCharactersFromDvaJiAsync();

        foreach (var charData in charactersData)
        {
            // Check if exists
            var existing = await _dbContext.Characters
                .FirstOrDefaultAsync(c => c.InternalKey == charData.InternalKey);

            if (existing != null)
            {
                // Update existing
                existing.GoodName = charData.GoodName;
                existing.Element = charData.Element;
                existing.UpdatedDate = DateTime.UtcNow;
                existing.DataSource = "dvaJi";
                existing.GameVersion = charData.GameVersion;
            }
            else
            {
                // Insert new
                _dbContext.Characters.Add(new Character
                {
                    InternalKey = charData.InternalKey,
                    GoodName = charData.GoodName,
                    Element = charData.Element,
                    WeaponType = charData.WeaponType,
                    Rarity = charData.Rarity,
                    AddedDate = DateTime.UtcNow,
                    DataSource = "dvaJi",
                    GameVersion = charData.GameVersion
                });
            }
        }

        await _dbContext.SaveChangesAsync();
    }
}
```

## Admin UI for Community Data Entry - DEFERRED TO PHASE 2

**Decision:** Admin UI is deferred to Phase 2 (Avalonia) to avoid building WinForms CRUD dialogs that will be immediately replaced.

**Phase 1.6 Scope:**
- Focus on data layer only (SQLite, EF Core, migrations)
- No UI for manual data entry
- Community can still contribute via:
  - Direct SQL inserts (for technical users)
  - Pull requests with migration scripts
  - Wait for Phase 2 Avalonia admin UI

**Phase 2 Avalonia Admin UI (Deferred):**
- MVVM architecture with proper data binding
- Modern validation and error handling
- Cross-platform from the start
- Better UX with Avalonia controls
- Features:
  - DataGrid for browsing characters/weapons/artifacts
  - Add/Edit/Delete dialogs with validation
  - Bulk import from community submissions
  - Data versioning and audit trail

## JSON Export for GOOD Format

```csharp
// Scanner still exports to GOOD JSON format for compatibility
public class GoodExporter
{
    private readonly IGameDataRepository _repository;

    public async Task<string> ExportToJsonAsync(Inventory inventory)
    {
        var good = new GoodFormat
        {
            format = "GOOD",
            version = 2,
            source = "Inventory Kamera",
            characters = await MapCharactersAsync(inventory.Characters),
            weapons = await MapWeaponsAsync(inventory.Weapons),
            artifacts = await MapArtifactsAsync(inventory.Artifacts),
            materials = await MapMaterialsAsync(inventory.Materials)
        };

        return JsonSerializer.Serialize(good, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
```

## Performance Considerations

### Current (JSON):
- Load entire characters.json (27KB, 112 entries) on every scan
- Linear search through array for name matching
- ~O(n) lookup time

### SQLite with Indexes:
- Index on `InternalKey` and `GoodName`
- Query returns single row
- ~O(log n) lookup time with B-tree index
- In practice: <1ms for lookups vs ~10ms parsing JSON

### Caching Strategy:
```csharp
// Cache commonly accessed data
public class CachedGameDataRepository : IGameDataRepository
{
    private readonly SqliteGameDataRepository _inner;
    private Dictionary<string, Character>? _characterCache;

    public async Task<Character?> GetCharacterByNameAsync(string name)
    {
        if (_characterCache == null)
        {
            // Warm cache on first access
            var all = await _inner.GetAllCharactersAsync();
            _characterCache = all.ToDictionary(c => c.InternalKey);
        }

        return _characterCache.GetValueOrDefault(name.ToLower());
    }
}
```

## Testing Strategy

### Unit Tests

```csharp
public class SqliteGameDataRepositoryTests
{
    [Fact]
    public async Task GetCharacterByName_ExistingCharacter_ReturnsCharacter()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<GenshinDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;

        using var context = new GenshinDbContext(options);
        context.Characters.Add(new Character
        {
            InternalKey = "hutao",
            GoodName = "HuTao",
            Element = ElementType.Pyro,
            WeaponType = WeaponType.Polearm,
            Rarity = 5
        });
        await context.SaveChangesAsync();

        var repository = new SqliteGameDataRepository(context);

        // Act
        var result = await repository.GetCharacterByNameAsync("hutao");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("HuTao", result.GoodName);
        Assert.Equal(ElementType.Pyro, result.Element);
    }
}
```

### Integration Tests

```csharp
// Test full migration pipeline
[Fact]
public async Task MigrateFromJson_ValidJsonFiles_CreatesDatabase()
{
    // Arrange
    var tempDb = Path.GetTempFileName();
    var jsonPath = "./testdata/inventorylists";

    // Act
    var migrationService = new DataMigrationService(new GenshinDbContext(tempDb));
    await migrationService.MigrateFromJsonAsync(jsonPath);

    // Assert
    using var context = new GenshinDbContext(tempDb);
    var characterCount = await context.Characters.CountAsync();
    Assert.True(characterCount > 0, "Should have migrated characters from JSON");

    // Verify specific known characters exist (more robust than hardcoded count)
    Assert.True(await context.Characters.AnyAsync(c => c.InternalKey == "hutao"));
    Assert.True(await context.Characters.AnyAsync(c => c.InternalKey == "raidenshogun"));
}
```

## Rollback Plan

If Phase 1.6 has issues:

1. **Keep JSON files as backup** - Don't delete them
2. **Toggle flag** - `UseSqlite` setting defaults to false initially
3. **Fallback to JSON repository** - Swap implementation in DI container
4. **Export DB to JSON** - Tool to regenerate JSON from SQLite if needed

```csharp
// Dependency injection setup
if (Settings.Default.UseSqlite)
{
    services.AddDbContext<GenshinDbContext>();
    services.AddScoped<IGameDataRepository, SqliteGameDataRepository>();
}
else
{
    services.AddScoped<IGameDataRepository, JsonGameDataRepository>();
}
```

## Timeline

**Phase 1.6 broken into sub-phases:**

### 1.6.1: Schema & Models (1 week)
- Define EF Core models
- Create GenshinDbContext
- Write migrations
- Unit tests for models

### 1.6.2: Repository Implementation (1 week)
- Implement SqliteGameDataRepository
- Keep JsonGameDataRepository for fallback
- Integration tests
- Performance benchmarks

### 1.6.3: Migration Tool (1 week)
- JSON → SQLite importer
- Validation of migrated data
- Backup/restore utilities
- Documentation

### 1.6.4: DatabaseManager Update (1 week)
- Modify dvaJi fetch logic to upsert to DB
- Update manual entry workflows
- Test with real game data

### 1.6.5: Integration & Testing (1 week)
- Full scanner tests with SQLite backend
- Performance validation
- Edge case handling
- Documentation updates

**Total: ~5 weeks** (reduced from 7 weeks by deferring Admin UI to Phase 2)

## Success Criteria

Phase 1.6 complete when:

- ✅ Scanner reads character/weapon/artifact/material data from SQLite
- ✅ DatabaseManager updates pull from dvaJi and upsert to DB
- ✅ Migration tool successfully imports all existing JSON data
- ✅ Performance is equal or better than JSON parsing
- ✅ Rollback to JSON repository works via config flag
- ✅ All unit tests pass
- ✅ All integration tests pass
- ✅ GOOD export still works correctly
- ✅ Database schema supports all current GOOD format fields
- ✅ Phase 2 can build on this foundation without schema changes

## Future Enhancements (Post-1.6)

- **Web-based admin UI** - Community can contribute without installing app
- **Multi-user editing** - Track who added/updated data
- **Approval workflow** - PRs for data changes, not code changes
- **Data versioning** - Git-like history for database changes
- **Conflict resolution** - When multiple sources have different data
- **Schema evolution** - Handle game updates that add new fields
- **Import/export tools** - Share data between installations
- **Validation rules** - Enforce game constraints (e.g., no 6★ artifacts)

---

**Status:** Planning document - Phase 1.5 must complete first
**Last Updated:** 2026-04-08
