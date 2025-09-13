using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mini_LMS.Models;

namespace Mini_LMS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly MiniLMSContext _db;

        public UsersController(MiniLMSContext db)
        {
            _db = db;
        }

        // ✅ Get all users
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _db.Users.ToListAsync();
            return Ok(users);
        }

        // ✅ Get single user by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(user);
        }

        // ✅ Update user (email, role, status)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromForm] string email, [FromForm] string role, [FromForm] bool is_active)
        {
            var existingUser = await _db.Users.FindAsync(id);
            if (existingUser == null)
                return NotFound(new { message = "User not found" });

            existingUser.Email = email;
            existingUser.Role = role;
            existingUser.IsActive = is_active;

            await _db.SaveChangesAsync();

            return Ok(new { message = "User updated successfully", user = existingUser });
        }

        // ✅ Delete user
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            return Ok(new { message = "User deleted successfully" });
        }

        // ✅ Toggle active/inactive
        [HttpPatch("{id}/toggle")]
        public async Task<IActionResult> ToggleUser(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            user.IsActive = !user.IsActive;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = $"User status updated to {((bool)user.IsActive ? "Active" : "Inactive")}",
                user
            });
        }
    }
}
