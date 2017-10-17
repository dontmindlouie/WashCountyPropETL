using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace WashCountyPropETL.Models
{
    public partial class RealEstatePropContext : DbContext
    {
        public virtual DbSet<WashCountyPropStaging> WashCountyPropStaging { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            #warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
            optionsBuilder.UseSqlServer(@"Data Source=DESKTOP-5LVATAU\SQLEXPRESS;Initial Catalog=RealEstateProp;Integrated Security=True");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WashCountyPropStaging>(entity =>
            {
                entity.HasKey(e => e.StagingId)
                    .HasName("PK_StagingID");

                entity.Property(e => e.StagingId).HasColumnName("StagingID");

                entity.Property(e => e.BldgArea).HasColumnType("varchar(32)");

                entity.Property(e => e.LatLong).HasColumnType("varchar(64)");

                entity.Property(e => e.Legal).HasColumnType("varchar(128)");

                entity.Property(e => e.LotSize).HasColumnType("varchar(32)");

                entity.Property(e => e.MarketBldgValue).HasColumnType("varchar(64)");

                entity.Property(e => e.MarketLandValue).HasColumnType("varchar(64)");

                entity.Property(e => e.NeighCode).HasColumnType("varchar(64)");

                entity.Property(e => e.PropAcctId)
                    .HasColumnName("PropAcctID")
                    .HasColumnType("varchar(64)");

                entity.Property(e => e.PropClass).HasColumnType("varchar(128)");

                entity.Property(e => e.RollDate).HasColumnType("varchar(64)");

                entity.Property(e => e.SaleDate).HasColumnType("varchar(64)");

                entity.Property(e => e.SaleDeed).HasColumnType("varchar(64)");

                entity.Property(e => e.SaleInstr).HasColumnType("varchar(64)");

                entity.Property(e => e.SalePrice).HasColumnType("varchar(64)");

                entity.Property(e => e.SiteAddress).HasColumnType("varchar(128)");

                entity.Property(e => e.Source).HasColumnType("varchar(32)");

                entity.Property(e => e.SpecialMarketValue).HasColumnType("varchar(64)");

                entity.Property(e => e.TaxCode).HasColumnType("varchar(64)");

                entity.Property(e => e.TaxLotId)
                    .HasColumnName("TaxLotID")
                    .HasColumnType("varchar(64)");

                entity.Property(e => e.TaxableAssessedValue).HasColumnType("varchar(64)");

                entity.Property(e => e.YearBuilt).HasColumnType("varchar(32)");
            });
        }
    }
}