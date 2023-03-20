using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace AdvertisementWpf.Models
{
    public partial class AdvertisementNFContext : DbContext
    {
        public AdvertisementNFContext()
        {
        }

        public AdvertisementNFContext(DbContextOptions<AdvertisementNFContext> options)
            : base(options)
        {
        }

        public virtual DbSet<TypeOfActivity> TypeOfActivitys { get; set; }
        public virtual DbSet<WorkInTechCard> WorkInTechCards { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseSqlServer("Data Source=YEVGENIY; Database=AdvertisementNF; Integrated Security=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "Cyrillic_General_CI_AS");

            modelBuilder.Entity<TypeOfActivity>(entity =>
            {
                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .IsFixedLength(true);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<WorkInTechCard>(entity =>
            {
                entity.HasIndex(e => e.TechCardID, "IX_WorkInTechCards");

                entity.Property(e => e.DateFactCompletion).HasColumnType("datetime");

                entity.Property(e => e.DatePlanCompletion).HasColumnType("datetime");

                entity.HasOne(d => d.TypeOfActivity)
                    .WithMany(p => p.WorkInTechCards)
                    .HasForeignKey(d => d.TypeOfActivityID)
                    .HasConstraintName("FK_WorkInTechCards_TypeOfActivitys");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
