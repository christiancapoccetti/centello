using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Centesimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CategoryPaletteSlots : Migration
    {
        /// <inheritdoc />
        // The hex values and slot keys are frozen here on purpose: they are what older builds stored,
        // not the live catalog. The WHERE clause makes a second run a no-op.
        protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
            UPDATE Categories
            SET Color = CASE upper(Color)
                WHEN '#176B5B' THEN 'mint'
                WHEN '#8B4A5D' THEN 'coral'
                WHEN '#725C00' THEN 'butter'
                WHEN '#4F5F7A' THEN 'peri'
                WHEN '#6A4F88' THEN 'lilac'
                ELSE 'grey'
            END
            WHERE Color NOT IN ('coral', 'peri', 'butter', 'mint', 'lilac', 'sky', 'grey');
            """);

        /// <inheritdoc />
        // sky and grey have no legacy hex, so they stay as they are.
        protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
            UPDATE Categories
            SET Color = CASE Color
                WHEN 'mint' THEN '#176B5B'
                WHEN 'coral' THEN '#8B4A5D'
                WHEN 'butter' THEN '#725C00'
                WHEN 'peri' THEN '#4F5F7A'
                WHEN 'lilac' THEN '#6A4F88'
                ELSE Color
            END;
            """);
    }
}
