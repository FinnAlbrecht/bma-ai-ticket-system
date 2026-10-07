using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Api.Contracts;
using TicketSystem.Infrastructure.Persistence;

namespace TicketSystem.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    TicketDbContext database,
    IPasswordHasher<UserRecord> passwordHasher,
    ILogger<AuthController> logger) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim();
        var displayName = request.DisplayName.Trim();
        if (displayName.Length < 2)
            return BadRequest("Der Anzeigename muss mindestens 2 Zeichen enthalten.");

        var normalizedEmail = email.ToUpperInvariant();
        if (await database.Users.AnyAsync(user => user.NormalizedEmail == normalizedEmail, ct))
            return Conflict("Für diese E-Mail-Adresse existiert bereits ein Konto.");

        var user = new UserRecord
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = normalizedEmail,
            DisplayName = displayName,
            CreatedAtUtcTicks = DateTimeOffset.UtcNow.UtcDateTime.Ticks
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        database.Users.Add(user);
        try
        {
            await database.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            logger.LogInformation("A registration attempt used an email address that already exists.");
            return Conflict("Für diese E-Mail-Adresse existiert bereits ein Konto.");
        }

        await SignInAsync(user);
        return Ok(ToDto(user));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await database.Users.SingleOrDefaultAsync(
            account => account.NormalizedEmail == normalizedEmail,
            ct);

        if (user is null
            || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
                is PasswordVerificationResult.Failed)
        {
            return Unauthorized("E-Mail-Adresse oder Passwort ist falsch.");
        }

        await SignInAsync(user);
        return Ok(ToDto(user));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var user = await database.Users.SingleOrDefaultAsync(account => account.Id == userId, ct);
        return user is null ? Unauthorized() : Ok(ToDto(user));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    private async Task SignInAsync(UserRecord user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Email, user.Email)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
            });
    }

    private static AuthUserDto ToDto(UserRecord user) => new(user.Id, user.Email, user.DisplayName);
}
