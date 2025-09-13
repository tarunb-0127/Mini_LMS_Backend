using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mini_LMS.Models;
using Mini_LMS.Services;

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

        public class ModuleCreateDto
        {
            public int CourseId { get; set; }
            public string Name { get; set; } = null!;
            public string? Difficulty { get; set; }
            public string? Description { get; set; }
            public IFormFile? File { get; set; }
        }

        public class ModuleUpdateDto
        {
            public string Name { get; set; } = null!;
            public string? Difficulty { get; set; }
            public string? Description { get; set; }
            public IFormFile? File { get; set; }
        }

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

        // 🔐 Only Trainers can create modules
        [Authorize(Roles = "Trainer")]
        [HttpPost("create")]
        public async Task<IActionResult> CreateModule([FromForm] ModuleCreateDto dto)
        {
            var course = await _db.Courses.FindAsync(dto.CourseId);
            if (course == null)
                return NotFound($"Course with Id {dto.CourseId} not found.");

            var module = new Module
            {
                CourseId = dto.CourseId,
                Name = dto.Name,
                Difficulty = dto.Difficulty,
                Description = dto.Description,
                FilePath = dto.File != null ? await SaveFileAsync(dto.File) : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Modules.Add(module);
            await _db.SaveChangesAsync();

            // Notify trainer
            var trainer = await _db.Users.FindAsync(course.TrainerId);
            if (trainer != null)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = trainer.Id,
                    Type = "ModuleCreated",
                    Message = $"A new module '{module.Name}' was added to your course '{course.Name}'.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _email.SendCourseUpdateEmailAsync(trainer.Email, course.Name);
                await _db.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(GetModule), new { id = module.Id }, module);
        }

        // 🔐 Only Trainers can update modules
        [Authorize(Roles = "Trainer")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateModule(int id, [FromForm] ModuleUpdateDto dto)
        {
            var module = await _db.Modules.FindAsync(id);
            if (module == null)
                return NotFound();

            module.Name = dto.Name;
            module.Difficulty = dto.Difficulty;
            module.Description = dto.Description;

            if (dto.File != null)
                module.FilePath = await SaveFileAsync(dto.File);

            module.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Notify trainer
            var course = await _db.Courses.FindAsync(module.CourseId);
            var trainer = await _db.Users.FindAsync(course?.TrainerId);
            if (trainer != null)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = trainer.Id,
                    Type = "ModuleUpdated",
                    Message = $"Module '{module.Name}' in course '{course?.Name}' was updated.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _email.SendCourseUpdateEmailAsync(trainer.Email, course?.Name ?? "your course");
                await _db.SaveChangesAsync();
            }

            return Ok(module);
        }

        // 🔐 Only Trainers can delete modules
        [Authorize(Roles = "Trainer")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteModule(int id)
        {
            var module = await _db.Modules.FindAsync(id);
            if (module == null)
                return NotFound();

            var course = await _db.Courses.FindAsync(module.CourseId);
            var trainer = await _db.Users.FindAsync(course?.TrainerId);

            _db.Modules.Remove(module);
            await _db.SaveChangesAsync();

            // Notify trainer
            if (trainer != null)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = trainer.Id,
                    Type = "ModuleDeleted",
                    Message = $"Module '{module.Name}' was removed from course '{course?.Name}'.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _email.SendCourseUpdateEmailAsync(trainer.Email, course?.Name ?? "your course");
                await _db.SaveChangesAsync();
            }

            return NoContent();
        }

        // 👁️ Any authenticated user can view a module
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetModule(int id)
        {
            var module = await _db.Modules
                .Include(m => m.Course)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (module == null)
                return NotFound();

            return Ok(module);
        }

        // 👁️ Any authenticated user can view all modules
        [Authorize]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllModules()
        {
            var modules = await _db.Modules
                .Include(m => m.Course)
                .ToListAsync();

            return Ok(modules);
        }

        // 👁️ Any authenticated user can view modules by course
        [Authorize]
        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetModulesByCourse(int courseId)
        {
            var modules = await _db.Modules
                .Where(m => m.CourseId == courseId)
                .ToListAsync();

            if (!modules.Any())
                return NotFound($"No modules found for course {courseId}");

            return Ok(modules);
        }
    }
}
