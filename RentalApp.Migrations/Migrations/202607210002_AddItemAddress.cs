using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RentalApp.Database.Data;

#nullable disable

namespace RentalApp.Migrations.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202607210002_AddItemAddress")]
public sealed class AddItemAddress : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Address",
            table: "Items",
            type: "character varying(250)",
            maxLength: 250,
            nullable: false,
            defaultValue: "Location not specified");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Address", table: "Items");
    }
}
