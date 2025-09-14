namespace Mini_LMS.DTOs
{
    public class AdminLoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class RegisterDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }

    public class LoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }

    public class PasswordResetRequestDto
    {
        public int UserId { get; set; }
    }

    public class PasswordResetDto
    {
        public string Email { get; set; }
        public string Token { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }

    public class CourseCreateDTO
    {
        public int TrainerId { get; set; }
        public string Name { get; set; }
        public string? Type { get; set; }
        public int? Duration { get; set; }
        public string? Visibility { get; set; }
    }


}
