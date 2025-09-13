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

        // 🔐 Only Trainers can create courses
        [Authorize(Roles = "Trainer")]
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromForm] Course course)
        {
            course.CreatedAt = DateTime.UtcNow;
            course.UpdatedAt = DateTime.UtcNow;

            _db.Courses.Add(course);
            await _db.SaveChangesAsync();

            // Notify learners
            var learners = await _db.Users
                .Where(u => u.Role == "Learner" && u.IsActive == true)
                .ToListAsync();

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

        // 🔐 Only Trainers can update courses
        [Authorize(Roles = "Trainer")]
        [HttpPut("edit/{id}")]
        public async Task<IActionResult> Edit(int id, [FromForm] Course updated)
        {
            var course = await _db.Courses.FindAsync(id);
            if (course == null)
                return NotFound();

            course.Name = updated.Name;
            course.Type = updated.Type;
            course.Duration = updated.Duration;
            course.Visibility = updated.Visibility;
            course.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Notify learners
            var learners = await _db.Users
                .Where(u => u.Role == "Learner" && u.IsActive == true)
                .ToListAsync();

            foreach (var learner in learners)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = learner.Id,
                    Type = "CourseUpdated",
                    Message = $"Course '{course.Name}' has been updated.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _email.SendCourseUpdateEmailAsync(learner.Email, course.Name);
            }

            await _db.SaveChangesAsync();
            return Ok(course);
        }

        // 👁️ Any authenticated user can view all courses
        [Authorize]
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var courses = await _db.Courses
                .Include(c => c.Trainer)
                .ToListAsync();
            return Ok(courses);
        }

        // 👁️ Any authenticated user can view a course
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var course = await _db.Courses
                .Include(c => c.Trainer)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (course == null)
                return NotFound();
            return Ok(course);
        }
    }
}
