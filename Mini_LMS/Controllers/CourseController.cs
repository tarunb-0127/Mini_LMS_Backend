using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mini_LMS.Models;
using Mini_LMS.Services;

namespace Mini_LMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourseController : ControllerBase
    {
        private readonly MiniLMSContext _db;
        private readonly EmailService _email;

        public CourseController(MiniLMSContext db, EmailService email)
        {
            _db = db;
            _email = email;
        }

        // DTOs
        public class CourseCreateDTO
        {
            public string Name { get; set; } = null!;
            public string? Type { get; set; }
            public int? Duration { get; set; }
            public string? Visibility { get; set; }
        }

        public class TakedownRequestDTO
        {
            public int CourseId { get; set; }
            public string Reason { get; set; } = null!;
        }

        // 🔐 Only Trainers can create courses
        [Authorize(Roles = "Trainer")]
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CourseCreateDTO dto)
        {
            // Get trainer email from JWT claims
            var trainerEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
            if (string.IsNullOrEmpty(trainerEmail))
                return Unauthorized(new { message = "Trainer not found or not authorized." });

            // Fetch trainer from DB
            var trainer = await _db.Users.SingleOrDefaultAsync(u => u.Email == trainerEmail && u.Role == "Trainer");
            if (trainer == null)
                return Unauthorized(new { message = "Trainer not found or not authorized." });

            // Create course
            var course = new Course
            {
                TrainerId = trainer.Id,
                Name = dto.Name,
                Type = dto.Type,
                Duration = dto.Duration,
                Visibility = dto.Visibility ?? "Public",
                IsApproved = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Courses.Add(course);
            await _db.SaveChangesAsync();

            // Notify learners
            var learners = await _db.Users.Where(u => u.Role == "Learner" && u.IsActive == true).ToListAsync();
            foreach (var learner in learners)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = learner.Id,
                    Type = "CourseCreated",
                    Message = $"New course '{course.Name}' is now available.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _email.SendNewCourseAvailableEmailAsync(learner.Email, course.Name);
            }

            await _db.SaveChangesAsync();
            return Ok(course);
        }

        // 👁️ Any authenticated user can view all courses
        [Authorize]
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var courses = await _db.Courses.Include(c => c.Trainer).ToListAsync();
            return Ok(courses);
        }

        // 🔐 Trainers can request course takedown
        [Authorize(Roles = "Trainer")]
        [HttpPost("request-takedown")]
        public async Task<IActionResult> RequestTakedown([FromBody] TakedownRequestDTO dto)
        {
            var trainerEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
            if (string.IsNullOrEmpty(trainerEmail))
                return Unauthorized(new { message = "Trainer not found or not authorized." });

            var trainer = await _db.Users.SingleOrDefaultAsync(u => u.Email == trainerEmail && u.Role == "Trainer");
            if (trainer == null)
                return Unauthorized(new { message = "Trainer not found or not authorized." });

            var course = await _db.Courses.FindAsync(dto.CourseId);
            if (course == null)
                return NotFound(new { message = "Course not found." });

            var admins = await _db.Users.Where(u => u.Role == "Admin").ToListAsync();
            foreach (var admin in admins)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    Type = "TakedownRequested",
                    Message = $"Trainer '{trainer.Email}' requested takedown of course '{course.Name}'. Reason: {dto.Reason}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _email.SendCourseUpdateEmailAsync(admin.Email, $"Takedown Request: {course.Name}\nReason: {dto.Reason}");
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = "Takedown request submitted successfully." });
        }

        // 👁️ Any authenticated user can view a course
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var course = await _db.Courses.Include(c => c.Trainer).FirstOrDefaultAsync(c => c.Id == id);
            if (course == null) return NotFound();
            return Ok(course);
        }

        [Authorize(Roles = "Trainer")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CourseCreateDTO dto)
        {
            // 1) grab trainer email from JWT
            var email = User.FindFirst(ClaimTypes.Email)?.Value
                        ?? User.FindFirst("email")?.Value;
            if (email == null)
                return Unauthorized(new { message = "Not authorized." });

            // 2) fetch the course and ensure this trainer owns it
            var course = await _db.Courses
                .Include(c => c.Trainer)
                .SingleOrDefaultAsync(c => c.Id == id && c.Trainer.Email == email);

            if (course == null)
                return NotFound(new { message = "Course not found or you’re not its trainer." });

            // 3) apply updates
            course.Name = dto.Name;
            course.Type = dto.Type;
            course.Duration = dto.Duration;
            course.Visibility = dto.Visibility ?? course.Visibility;
            course.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(course);
        }

    }
}
