using Microsoft.EntityFrameworkCore;
using Wormholio.Data.Entities;

namespace Wormholio.Data;

public sealed class AppDbContext : DbContext
{
    public DbSet<Character> Characters { get; private set; } = null!;

    public DbSet<User> Users { get; private set; } = null!;

    public DbSet<UserSession> UserSessions { get; private set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }
}