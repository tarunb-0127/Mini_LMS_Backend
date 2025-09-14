using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace Mini_LMS.Models;

public partial class MiniLMSContext : DbContext
{
    public MiniLMSContext()
    {
    }

    public MiniLMSContext(DbContextOptions<MiniLMSContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<CourseApproval> CourseApprovals { get; set; }

    public virtual DbSet<CourseTakedownRequest> CourseTakedownRequests { get; set; }

    public virtual DbSet<Efmigrationshistory> Efmigrationshistories { get; set; }

    public virtual DbSet<Emailotp> Emailotps { get; set; }

    public virtual DbSet<Enrollment> Enrollments { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<Module> Modules { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Passwordreset> Passwordresets { get; set; }

    public virtual DbSet<Passwordtoken> Passwordtokens { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=localhost;port=3306;database=minilmsdb;user=root;password=admin", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.40-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("courses");

            entity.HasIndex(e => e.TrainerId, "TrainerId");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.IsApproved).HasDefaultValueSql("'0'");
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Type).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.Visibility)
                .HasDefaultValueSql("'Hidden'")
                .HasColumnType("enum('Public','Hidden')");

            entity.HasOne(d => d.Trainer).WithMany(p => p.Courses)
                .HasForeignKey(d => d.TrainerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("courses_ibfk_1");
        });

        modelBuilder.Entity<CourseApproval>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("course_approvals");

            entity.HasIndex(e => e.CourseId, "CourseId");

            entity.HasIndex(e => e.RequestedBy, "RequestedBy");

            entity.Property(e => e.RequestedAt).HasColumnType("datetime");
            entity.Property(e => e.RespondedAt).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Pending'")
                .HasColumnType("enum('Pending','Approved','Rejected')");

            entity.HasOne(d => d.Course).WithMany(p => p.CourseApprovals)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("course_approvals_ibfk_1");

            entity.HasOne(d => d.RequestedByNavigation).WithMany(p => p.CourseApprovals)
                .HasForeignKey(d => d.RequestedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("course_approvals_ibfk_2");
        });

        modelBuilder.Entity<CourseTakedownRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("course_takedown_requests");

            entity.HasIndex(e => e.CourseId, "CourseId1");

            entity.HasIndex(e => e.RequestedBy, "RequestedBy1");

            entity.Property(e => e.Reason).HasColumnType("text");
            entity.Property(e => e.RequestedAt).HasColumnType("datetime");
            entity.Property(e => e.RespondedAt).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Pending'")
                .HasColumnType("enum('Pending','Removed','Rejected')");

            entity.HasOne(d => d.Course).WithMany(p => p.CourseTakedownRequests)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("course_takedown_requests_ibfk_1");

            entity.HasOne(d => d.RequestedByNavigation).WithMany(p => p.CourseTakedownRequests)
                .HasForeignKey(d => d.RequestedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("course_takedown_requests_ibfk_2");
        });

        modelBuilder.Entity<Efmigrationshistory>(entity =>
        {
            entity.HasKey(e => e.MigrationId).HasName("PRIMARY");

            entity.ToTable("__efmigrationshistory");

            entity.Property(e => e.MigrationId).HasMaxLength(150);
            entity.Property(e => e.ProductVersion).HasMaxLength(32);
        });

        modelBuilder.Entity<Emailotp>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("emailotps");

            entity.HasIndex(e => e.Email, "IX_emailotps_Email").IsUnique();

            entity.Property(e => e.ExpiryTime).HasColumnType("datetime");
            entity.Property(e => e.OtpCode).HasMaxLength(10);
            entity.Property(e => e.SentAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("enrollments");

            entity.HasIndex(e => e.CourseId, "fk_enroll_course");

            entity.HasIndex(e => new { e.LearnerId, e.CourseId }, "uq_learner_course").IsUnique();

            entity.Property(e => e.EnrolledAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Active'")
                .HasColumnType("enum('Active','Completed','Dropped')");

            entity.HasOne(d => d.Course).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.CourseId)
                .HasConstraintName("fk_enroll_course");

            entity.HasOne(d => d.Learner).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.LearnerId)
                .HasConstraintName("fk_enroll_user");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("feedbacks");

            entity.HasIndex(e => e.CourseId, "CourseId2");

            entity.HasIndex(e => e.LearnerId, "LearnerId");

            entity.HasIndex(e => e.ModuleId, "ModuleId");

            entity.Property(e => e.Message).HasColumnType("text");
            entity.Property(e => e.SubmittedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Course).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("feedbacks_ibfk_2");

            entity.HasOne(d => d.Learner).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.LearnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("feedbacks_ibfk_1");

            entity.HasOne(d => d.Module).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.ModuleId)
                .HasConstraintName("feedbacks_ibfk_3");
        });

        modelBuilder.Entity<Module>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("modules");

            entity.HasIndex(e => e.CourseId, "CourseId3");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.Difficulty).HasMaxLength(50);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Course).WithMany(p => p.Modules)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("modules_ibfk_1");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("notifications");

            entity.HasIndex(e => e.UserId, "UserId");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.IsRead).HasDefaultValueSql("'0'");
            entity.Property(e => e.Message).HasColumnType("text");
            entity.Property(e => e.Type).HasMaxLength(100);

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("notifications_ibfk_1");
        });

        modelBuilder.Entity<Passwordreset>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("passwordresets");

            entity.HasIndex(e => e.UserId, "UserId1");

            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.ExpiryTime).HasColumnType("datetime");
            entity.Property(e => e.SentAt).HasColumnType("datetime");
            entity.Property(e => e.Token).HasMaxLength(255);

            entity.HasOne(d => d.User).WithMany(p => p.Passwordresets)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("passwordresets_ibfk_1");
        });

        modelBuilder.Entity<Passwordtoken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("passwordtokens");

            entity.HasIndex(e => e.UserId, "UserId2");

            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.ExpiryTime).HasColumnType("datetime");
            entity.Property(e => e.SentAt).HasColumnType("datetime");
            entity.Property(e => e.Token).HasMaxLength(255);

            entity.HasOne(d => d.User).WithMany(p => p.Passwordtokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("passwordtokens_ibfk_1");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "email").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasColumnName("role");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .HasColumnName("username");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
