using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mini_LMS.Models;

namespace Mini_LMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly MiniLMSContext _db;

        public NotificationsController(MiniLMSContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = long.Parse(User.FindFirst("UserId")!.Value);
            var notes = await _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
            return Ok(notes);
        }

        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = long.Parse(User.FindFirst("UserId")!.Value);
            var note = await _db.Notifications.FindAsync(id);
            if (note == null || note.UserId != userId)
                return NotFound(new { message = "Notification not found" });

            note.IsRead = true;
            await _db.SaveChangesAsync();
            return Ok(note);
        }
    }
}
