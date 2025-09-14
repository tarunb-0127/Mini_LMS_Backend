﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mini_LMS.Models;
using System.Security.Claims;

namespace MiniLMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnrollmentController : ControllerBase
    {
        private readonly MiniLMSContext _db;

        public EnrollmentController(MiniLMSContext db)
        {
            _db = db;
        }

        // ✅ Enroll learner into a course
        [HttpPost("enroll/{courseId}")]
        [Authorize(Roles = "Learner")]
        public async Task<IActionResult> EnrollInCourse(int courseId)
        {
            var learnerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // prevent duplicate enrollment
            if (await _db.Enrollments.AnyAsync(e => e.LearnerId == learnerId && e.CourseId == courseId))
                return BadRequest("You are already enrolled in this course.");

            var enrollment = new Enrollment
            {
                LearnerId = learnerId,
                CourseId = courseId,
                EnrolledAt = DateTime.UtcNow,
                Status = "Active"
            };

            _db.Enrollments.Add(enrollment);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Enrolled successfully", enrollment });
        }

        // ✅ Get all courses for the logged-in learner
        [HttpGet("my-courses")]
        [Authorize(Roles = "Learner")]
        public async Task<IActionResult> GetMyCourses()
        {
            var learnerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var courses = await _db.Enrollments
                .Where(e => e.LearnerId == learnerId)
                .Include(e => e.Course)
                .ThenInclude(c => c.Trainer)
                .Select(e => new {
                    e.Course.Id,
                    e.Course.Name,
                    e.Course.Duration,
                    Trainer = e.Course.Trainer.Username,
                    e.Status,
                    e.EnrolledAt
                })
                .ToListAsync();

            return Ok(courses);
        }

        // ✅ Get all learners enrolled in a course (Trainer/Admin)
        [HttpGet("course/{courseId}")]
        [Authorize(Roles = "Trainer,Admin")]
        public async Task<IActionResult> GetLearnersForCourse(int courseId)
        {
            var learners = await _db.Enrollments
                .Where(e => e.CourseId == courseId)
                .Include(e => e.Learner)
                .Select(e => new {
                    e.Learner.Id,
                    e.Learner.Username,
                    e.Learner.Email,
                    e.Status,
                    e.EnrolledAt
                })
                .ToListAsync();

            return Ok(learners);
        }

        // ✅ Drop enrollment (Learner or Admin)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Learner,Admin")]
        public async Task<IActionResult> DropEnrollment(int id)
        {
            var enrollment = await _db.Enrollments.FindAsync(id);
            if (enrollment == null) return NotFound();

            // If learner is dropping, ensure it's their own enrollment
            var learnerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (User.IsInRole("Learner") && enrollment.LearnerId != learnerId)
                return Forbid();

            _db.Enrollments.Remove(enrollment);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Enrollment removed successfully" });
        }
    }
}
