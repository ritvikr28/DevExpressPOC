#nullable enable
using DXApplication1.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DXApplication1.Services
{
    /// <summary>
    /// Interface for authentication service
    /// </summary>
    public interface IAuthService
    {
        LoginResponse? Authenticate(LoginRequest request);
        ClaimsPrincipal? ValidateToken(string token);
        User? GetUserByUsername(string username);
    }

    /// <summary>
    /// JWT Authentication Service implementation
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthService> _logger;
        private readonly List<User> _users;

        public AuthService(IOptions<JwtSettings> jwtSettings, ILogger<AuthService> logger)
        {
            _jwtSettings = jwtSettings.Value;
            _logger = logger;

            // Initialize demo users - In production, this should come from a database
            // Passwords are hashed using SHA256 for demonstration
            _users = new List<User>
            {
                new User
                {
                    Id = "1",
                    Username = "admin",
                    PasswordHash = HashPassword("admin123"),
                    Roles = new[] { AppRoles.Admin, AppRoles.ReportViewer, AppRoles.ReportEditor },
                    IsActive = true
                },
                new User
                {
                    Id = "2",
                    Username = "reportviewer",
                    PasswordHash = HashPassword("viewer123"),
                    Roles = new[] { AppRoles.ReportViewer },
                    IsActive = true
                },
                new User
                {
                    Id = "3",
                    Username = "reporteditor",
                    PasswordHash = HashPassword("editor123"),
                    Roles = new[] { AppRoles.ReportViewer, AppRoles.ReportEditor },
                    IsActive = true
                }
            };
        }

        /// <summary>
        /// Authenticate user and generate JWT token
        /// </summary>
        public LoginResponse? Authenticate(LoginRequest request)
        {
            try
            {
                var user = _users.FirstOrDefault(u =>
                    u.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase) &&
                    u.IsActive);

                if (user == null)
                {
                    _logger.LogWarning("Authentication failed: User not found - {Username}", request.Username);
                    return null;
                }

                if (!VerifyPassword(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Authentication failed: Invalid password for user - {Username}", request.Username);
                    return null;
                }

                var token = GenerateJwtToken(user);
                var expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

                _logger.LogInformation("User authenticated successfully: {Username}", request.Username);

                return new LoginResponse
                {
                    Token = token,
                    Expiration = expiration,
                    Username = user.Username,
                    Roles = user.Roles
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication error for user: {Username}", request.Username);
                return null;
            }
        }

        /// <summary>
        /// Validate JWT token and return claims principal
        /// </summary>
        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                return null;
            }
        }

        /// <summary>
        /// Get user by username
        /// </summary>
        public User? GetUserByUsername(string username)
        {
            return _users.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
                u.IsActive);
        }

        /// <summary>
        /// Generate JWT token for authenticated user
        /// </summary>
        private string GenerateJwtToken(User user)
        {
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);
            var securityKey = new SymmetricSecurityKey(key);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Add role claims
            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Hash password using SHA256
        /// In production, use a proper password hashing algorithm like bcrypt or Argon2
        /// </summary>
        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        /// <summary>
        /// Verify password against hash
        /// </summary>
        private static bool VerifyPassword(string password, string passwordHash)
        {
            var hash = HashPassword(password);
            return hash == passwordHash;
        }
    }
}
