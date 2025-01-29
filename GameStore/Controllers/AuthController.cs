using GameStore.Models.DTO;
using GameStore.Models.Entities;
using GameStore.Models.Enums;
using GameStore.Services;
using GameStore.Utils;
using Microsoft.AspNetCore.Authentication;
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
        public async Task<IActionResult> Register(RegisterUserRequest request)
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
        [AllowAnonymous]
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

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] Dictionary<string, string> data)
        {
            if (!data.TryGetValue("email", out var email) || string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("Email is required.");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound(new { message = "Provided email is not valid." });
            }
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var resetLink = Url.Action("ResetPassword", "Auth", new { userId = user.Id, token = HttpUtility.UrlEncode(token) }, Request.Scheme);
            var subject = "Reset your password";
            var message = $"<h3>Reset your password</h3>" +
                "<p>Hello <strong>" + user.FirstName + "</strong>,</p>" +
                "<p>We received a request to reset the password for your account. " +
                "If you made this request, please click the button below to reset your password:</p>" +
                "<a href=\"" + resetLink + "\">Reset My Password</a>" +
                "<p>If the button above doesn’t work, copy and paste the following link into your browser:</p>" +
                "<p>" + resetLink + "</p>" +
                "<p>This link is valid for <strong>24 hours</strong>.</p>" +
                "<p><strong>If you did not request a password reset</strong>, no action is required. " +
                "Your account is still secure, and your password has not been changed. " +
                "If you suspect any suspicious activity, please contact our support team immediately.</p>" +
                "<p>Thank you,</p>" +
                "<p>The <strong>Game Store</strong> Support Team</p>" +
                "<div>" +
                "<p>This email is automatically generated. Please do not answer. If you need further assistance, " +
                "please contact us at <a href=\"mailto:support@yourwebsite.com\">support@yourwebsite.com</a>.</p>" +
                "</div>";

            await _emailService.SendEmailAsync(user.Email, subject, message);

            return Ok(new { message = "An email has been sent to you with instructions how to reset your password." });
        }

        [HttpGet("reset-password")]
        [AllowAnonymous]
        public IActionResult ResetPassword(string userId, string token)
        {
            return Redirect($"http://localhost:5173/reset-password?userId={userId}&token={HttpUtility.UrlEncode(token)}");
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(string userId, string token, string password)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }
            
            var result = await _userManager.ResetPasswordAsync(user, token, password);
            if (result.Succeeded)
            {
                return Ok(new { message = "Password reset successful." });
            }
            return BadRequest(new { message = "Password reset failed.", errors = result.Errors.Select(e => e.Description) });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return Ok(new { message = "Logout successful." });
        }
    }
}
