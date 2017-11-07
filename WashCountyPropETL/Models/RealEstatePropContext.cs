using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace WashCountyPropETL.Models
{
    public partial class RealEstatePropContext : DbContext
    {
        public virtual DbSet<WashCountyProp> WashCountyProp { get; set; }
        public virtual DbSet<WashCountyPropStaging> WashCountyPropStaging { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            #warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
            optionsBuilder.UseSqlServer(@"Data Source=DESKTOP-5LVATAU\SQLEXPRESS;Initial Catalog=RealEstateProp;Integrated Security=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WashCountyProp>(entity =>
            {
                entity.HasKey(e => e.PropertyId)
                    .HasName("PK_WashCountyProp_PropertyID");

                entity.Property(e => e.PropertyId).HasColumnName("PropertyID");

                entity.Property(e => e.Latitude).HasColumnType("decimal");

                entity.Property(e => e.Legal).HasColumnType("varchar(128)");

                entity.Property(e => e.Longitude).HasColumnType("decimal");

                entity.Property(e => e.LotSize).HasColumnType("varchar(128)");

                entity.Property(e => e.MarketBldgValue).HasColumnType("money");

                entity.Property(e => e.MarketLandValue).HasColumnType("money");

                entity.Property(e => e.NeighCode).HasColumnType("varchar(64)");

                entity.Property(e => e.PropAcctId)
                    .HasColumnName("PropAcctID")
                    .HasColumnType("varchar(64)");

                entity.Property(e => e.PropClass).HasColumnType("varchar(128)");

                entity.Property(e => e.RollDate).HasColumnType("date");

                entity.Property(e => e.SaleDate).HasColumnType("date");

                entity.Property(e => e.SaleDeed).HasColumnType("varchar(64)");

                entity.Property(e => e.SaleInstr).HasColumnType("varchar(64)");

                entity.Property(e => e.SalePrice).HasColumnType("money");

                entity.Property(e => e.SiteAddress).HasColumnType("varchar(128)");

                entity.Property(e => e.Source).HasColumnType("varchar(64)");

                entity.Property(e => e.SpecialMarketValue).HasColumnType("money");

                entity.Property(e => e.StagingId).HasColumnName("StagingID");

                entity.Property(e => e.TaxCode).HasColumnType("varchar(64)");

                entity.Property(e => e.TaxLotId)
                    .HasColumnName("TaxLotID")
                    .HasColumnType("varchar(64)");

                entity.Property(e => e.TaxableAssessedValue).HasColumnType("money");

                entity.HasOne(d => d.Staging)
                    .WithMany(p => p.WashCountyProp)
                    .HasForeignKey(d => d.StagingId)
                    .HasConstraintName("FK_WashCountyProp_StagingID");
            });

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