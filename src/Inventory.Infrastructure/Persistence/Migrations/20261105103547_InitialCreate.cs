using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Sku = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ReorderLevel = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CurrentStock = table.Column<long>(type: "INTEGER", nullable: false),
                    Version = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.CheckConstraint("CK_Products_CurrentStock_NonNegative", "CurrentStock >= 0");
                    table.CheckConstraint("CK_Products_Name_NotBlank", "trim(Name) <> ''");
                    table.CheckConstraint("CK_Products_ReorderLevel_NonNegative", "ReorderLevel >= 0");
                    table.CheckConstraint("CK_Products_Sku_NotBlank", "trim(Sku) <> ''");
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.CheckConstraint("CK_StockMovements_Quantity_Positive", "Quantity > 0");
                    table.CheckConstraint("CK_StockMovements_Type", "Type IN ('In', 'Out')");
                    table.ForeignKey(
                        name: "FK_StockMovements_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Sku",
                table: "Products",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductId_CreatedUtc",
                table: "StockMovements",
                columns: new[] { "ProductId", "CreatedUtc" });

            // The stock ledger is append-only: history can never be edited or removed, only added to.
            migrationBuilder.Sql(
                "CREATE TRIGGER TR_StockMovements_NoUpdate BEFORE UPDATE ON StockMovements " +
                "BEGIN SELECT RAISE(ABORT, 'LEDGER_IS_APPEND_ONLY'); END;");

            migrationBuilder.Sql(
                "CREATE TRIGGER TR_StockMovements_NoDelete BEFORE DELETE ON StockMovements " +
                "BEGIN SELECT RAISE(ABORT, 'LEDGER_IS_APPEND_ONLY'); END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER TR_StockMovements_NoDelete;");
            migrationBuilder.Sql("DROP TRIGGER TR_StockMovements_NoUpdate;");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
