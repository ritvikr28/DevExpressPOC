#nullable enable
using DXApplication1.Models;
using DXApplication1.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DXApplication1.Controllers
{
    /// <summary>
    /// Authentication controller for handling login and token operations
    /// </summary>
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticate user and return JWT token
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>JWT token and user information</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Invalid request", details = ModelState });
            }

            var response = _authService.Authenticate(request);

            if (response == null)
            {
                _logger.LogWarning("Failed login attempt for user: {Username}", request.Username);
                return Unauthorized(new { error = "Invalid username or password" });
            }

            _logger.LogInformation("Successful login for user: {Username}", request.Username);
            return Ok(response);
        }

        /// <summary>
        /// Validate current token and return user information
        /// </summary>
        /// <returns>Current user information</returns>
        [HttpGet("me")]
        [Authorize]
        public ActionResult<object> GetCurrentUser()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var user = _authService.GetUserByUsername(username);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            return Ok(new
            {
                username = user.Username,
                roles = user.Roles,
                isActive = user.IsActive
            });
        }

        /// <summary>
        /// Verify if the current token is valid
        /// </summary>
        /// <returns>Token validation status</returns>
        [HttpGet("verify")]
        [Authorize]
        public ActionResult<object> VerifyToken()
        {
            return Ok(new
            {
                valid = true,
                username = User.Identity?.Name,
                roles = User.Claims
                    .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToArray()
            });
        }
    }
}
