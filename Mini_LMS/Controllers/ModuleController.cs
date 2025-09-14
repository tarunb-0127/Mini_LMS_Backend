using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mini_LMS.Models;
using Mini_LMS.Services;
using System.Security.Claims;

namespace Mini_LMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModuleController : ControllerBase
    {
        private readonly MiniLMSContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly EmailService _email;

        public ModuleController(MiniLMSContext db, IWebHostEnvironment env, EmailService email)
        {
            _db = db;
            _env = env;
            _email = email;
        }

        // DTOs
        public class ModuleCreateDto
        {
            public int CourseId { get; set; }
            public string Title { get; set; } = null!;
            public string? Content { get; set; }
            public IFormFile? File { get; set; }
        }

        public class ModuleUpdateDto
        {
            public string Title { get; set; } = null!;
            public string? Content { get; set; }
            public IFormFile? File { get; set; }
        }

        // Helper: Save uploaded file
        private async Task<string?> SaveFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return null;

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsFolder = Path.Combine(webRoot, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/{fileName}";
        }

        // Helper: Get current trainer
        private async Task<User?> GetTrainerAsync()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (email == null) return null;
            return await _db.Users.SingleOrDefaultAsync(u => u.Email == email && u.Role == "Trainer");
        }

        // 🔐 Create Module (Trainer only)
        [Authorize(Roles = "Trainer")]
        [HttpPost("create")]
        public async Task<IActionResult> CreateModule([FromForm] ModuleCreateDto dto)
        {
            var trainer = await GetTrainerAsync();
            if (trainer == null) return Unauthorized("Trainer not authorized");

            var course = await _db.Courses.FindAsync(dto.CourseId);
            if (course == null) return NotFound("Course not found");
            if (course.TrainerId != trainer.Id)
                return Unauthorized("You can only add modules to your own courses");

            var module = new Module
            {
                CourseId = dto.CourseId,
                Name = dto.Title,
                Description = dto.Content,
                FilePath = await SaveFileAsync(dto.File),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Modules.Add(module);
            await _db.SaveChangesAsync();

            // Notify trainer
            await NotifyTrainerAsync(trainer, $"Module '{module.Name}' added to course '{course.Name}'.");

            return CreatedAtAction(nameof(GetModule), new { id = module.Id }, module);
        }

        // 🔐 Update Module
        [Authorize(Roles = "Trainer")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateModule(int id, [FromForm] ModuleUpdateDto dto)
        {
            var trainer = await GetTrainerAsync();
            if (trainer == null) return Unauthorized("Trainer not authorized");

            var module = await _db.Modules.FindAsync(id);
            if (module == null) return NotFound("Module not found");

            var course = await _db.Courses.FindAsync(module.CourseId);
            if (course == null || course.TrainerId != trainer.Id)
                return Unauthorized("You can only update modules in your own courses");

            module.Name = dto.Title;
            module.Description = dto.Content;
            if (dto.File != null) module.FilePath = await SaveFileAsync(dto.File);
            module.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await NotifyTrainerAsync(trainer, $"Module '{module.Name}' updated in course '{course.Name}'.");

            return Ok(module);
        }

        // 🔐 Delete Module
        [Authorize(Roles = "Trainer")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteModule(int id)
        {
            var trainer = await GetTrainerAsync();
            if (trainer == null) return Unauthorized("Trainer not authorized");

            var module = await _db.Modules.FindAsync(id);
            if (module == null) return NotFound("Module not found");

            var course = await _db.Courses.FindAsync(module.CourseId);
            if (course == null || course.TrainerId != trainer.Id)
                return Unauthorized("You can only delete modules in your own courses");

            _db.Modules.Remove(module);
            await _db.SaveChangesAsync();

            await NotifyTrainerAsync(trainer, $"Module '{module.Name}' deleted from course '{course.Name}'.");

            return NoContent();
        }

        // 👁️ Get single module
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetModule(int id)
        {
            var module = await _db.Modules
                .Include(m => m.Course)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (module == null) return NotFound();
            return Ok(module);
        }

        // 👁️ Get all modules for a course
        [Authorize]
        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetModulesByCourse(int courseId)
        {
            var modules = await _db.Modules
                .Where(m => m.CourseId == courseId)
                .ToListAsync();

            return Ok(modules); // empty array if none
        }

        // Helper: notify trainer via notification + email
        private async Task NotifyTrainerAsync(User trainer, string message)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = trainer.Id,
                Type = "ModuleUpdate",
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _email.SendCourseUpdateEmailAsync(trainer.Email, message);
            await _db.SaveChangesAsync();
        }
    }
}
