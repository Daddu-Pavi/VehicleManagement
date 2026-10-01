using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleManagement.Core.Entities;

namespace VehicleManagement.Web.Data.Configurations;

public class VehicleCategoryConfiguration
    : IEntityTypeConfiguration<VehicleCategory>
{
    public void Configure(EntityTypeBuilder<VehicleCategory> builder)
    {
        builder.ToTable("VehicleCategories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(c => c.Name)
            .IsUnique();

        builder.Property(c => c.MinWeight)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(c => c.MaxWeight)
            .HasPrecision(10, 2);

        builder.Property(c => c.Icon)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasData(
    new VehicleCategory
    {
        Id = 1,
        Name = "Light",
        MinWeight = 0m,
        MaxWeight = 500m,
        Icon = "car"
    },
    new VehicleCategory
    {
        Id = 2,
        Name = "Medium",
        MinWeight = 500m,
        MaxWeight = 2500m,
        Icon = "van"
    },
    new VehicleCategory
    {
        Id = 3,
        Name = "Heavy",
        MinWeight = 2500m,
        MaxWeight = null,
        Icon = "truck"
    }
);
    }
}