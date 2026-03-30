using HospitalManagement.Domain.Models;
using HospitalManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace HospitalManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDBContext _context;

        public AuthController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<IdentityUser> signInManager,
            IConfiguration configuration,
            ApplicationDBContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _context = context;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                    return BadRequest(new { message = "Email and password are required" });

                var user = await _userManager.FindByEmailAsync(dto.Email);
                if (user == null)
                    return Unauthorized(new { message = "Invalid email or password" });

                var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
                if (!result.Succeeded)
                    return Unauthorized(new { message = "Invalid email or password" });

                var token = await GenerateJwtTokenAsync(user);

                return Ok(new LoginResponseDto
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Token = token,
                    Message = "Login successful"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        } 

        [HttpPost("register/patient")]
        [AllowAnonymous]
        public async Task<ActionResult<RegisterResponseDto>> RegisterPatient([FromBody] RegisterPatientRequestDto dto)
        {
            try
            {
                if (await _userManager.FindByEmailAsync(dto.Email) != null)
                    return BadRequest(new { message = "Email already exists" });

                var user = new IdentityUser
                {
                    UserName = dto.Email,
                    Email = dto.Email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, dto.Password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors);
                    return BadRequest(new { message = $"Registration failed: {errors}" });
                }

                await _userManager.AddToRoleAsync(user, "Patient");
                 
                var patient = new Patient
                {
                    Id = Guid.NewGuid(),
                    PatientName = dto.PatientName,
                    PatientContact = dto.PatientContact,
                    DateOfBirth = dto.DateOfBirth,
                    Gender = dto.Gender,
                    Allergies = dto.Allergies,
                    PatientAddress = dto.PatientAddress,
                    Bloodgroup = dto.Bloodgroup,
                    Age = CalculateAge(dto.DateOfBirth),
                    CreatedAt = DateTime.Now,
                    UserId = user.Id,
                    Email = dto.Email
                };

                try
                {
                    _context.Patients.Add(patient);
                    await _context.SaveChangesAsync();
                }
                catch (Exception dbEx)
                {
                    // rollback when user creation
                    await _userManager.DeleteAsync(user);
                    return BadRequest(new { message = "Failed to create patient record: " + dbEx.Message });
                }

                return Ok(new RegisterResponseDto
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Role = "Patient",
                    Message = "Patient registered successfully. You can now login."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("register/doctor")]
        [AllowAnonymous]
        public async Task<ActionResult<RegisterResponseDto>> RegisterDoctor([FromBody] RegisterDoctorRequestDto dto)
        {
            try
            {
                if (await _userManager.FindByEmailAsync(dto.Email) != null)
                    return BadRequest(new { message = "Email already exists" });

                var user = new IdentityUser
                {
                    UserName = dto.Email,
                    Email = dto.Email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, dto.Password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors);
                    return BadRequest(new { message = $"Registration failed: {errors}" });
                }

                await _userManager.AddToRoleAsync(user, "Doctor");
                 
                var doctor = new Doctor
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    Specialty = dto.Specialty,
                    CreatedAt = DateTime.Now,
                    UserId = user.Id,
                    Email = dto.Email
                };

                try
                {
                    _context.Doctors.Add(doctor);
                    await _context.SaveChangesAsync();
                }
                catch (Exception dbEx)
                {
                    // rollback while user creation
                    await _userManager.DeleteAsync(user);
                    return BadRequest(new { message = "Failed to create doctor record: " + dbEx.Message });
                }

                return Ok(new RegisterResponseDto
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Role = "Doctor",
                    Message = "Doctor registered successfully. You can now login."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Helper method for generating the JWT token
        private async Task<string> GenerateJwtTokenAsync(IdentityUser user)
        {
            var secret = _configuration["Jwt:Key"] ?? _configuration["Jwt:SecretKey"] ?? "your-secret-key";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var userRoles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
            };

            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var issuer = _configuration["Jwt:Issuer"] ?? "HospitalManagementAPI";
            var audience = _configuration["Jwt:Audience"] ?? "HospitalManagementClient";
             
            int expiryMinutes = 1440;
            if (int.TryParse(_configuration["Jwt:ExpiryInMinutes"], out var m))
            {
                expiryMinutes = m;
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.Today;
            int age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }
    }

    // DTOs models
    public class LoginRequestDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginResponseDto
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
        public string Message { get; set; }
    }

    public class RegisterPatientRequestDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string PatientName { get; set; }
        public string PatientContact { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string Allergies { get; set; }
        public string PatientAddress { get; set; }
        public string Bloodgroup { get; set; }
    }

    public class RegisterDoctorRequestDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public string Specialty { get; set; }
    }

    public class RegisterResponseDto
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Message { get; set; }
    }
}
