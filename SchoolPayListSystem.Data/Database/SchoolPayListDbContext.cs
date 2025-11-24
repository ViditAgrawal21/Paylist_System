using System;
using Microsoft.EntityFrameworkCore;
using SchoolPayListSystem.Core.Models;

namespace SchoolPayListSystem.Data.Database
{
    public class SchoolPayListDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<SchoolType> SchoolTypes { get; set; }
        public DbSet<School> Schools { get; set; }
        public DbSet<SalaryEntry> SalaryEntries { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dbFolder = System.IO.Path.Combine(appData, "SchoolPayListSystem", "Database");
            string dbPath = System.IO.Path.Combine(dbFolder, "SchoolPayList.db");
            
            if (!System.IO.Directory.Exists(dbFolder))
                System.IO.Directory.CreateDirectory(dbFolder);
            
            string connectionString = $"Data Source={dbPath}";
            optionsBuilder.UseSqlite(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().HasKey(u => u.UserId);
            modelBuilder.Entity<Branch>().HasKey(b => b.BranchId);
            modelBuilder.Entity<SchoolType>().HasKey(st => st.SchoolTypeId);
            modelBuilder.Entity<School>().HasKey(s => s.SchoolId);
            modelBuilder.Entity<SalaryEntry>().HasKey(se => se.SalaryEntryId);

            modelBuilder.Entity<School>()
                .HasOne(s => s.SchoolType)
                .WithMany()
                .HasForeignKey(s => s.SchoolTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<School>()
                .HasOne(s => s.Branch)
                .WithMany()
                .HasForeignKey(s => s.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalaryEntry>()
                .HasOne(se => se.School)
                .WithMany()
                .HasForeignKey(se => se.SchoolId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalaryEntry>()
                .HasOne(se => se.Branch)
                .WithMany()
                .HasForeignKey(se => se.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalaryEntry>()
                .HasOne(se => se.CreatedByUser)
                .WithMany()
                .HasForeignKey(se => se.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Branch>().HasIndex(b => b.BranchCode).IsUnique();
            modelBuilder.Entity<School>().HasIndex(s => s.SchoolCode).IsUnique();
        }
    }
}
