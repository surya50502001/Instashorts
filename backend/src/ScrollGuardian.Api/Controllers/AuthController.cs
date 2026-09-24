using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScrollGuardian.Application.Common.Interfaces;
using ScrollGuardian.Domain.Entities;
using ScrollGuardian.Domain.Enums;
using ScrollGuardian.Infrastructure.Persistence;

namespace ScrollGuardian.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IValidator<RegisterRequestDto> _registerValidator;
    private readonly IValidator<LoginRequestDto> _loginValidator;

    public AuthController(
        ApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        IValidator<RegisterRequestDto> registerValidator,
        IValidator<LoginRequestDto> loginValidator)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto dto)
    {
        var validationResult = await _registerValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        if (await _dbContext.Users.AnyAsync(u => u.Email == normalizedEmail))
        {
            return BadRequest(new { message = "An account with this email address already exists." });
        }

        var user = new User
        {
            Email = normalizedEmail,
            FullName = dto.FullName.Trim(),
            PasswordHash = _passwordHasher.HashPassword(dto.Password),
            Role = UserRole.User,
            Plan = SubscriptionPlan.Free,
            IsEmailVerified = true, // In production, send verification email
            IsActive = true,
            IsOnboarded = false,
            Preference = new UserPreference(),
            PrivacyConsent = new PrivacyConsent(),
            Subscription = new Subscription { Plan = SubscriptionPlan.Free, Status = SubscriptionStatus.Active }
        };

        _dbContext.Users.Add(user);

        var ip = _currentUserService.IpAddress;
        var refreshToken = _jwtTokenService.GenerateRefreshToken(user.Id, ip);
        _dbContext.RefreshTokens.Add(refreshToken);

        await _dbContext.SaveChangesAsync();
        await _auditService.LogAsync(user.Id, "REGISTER", "Auth", new { Email = user.Email }, ip, _currentUserService.UserAgent);

        var accessToken = _jwtTokenService.GenerateAccessToken(user);

        return Ok(new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(120),
            User = new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                Plan = user.Plan,
                IsEmailVerified = user.IsEmailVerified,
                IsOnboarded = user.IsOnboarded,
                CreatedAtUtc = user.CreatedAtUtc
            }
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto dto)
    {
        var validationResult = await _loginValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users
            .Include(u => u.Preference)
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null || !_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { message = "This account has been deactivated." });
        }

        user.LastLoginAtUtc = DateTime.UtcNow;

        var ip = _currentUserService.IpAddress;

        // Register device if provided
        if (!string.IsNullOrEmpty(dto.DeviceIdentifier))
        {
            var device = await _dbContext.Devices
                .FirstOrDefaultAsync(d => d.UserId == user.Id && d.DeviceIdentifier == dto.DeviceIdentifier);

            if (device == null)
            {
                device = new Device
                {
                    UserId = user.Id,
                    DeviceIdentifier = dto.DeviceIdentifier,
                    DeviceName = dto.DeviceName ?? "Browser Extension",
                    DeviceType = dto.DeviceType ?? "Browser",
                    Browser = _currentUserService.UserAgent ?? "Unknown",
                    LastActiveAtUtc = DateTime.UtcNow,
                    IsTrusted = true
                };
                _dbContext.Devices.Add(device);
            }
            else
            {
                device.LastActiveAtUtc = DateTime.UtcNow;
            }
        }

        var refreshToken = _jwtTokenService.GenerateRefreshToken(user.Id, ip);
        _dbContext.RefreshTokens.Add(refreshToken);

        await _dbContext.SaveChangesAsync();
        await _auditService.LogAsync(user.Id, "LOGIN", "Auth", new { Device = dto.DeviceName }, ip, _currentUserService.UserAgent);

        var accessToken = _jwtTokenService.GenerateAccessToken(user);

        return Ok(new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(120),
            User = new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                Plan = user.Plan,
                IsEmailVerified = user.IsEmailVerified,
                IsOnboarded = user.IsOnboarded,
                CreatedAtUtc = user.CreatedAtUtc
            }
        });
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
        {
            return BadRequest(new { message = "Refresh token is required." });
        }

        var storedToken = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken);

        if (storedToken == null || !storedToken.IsActive || storedToken.User == null)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token." });
        }

        storedToken.RevokedAtUtc = DateTime.UtcNow;

        var ip = _currentUserService.IpAddress;
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken(storedToken.UserId, ip);
        storedToken.ReplacedByToken = newRefreshToken.Token;
        _dbContext.RefreshTokens.Add(newRefreshToken);

        await _dbContext.SaveChangesAsync();

        var accessToken = _jwtTokenService.GenerateAccessToken(storedToken.User);

        return Ok(new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(120),
            User = new UserProfileDto
            {
                Id = storedToken.User.Id,
                Email = storedToken.User.Email,
                FullName = storedToken.User.FullName,
                Role = storedToken.User.Role,
                Plan = storedToken.User.Plan,
                IsEmailVerified = storedToken.User.IsEmailVerified,
                IsOnboarded = storedToken.User.IsOnboarded,
                CreatedAtUtc = storedToken.User.CreatedAtUtc
            }
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetCurrentUser()
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        var user = await _dbContext.Users.FindAsync(userId.Value);
        if (user == null) return NotFound();

        return Ok(new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            Plan = user.Plan,
            IsEmailVerified = user.IsEmailVerified,
            IsOnboarded = user.IsOnboarded,
            CreatedAtUtc = user.CreatedAtUtc
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = _currentUserService.UserId;
        if (userId.HasValue)
        {
            var tokens = await _dbContext.RefreshTokens
                .Where(rt => rt.UserId == userId.Value && rt.RevokedAtUtc == null)
                .ToListAsync();

            foreach (var t in tokens)
            {
                t.RevokedAtUtc = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();
            await _auditService.LogAsync(userId.Value, "LOGOUT", "Auth", ipAddress: _currentUserService.IpAddress, userAgent: _currentUserService.UserAgent);
        }

        return Ok(new { message = "Successfully logged out." });
    }
}
