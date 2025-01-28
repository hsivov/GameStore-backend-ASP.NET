using GameStore.Models.DTO;
using GameStore.Models.Entities;
using GameStore.Models.Enums;
using GameStore.Services;
using GameStore.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Web;

namespace GameStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly JwtUtil _jwtUtil;

        public AuthController(UserManager<ApplicationUser> userManager, IEmailService emailService, JwtUtil jwtUtil)
        {
            _userManager = userManager;
            _emailService = emailService;
            _jwtUtil = jwtUtil;
        }

        [Authorize]
        [HttpGet("user")]
        public async Task<IActionResult> GetUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return Unauthorized(new { message = "The provided token is invalid or has expired. Please authenticate again." });
            }

            var user = await _userManager.Users
                .Include(u => u.OwnedGames)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var userDto = new UserDTO
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Age = user.Age,
                Role = user.Role.ToString(),
                OwnedGames = user.OwnedGames.Select(g => new OwnedGameDTO
                {
                    Id = g.Id,
                    Title = g.Title,
                    ImageUrl = g.ImageUrl,
                }).ToList(),
                ProfilePictureUrl = user.ProfilePictureUrl
            };

            return Ok(userDto);
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid registration data.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            if (await _userManager.FindByEmailAsync(request.Email) != null)
            {
                return Conflict(new { message = "The email address is already in use." });
            }

            if (await _userManager.FindByNameAsync(request.Username) != null)
            {
                return Conflict(new { message = "The username is already in use." });
            }

            var user = new ApplicationUser
            {
                UserName = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Age = request.Age,
                EmailConfirmed = false,
                Role = RoleName.User
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                return UnprocessableEntity(new { message = "User registration failed.", errors = result.Errors.Select(e => e.Description) });
            }

            // Assign default role
            await _userManager.AddToRoleAsync(user, user.Role.ToString());

            // Send confirmationLink to the user's email address
            var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action("ConfirmEmail", "Auth", new { userId = user.Id, token = HttpUtility.UrlEncode(confirmationToken) }, Request.Scheme);

            var subject = "Confirm your email";
            var message = $"<h3>Thank you for registering!</h3>" +
                $"Please confirm your email by clicking the link: <a href=\"{confirmationLink}\">Confirm Email</a>" +
                $"<p>If you didn't request this, please ignore this email.</p>";
            await _emailService.SendEmailAsync(user.Email, subject, message);

            return Ok(new { message = "User registered successfully. Please check your email to confirm your account." });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userManager.FindByNameAsync(request.Username);

            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                return Unauthorized(new { message = "Invalid username or password." });
            }

            if (!user.EmailConfirmed)
            {
                return Unauthorized(new { message = "Please confirm your email address." });
            }

            var token = _jwtUtil.GenerateJwtToken(user);

            return Ok(new { token, message = "Login successful." });
        }

        [HttpGet("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return Redirect($"http://localhost:5173/login?message={Uri.EscapeDataString("Invalid confirmation parameters.")}");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Redirect($"http://localhost:5173/login?message={Uri.EscapeDataString("User not found.")}");
            }

            if (user.EmailConfirmed)
            {
                // Redirect to frontend login page with a message
                return Redirect($"http://localhost:5173/login?message={Uri.EscapeDataString("Your email is already confirmed. Please log in.")}");
            }

            var decodedToken = HttpUtility.UrlDecode(token);

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            if (result.Succeeded)
            {
                return Redirect($"http://localhost:5173/login?message={Uri.EscapeDataString("Email confirmed successfully.")}");
            }

            return Redirect($"http://localhost:5173/login?message={Uri.EscapeDataString("Email confirmation failed. Please try again.")}");
        }
    }
}
