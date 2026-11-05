using Inventory.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements", table =>
        {
            table.HasCheckConstraint("CK_StockMovements_Quantity_Positive", "Quantity > 0");
            table.HasCheckConstraint("CK_StockMovements_Type", "Type IN ('In', 'Out')");

            // Created by the InitialCreate migration; declared so EF generates trigger-safe SQL.
            table.HasTrigger("TR_StockMovements_NoUpdate");
            table.HasTrigger("TR_StockMovements_NoDelete");
        });

        builder.Property(m => m.Type).HasConversion<string>().HasMaxLength(3);
        builder.Property(m => m.Note).HasMaxLength(ProductRules.NoteMaxLength);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.ProductId, m.CreatedUtc });
    }
}
