using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleManagement.Core.Entities;

namespace VehicleManagement.Web.Data.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.OwnerName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.YearOfManufacture)
            .IsRequired();

        builder.Property(v => v.WeightKg)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.HasOne(v => v.Manufacturer)
            .WithMany(m => m.Vehicles)
            .HasForeignKey(v => v.ManufacturerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}