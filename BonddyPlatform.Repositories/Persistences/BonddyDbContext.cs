using BonddyPlatform.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BonddyPlatform.Repositories.Persistences
{
    public class BonddyDbContext : DbContext
    {
        public BonddyDbContext(DbContextOptions<BonddyDbContext> options) : base(options) { }

        public DbSet<Contact> Contacts => Set<Contact>();
        public DbSet<User> Users => Set<User>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<OtpVerification> OtpVerifications => Set<OtpVerification>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<Trip> Trips => Set<Trip>();
        public DbSet<TripRequest> TripRequests => Set<TripRequest>();
        public DbSet<Buddy> Buddies => Set<Buddy>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Incident> Incidents => Set<Incident>();
        public DbSet<ChatRoom> ChatRooms => Set<ChatRoom>();
        public DbSet<ChatRoomParticipant> ChatRoomParticipants => Set<ChatRoomParticipant>();
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<MessageAttachment> MessageAttachments => Set<MessageAttachment>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<ChatRoomUserState> ChatRoomUserStates => Set<ChatRoomUserState>();
        public DbSet<ServicePackage> ServicePackages => Set<ServicePackage>();
        public DbSet<BuddySubscription> BuddySubscriptions => Set<BuddySubscription>();

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

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("UserRoles");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Role).IsRequired();
                entity.Property(x => x.AssignedAt).IsRequired();

                entity.HasOne(x => x.User)
                    .WithMany(u => u.Roles)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // One user cannot have the same role twice
                entity.HasIndex(x => new { x.UserId, x.Role }).IsUnique();
                entity.HasIndex(x => x.UserId);
            });

            modelBuilder.Entity<Trip>(entity =>
            {
                entity.ToTable("Trips");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.City).HasMaxLength(100).IsRequired();
                entity.Property(x => x.StartDate).HasColumnType("date").IsRequired();
                entity.Property(x => x.StartTime).HasColumnType("time").IsRequired();
                entity.Property(x => x.DurationHours).IsRequired();
                entity.Property(x => x.Adults).IsRequired();
                entity.Property(x => x.Children).IsRequired();
                entity.Property(x => x.Notes).HasMaxLength(1000);
                entity.Property(x => x.Status).IsRequired();
                entity.Property(x => x.CreatedAt).IsRequired();

                // List<string> Activities stored as JSONB
                entity.Property(x => x.Activities)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v ?? new List<string>(), (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
                    )
                    .HasColumnType("jsonb")
                    .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                        (a, b) => a != null && b != null && a.SequenceEqual(b),
                        v => v.Aggregate(0, (h, e) => HashCode.Combine(h, e.GetHashCode())),
                        v => v.ToList()));

                // List<string> PreferredLanguages stored as JSONB
                entity.Property(x => x.PreferredLanguages)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v ?? new List<string>(), (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
                    )
                    .HasColumnType("jsonb")
                    .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                        (a, b) => a != null && b != null && a.SequenceEqual(b),
                        v => v.Aggregate(0, (h, e) => HashCode.Combine(h, e.GetHashCode())),
                        v => v.ToList()));

                entity.HasOne(x => x.TravelerUser)
                    .WithMany()
                    .HasForeignKey(x => x.TravelerUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.City, x.StartDate });
                entity.HasIndex(x => new { x.TravelerUserId, x.CreatedAt });
                entity.HasIndex(x => x.Status);
            });

            modelBuilder.Entity<TripRequest>(entity =>
            {
                entity.ToTable("TripRequests");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.OfferedPrice).HasColumnType("decimal(18,2)");
                entity.Property(x => x.IsInboxOnly).IsRequired();
                entity.Property(x => x.Note).HasMaxLength(500);
                entity.Property(x => x.ExpiresAt).IsRequired();
                entity.Property(x => x.CreatedAt).IsRequired();

                entity.HasOne(x => x.Trip)
                    .WithMany()
                    .HasForeignKey(x => x.TripId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Buddy)
                    .WithMany()
                    .HasForeignKey(x => x.BuddyId)
                    .OnDelete(DeleteBehavior.Cascade);

                // One buddy can only send one request per trip
                entity.HasIndex(x => new { x.TripId, x.BuddyId }).IsUnique();
                entity.HasIndex(x => x.TripId);
                entity.HasIndex(x => x.BuddyId);
                entity.HasIndex(x => x.ExpiresAt);
            });

            modelBuilder.Entity<Buddy>(entity =>
            {
                entity.ToTable("Buddies");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.CostPerHour).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(x => x.Rate).HasColumnType("decimal(3,2)");
                entity.Property(x => x.Bio).HasMaxLength(300);
                entity.Property(x => x.IsActive).IsRequired();
                entity.Property(x => x.CreatedAt).IsRequired();

                entity.Property(x => x.Activities)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
                    )
                    .HasColumnType("jsonb")
                    .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                        (a, b) => a != null && b != null && a.SequenceEqual(b),
                        v => v.Aggregate(0, (h, e) => HashCode.Combine(h, e.GetHashCode())),
                        v => v.ToList()));

                entity.Property(x => x.Languages)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
                    )
                    .HasColumnType("jsonb")
                    .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                        (a, b) => a != null && b != null && a.SequenceEqual(b),
                        v => v.Aggregate(0, (h, e) => HashCode.Combine(h, e.GetHashCode())),
                        v => v.ToList()));

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => x.UserId).IsUnique();
                entity.HasIndex(x => x.IsActive);
            });



            modelBuilder.Entity<Booking>(entity =>
            {
                entity.ToTable("Bookings");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.BookedDate).HasColumnType("date").IsRequired();
                entity.Property(x => x.BookedStartTime).HasColumnType("time").IsRequired();
                entity.Property(x => x.Price).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(x => x.Currency).HasMaxLength(10).IsRequired();
                entity.Property(x => x.PlatformFeeAmount).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(x => x.Includes).HasMaxLength(2000);
                entity.Property(x => x.Excludes).HasMaxLength(2000);
                entity.Property(x => x.NoteForCustomer).HasMaxLength(500);
                entity.Property(x => x.Status).IsRequired();
                entity.Property(x => x.CreatedAt).IsRequired();

                entity.HasOne(x => x.Trip)
                    .WithMany()
                    .HasForeignKey(x => x.TripId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.TripRequest)
                    .WithMany()
                    .HasForeignKey(x => x.TripRequestId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Buddy)
                    .WithMany()
                    .HasForeignKey(x => x.BuddyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.TravelerUser)
                    .WithMany()
                    .HasForeignKey(x => x.TravelerUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.TripId);
                entity.HasIndex(x => x.TripRequestId).IsUnique();
                entity.HasIndex(x => new { x.TravelerUserId, x.Status });
                entity.HasIndex(x => new { x.BuddyId, x.Status });
                entity.HasIndex(x => x.Status);
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.ToTable("Payments");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(x => x.Method).IsRequired();
                entity.Property(x => x.Status).IsRequired();
                entity.Property(x => x.ExternalRef).HasMaxLength(200);
                entity.Property(x => x.Note).HasMaxLength(500);
                entity.Property(x => x.CreatedAt).IsRequired();

                // BookingId is now optional — null when payment is for a subscription
                entity.HasOne(x => x.Booking)
                    .WithOne(b => b.Payment)
                    .HasForeignKey<Payment>(x => x.BookingId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                // BuddySubscriptionId is optional — set when payment is for a subscription
                entity.HasOne(x => x.BuddySubscription)
                    .WithMany()
                    .HasForeignKey(x => x.BuddySubscriptionId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(x => x.BookingId).IsUnique(false);
                entity.HasIndex(x => x.BuddySubscriptionId);
                entity.HasIndex(x => x.Status);
            });

            modelBuilder.Entity<Review>(entity =>
            {
                entity.ToTable("Reviews");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Rating).IsRequired();
                entity.Property(x => x.Comment).HasMaxLength(1000);
                entity.Property(x => x.CreatedAt).IsRequired();

                entity.HasOne(x => x.Booking)
                    .WithMany()
                    .HasForeignKey(x => x.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.ReviewerUser)
                    .WithMany()
                    .HasForeignKey(x => x.ReviewerUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.BookingId).IsUnique();
                entity.HasIndex(x => new { x.ReviewerUserId, x.CreatedAt });
            });

            modelBuilder.Entity<Incident>(entity =>
            {
                entity.ToTable("Incidents");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Type).IsRequired();
                entity.Property(x => x.Description).HasMaxLength(2000).IsRequired();
                entity.Property(x => x.Status).IsRequired();
                entity.Property(x => x.Resolution).HasMaxLength(2000);
                entity.Property(x => x.CreatedAt).IsRequired();

                entity.HasOne(x => x.Booking)
                    .WithMany()
                    .HasForeignKey(x => x.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.ReportedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.ReportedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new { x.BookingId, x.Status });
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

            modelBuilder.Entity<ChatRoom>(entity =>
            {
                entity.ToTable("ChatRooms");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.RoomType)
                    .IsRequired();

                entity.Property(x => x.CreatedAt)
                    .IsRequired();

                entity.HasIndex(x => x.TripId);
                entity.HasIndex(x => new { x.RoomType, x.BuddyUserId });
                entity.HasIndex(x => x.TripRequestId);

                // Optional FK to TripRequest (only set for DirectBuddy rooms)
                entity.HasOne(x => x.TripRequest)
                    .WithMany()
                    .HasForeignKey(x => x.TripRequestId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ChatRoomParticipant>(entity =>
            {
                entity.ToTable("ChatRoomParticipants");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.RoleInRoom)
                    .IsRequired();

                entity.Property(x => x.JoinedAt)
                    .IsRequired();

                entity.HasOne(x => x.Room)
                    .WithMany(r => r.Participants)
                    .HasForeignKey(x => x.RoomId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.RoomId, x.UserId }).IsUnique();
                entity.HasIndex(x => x.UserId);
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.ToTable("Messages");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.ContentType)
                    .IsRequired();

                entity.Property(x => x.ContentText)
                    .HasMaxLength(4000);

                entity.Property(x => x.CreatedAt)
                    .IsRequired();

                entity.HasOne(x => x.Room)
                    .WithMany(r => r.Messages)
                    .HasForeignKey(x => x.RoomId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.SenderUser)
                    .WithMany()
                    .HasForeignKey(x => x.SenderUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.ReplyToMessage)
                    .WithMany()
                    .HasForeignKey(x => x.ReplyToMessageId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Booking>()
                    .WithMany()
                    .HasForeignKey(x => x.BookingId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(x => new { x.RoomId, x.CreatedAt });
            });

            modelBuilder.Entity<MessageAttachment>(entity =>
            {
                entity.ToTable("MessageAttachments");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.StoragePath)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(x => x.ContentType)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.CreatedAt)
                    .IsRequired();

                entity.HasOne(x => x.Message)
                    .WithMany(m => m.Attachments)
                    .HasForeignKey(x => x.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Type).IsRequired();
                entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Body).HasMaxLength(2000).IsRequired();
                entity.Property(x => x.DataJson);
                entity.Property(x => x.IsRead).IsRequired();
                entity.Property(x => x.CreatedAt).IsRequired();

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
                entity.HasIndex(x => new { x.UserId, x.CreatedAt });
            });

            modelBuilder.Entity<ChatRoomUserState>(entity =>
            {
                entity.ToTable("ChatRoomUserStates");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.LastReadAt).IsRequired();
                entity.Property(x => x.CreatedAt).IsRequired();
                entity.Property(x => x.UpdatedAt).IsRequired();

                entity.HasOne(x => x.Room)
                    .WithMany()
                    .HasForeignKey(x => x.RoomId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.RoomId, x.UserId }).IsUnique();
                entity.HasIndex(x => x.UserId);
            });

            modelBuilder.Entity<ServicePackage>(entity =>
            {
                entity.ToTable("ServicePackages");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Description).HasMaxLength(500);
                entity.Property(x => x.PricePerMonth).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(x => x.Currency).HasMaxLength(10).IsRequired();
                entity.Property(x => x.CommissionRate).HasColumnType("decimal(5,2)").IsRequired();
                entity.Property(x => x.HasChatAccess).IsRequired();
                entity.Property(x => x.HasSearchPriority).IsRequired();
                entity.Property(x => x.HasPrioritySupport).IsRequired();
                entity.Property(x => x.HasProductFeedback).IsRequired();
                entity.Property(x => x.SortOrder).IsRequired();
                entity.Property(x => x.IsActive).IsRequired();
                entity.Property(x => x.CreatedAt).IsRequired();

                entity.Property(x => x.Features)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v ?? new List<string>(), (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
                    )
                    .HasColumnType("jsonb")
                    .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                        (a, b) => a != null && b != null && a.SequenceEqual(b),
                        v => v.Aggregate(0, (h, e) => HashCode.Combine(h, e.GetHashCode())),
                        v => v.ToList()));

                entity.HasIndex(x => x.Name).IsUnique();
                entity.HasIndex(x => x.IsActive);
            });

            modelBuilder.Entity<BuddySubscription>(entity =>
            {
                entity.ToTable("BuddySubscriptions");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.StartDate).IsRequired();
                entity.Property(x => x.EndDate).IsRequired();
                entity.Property(x => x.Status).IsRequired();
                entity.Property(x => x.PaymentMethod).IsRequired();
                entity.Property(x => x.AmountPaid).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(x => x.Currency).HasMaxLength(10).IsRequired();
                entity.Property(x => x.ExternalPaymentRef).HasMaxLength(200);
                entity.Property(x => x.CreatedAt).IsRequired();

                entity.HasOne(x => x.Buddy)
                    .WithMany()
                    .HasForeignKey(x => x.BuddyId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.ServicePackage)
                    .WithMany()
                    .HasForeignKey(x => x.ServicePackageId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.DowngradeToPackage)
                    .WithMany()
                    .HasForeignKey(x => x.DowngradeToPackageId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(x => x.IsCancelledAtEndOfCycle).IsRequired().HasDefaultValue(false);

                entity.HasIndex(x => new { x.BuddyId, x.Status });
                entity.HasIndex(x => new { x.ServicePackageId, x.Status });
                entity.HasIndex(x => x.EndDate);
                entity.HasIndex(x => x.ExternalPaymentRef);
            });

            SeedDefaultData(modelBuilder);
        }

        /// <summary>
        /// Seeds a default admin account and pre-assigns all 4 roles as reference data.
        /// Credentials: admin@bonddy.com / Admin@123456
        /// </summary>
        private static void SeedDefaultData(ModelBuilder modelBuilder)
        {
            // Fixed GUIDs so re-running migrations doesn't create duplicates
            var adminUserId = new Guid("00000000-0000-0000-0000-000000000001");

            // Pre-computed bcrypt hash for "Admin@123456" — do NOT use BCrypt at design-time
            // because Repositories project doesn't reference BCrypt.Net-Next.
            const string adminPasswordHash = "$2a$11$4utbNwmPKUJywWue101YXe7l0StEL09.KPQdV04hADeCAswcd7QFq";

            modelBuilder.Entity<User>().HasData(new User
            {
                Id = adminUserId,
                Email = "admin@bonddy.com",
                Password = adminPasswordHash,
                FullName = "Bonddy Admin",
                Gender = Gender.Other,
                IsEmailVerified = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });

            // Seed all 4 roles for the admin account so every Role enum value
            // exists as at least one row — serves as reference data.
            var roleSeeds = new[]
            {
                new UserRole { Id = new Guid("00000000-0000-0000-0001-000000000001"), UserId = adminUserId, Role = Role.Admin,  AssignedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new UserRole { Id = new Guid("00000000-0000-0000-0001-000000000002"), UserId = adminUserId, Role = Role.Buddy,  AssignedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new UserRole { Id = new Guid("00000000-0000-0000-0001-000000000003"), UserId = adminUserId, Role = Role.CS,     AssignedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new UserRole { Id = new Guid("00000000-0000-0000-0001-000000000004"), UserId = adminUserId, Role = Role.User,   AssignedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            };
            modelBuilder.Entity<UserRole>().HasData(roleSeeds);

            // ── Seed Service Packages ────────────────────────────────────────
            var starterPackageId = new Guid("00000000-0000-0000-0002-000000000001");
            var proPackageId     = new Guid("00000000-0000-0000-0002-000000000002");
            var founderPackageId = new Guid("00000000-0000-0000-0002-000000000003");
            var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<ServicePackage>().HasData(
                new ServicePackage
                {
                    Id = starterPackageId,
                    Name = "Starter",
                    Description = "Dành cho buddy mới bắt đầu, muốn thử trải nghiệm nền tảng.",
                    PricePerMonth = 49000m,
                    Currency = "VND",
                    CommissionRate = 0.20m,
                    HasChatAccess = true,
                    HasSearchPriority = false,
                    HasPrioritySupport = false,
                    HasProductFeedback = false,
                    MaxSlots = null,
                    SortOrder = 1,
                    IsActive = true,
                    Features = new List<string>
                    {
                        "Profile xuất hiện trên nền tảng",
                        "Không giới hạn booking",
                        "Commission 20%",
                        "Chat với khách qua app"
                    },
                    CreatedAt = seedDate
                },
                new ServicePackage
                {
                    Id = proPackageId,
                    Name = "Pro",
                    Description = "Dành cho buddy muốn tăng thu nhập và xây dựng thương hiệu cá nhân.",
                    PricePerMonth = 99000m,
                    Currency = "VND",
                    CommissionRate = 0.10m,
                    HasChatAccess = true,
                    HasSearchPriority = true,
                    HasPrioritySupport = false,
                    HasProductFeedback = false,
                    MaxSlots = null,
                    SortOrder = 2,
                    IsActive = true,
                    Features = new List<string>
                    {
                        "Profile xuất hiện trên nền tảng",
                        "Không giới hạn booking",
                        "Commission chỉ 10%",
                        "Chat với khách qua app",
                        "Ưu tiên xuất hiện trong tìm kiếm"
                    },
                    CreatedAt = seedDate
                },
                new ServicePackage
                {
                    Id = founderPackageId,
                    Name = "Founder",
                    Description = "Chỉ 30 suất. Đặc quyền early adopter dành cho buddy đầu tiên của Bonddy.",
                    PricePerMonth = 149000m,
                    Currency = "VND",
                    CommissionRate = 0.00m,
                    HasChatAccess = true,
                    HasSearchPriority = true,
                    HasPrioritySupport = true,
                    HasProductFeedback = true,
                    MaxSlots = 30,
                    SortOrder = 3,
                    IsActive = true,
                    Features = new List<string>
                    {
                        "Tất cả tính năng của Pro",
                        "0% commission — giữ 100% thu nhập",
                        "Xuất hiện đầu tiên trong tìm kiếm",
                        "Hỗ trợ trực tiếp từ team Bonddy",
                        "Góp ý định hướng sản phẩm",
                        "Chỉ 30 suất — đóng khi đủ"
                    },
                    CreatedAt = seedDate
                }
            );
        }
    }
}


// dotnet ef migrations add AddTripEntity --project BonddyPlatform.Repositories --startup-project BonddyPlatform.API
// dotnet ef database update --project BonddyPlatform.Repositories --startup-project BonddyPlatform.API