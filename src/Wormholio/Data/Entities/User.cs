using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wormholio.Data.Entities;

public sealed class User
{
    public int Id { get; private set; }

    public List<Character> Characters { get; set; } = [];
}