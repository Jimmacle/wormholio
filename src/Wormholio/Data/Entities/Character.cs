using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wormholio.Data.Entities;

[EntityTypeConfiguration<Configuration, Character>()]
public sealed class Character
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public int LocationId { get; set; }

    public int ShipTypeId { get; set; }

    public required string AccessToken { get; set; }

    public required string RefreshToken { get; set; }

    public User? User { get; set; }

    public sealed class Configuration : IEntityTypeConfiguration<Character>
    {
        public void Configure(EntityTypeBuilder<Character> builder)
        {
            builder.HasOne(x => x.User).WithMany(x => x.Characters);
        }
    }
}