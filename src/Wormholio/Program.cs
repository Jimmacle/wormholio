using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vite.AspNetCore.Extensions;
using Wormholio;
using Wormholio.Data;
using Wormholio.Data.Entities;
using Wormholio.Esi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.LoginPath = "/auth/login";
});
builder.Services.AddAuthorization();
builder.Services.AddSingleton(new EsiClient(builder.Configuration["Esi:ClientId"], builder.Configuration["Esi:Secret"], builder.Configuration["Esi:CallbackUrl"]));
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddViteServices(options =>
    {
        options.Server.AutoRun = true;
        options.Server.Https = true;
        options.Base = "../Client/assets";
        options.PackageDirectory = "Client";
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseWebSockets();
    app.UseViteDevelopmentServer(true);
}

app.UseFileServer();
app.UseAuthentication();
app.UseAuthorization();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/auth/logout", async (HttpContext httpContext, AppDbContext db) =>
{
    var sessionId = httpContext.User.Claims.FirstOrDefault(claim => claim.Type == "SessionId")?.Value;
    await httpContext.SignOutAsync();
    if (sessionId is not null)
    {
        await db.UserSessions.Where(x => x.Id == int.Parse(sessionId)).ExecuteDeleteAsync();
    }
    return Results.Redirect("/");
});

app.MapGet("/auth/login", (EsiClient esi) => Results.Redirect(esi.GenerateOAuthUrl([])));

app.MapGet("/auth/callback", async (AppDbContext db, EsiClient esi, HttpContext httpContext, [FromQuery] string code) =>
{
    var tokens = await esi.GetTokens(code);
    var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);
    var characterId = int.Parse(jwt.Claims.First(claim => claim.Type == "sub").Value.Split(':').Last());
    var characterName = jwt.Claims.First(claim => claim.Type == "name").Value;
    var character = await db.Characters.Include(x => x.User).FirstOrDefaultAsync(c => c.Id == characterId);
    if (character is null)
    {
        character = new Character
        {
            Id = characterId,
            Name = characterName,
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken
        };

        db.Characters.Add(character);
    }
    else
    {
        character.AccessToken = tokens.AccessToken;
        character.RefreshToken = tokens.RefreshToken;
    }

    var sessionId = httpContext.User.Claims.FirstOrDefault(claim => claim.Type == "SessionId")?.Value;
    if (sessionId is not null)
    {
        var user = await db.UserSessions.Where(x => x.Id == int.Parse(sessionId)).Select(x => x.User).FirstOrDefaultAsync();
        character.User = user;
        await db.SaveChangesAsync();
    }
    else
    {
        var user = character.User;
        if (user is null)
        {
            user = new User { Characters = [character] };
        }

        var session = new UserSession { User = user };

        db.UserSessions.Add(session);
        await db.SaveChangesAsync();
        await httpContext.SignInAsync(
            new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim("SessionId", session.Id.ToString()) },
            CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = true });
    }

    return Results.Redirect("/");
});

app.MapGet("/me", async (ClaimsPrincipal User, AppDbContext db) =>
{
    var sessionId = int.Parse(User.Claims.First(claim => claim.Type == "SessionId").Value);
    var characters = await db.UserSessions.Where(x => x.Id == sessionId).SelectMany(x => x.User.Characters).ToListAsync();
    return characters.Select(x => x.Name);
}).RequireAuthorization();

app.MapPost("/signatures/paste", ([FromBody] SigPasteDto paste) => Signature.TryParseClipboard(paste.Clipboard, out var list) ? list : null);

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

record SigPasteDto(int System, string Clipboard);