using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleManagement.Core.Entities;

namespace VehicleManagement.Web.Data.Configurations;

public class ManufacturerConfiguration : IEntityTypeConfiguration<Manufacturer>
{
    public void Configure(EntityTypeBuilder<Manufacturer> builder)
    {
        builder.ToTable("Manufacturers");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(m => m.Name)
            .IsUnique();

        builder.HasData(
    new Manufacturer { Id = 1, Name = "Mazda" },
    new Manufacturer { Id = 2, Name = "Mercedes" },
    new Manufacturer { Id = 3, Name = "Honda" },
    new Manufacturer { Id = 4, Name = "Ferrari" },
    new Manufacturer { Id = 5, Name = "Toyota" }
);
    }
}