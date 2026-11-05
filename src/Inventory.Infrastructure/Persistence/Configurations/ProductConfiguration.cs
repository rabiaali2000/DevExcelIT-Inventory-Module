using Inventory.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", table =>
        {
            table.HasCheckConstraint("CK_Products_Sku_NotBlank", "trim(Sku) <> ''");
            table.HasCheckConstraint("CK_Products_Name_NotBlank", "trim(Name) <> ''");
            table.HasCheckConstraint("CK_Products_ReorderLevel_NonNegative", "ReorderLevel >= 0");
            table.HasCheckConstraint("CK_Products_CurrentStock_NonNegative", "CurrentStock >= 0");
        });

        builder.Property(p => p.Sku).HasMaxLength(ProductRules.SkuMaxLength);
        builder.HasIndex(p => p.Sku).IsUnique();

        builder.Property(p => p.Name).HasMaxLength(ProductRules.NameMaxLength);
        builder.Property(p => p.Description).HasMaxLength(ProductRules.DescriptionMaxLength);
        builder.Property(p => p.ReorderLevel).HasDefaultValue(0);
        builder.Property(p => p.Version).IsConcurrencyToken();

        // Stock changes only through the guarded, atomic UPDATE. Saving a stock change through
        // change tracking would write an absolute value, so it must fail loudly instead.
        builder.Property(p => p.CurrentStock)
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }
}
