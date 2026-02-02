using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BonddyPlatform.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Repositories.Persistences
{
    public class BonddyDbContext : DbContext
    {
        public BonddyDbContext(DbContextOptions<BonddyDbContext> options) : base(options) { }

        public DbSet<Contact> Contacts => Set<Contact>();
        public DbSet<User> Users => Set<User>();
        public DbSet<OtpVerification> OtpVerifications => Set<OtpVerification>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Contact>(entity =>
            {
                entity.ToTable("Contacts");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Gmail).HasMaxLength(255).IsRequired();
                entity.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();

                entity.HasIndex(x => x.Gmail).IsUnique();        // unique email
                entity.HasIndex(x => x.PhoneNumber).IsUnique();  // unique phone
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Email).HasMaxLength(255).IsRequired();
                entity.Property(x => x.Password).HasMaxLength(500).IsRequired();    
                entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Gender).IsRequired();
                entity.Property(x => x.PhoneNumber).HasMaxLength(20);
                entity.Property(x => x.Address).HasMaxLength(255);
                entity.Property(x => x.DateOfBirth).HasColumnType("date");
                entity.Property(x => x.aboutMe).HasMaxLength(500);
                entity.Property(x => x.ProfilePicture).HasMaxLength(255);
                entity.Property(x => x.IsEmailVerified).IsRequired();
                entity.HasIndex(x => x.Email).IsUnique();
            });

            modelBuilder.Entity<OtpVerification>(entity =>
            {
                entity.ToTable("OtpVerifications");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Email).HasMaxLength(255).IsRequired();
                entity.Property(x => x.OtpCode).HasMaxLength(10).IsRequired();
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("RefreshTokens");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Token).HasMaxLength(500).IsRequired();
                entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
