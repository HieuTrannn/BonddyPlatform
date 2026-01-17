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
        }
    }
}
