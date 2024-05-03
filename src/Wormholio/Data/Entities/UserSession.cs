using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wormholio.Data.Entities;

public sealed class UserSession
{
    public int Id { get; private set; }

    public required User User { get; set; }
}