using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using RentalApp.Database.Data;

#nullable disable

namespace RentalApp.Migrations.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202607160001_InitialCreate")]
public sealed class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS postgis;");

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DisplayName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Users", value => value.Id));

        migrationBuilder.CreateTable(
            name: "Items",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Description = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
                DailyRate = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                Category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                Location = table.Column<Point>(type: "geography (point)", nullable: false),
                IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Items", value => value.Id);
                table.ForeignKey("FK_Items_Users_OwnerId", value => value.OwnerId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "RefreshTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RefreshTokens", value => value.Id);
                table.ForeignKey("FK_RefreshTokens_Users_UserId", value => value.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Rentals",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                BorrowerId = table.Column<Guid>(type: "uuid", nullable: false),
                StartDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                EndDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                TotalPrice = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Rentals", value => value.Id);
                table.ForeignKey("FK_Rentals_Items_ItemId", value => value.ItemId, "Items", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_Rentals_Users_BorrowerId", value => value.BorrowerId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Reviews",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RentalId = table.Column<Guid>(type: "uuid", nullable: false),
                ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                Rating = table.Column<int>(type: "integer", nullable: false),
                Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Reviews", value => value.Id);
                table.CheckConstraint("CK_Reviews_Rating", "\"Rating\" BETWEEN 1 AND 5");
                table.ForeignKey("FK_Reviews_Items_ItemId", value => value.ItemId, "Items", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_Reviews_Rentals_RentalId", value => value.RentalId, "Rentals", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_Reviews_Users_ReviewerId", value => value.ReviewerId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_Items_Location", "Items", "Location").Annotation("Npgsql:IndexMethod", "gist");
        migrationBuilder.CreateIndex("IX_Items_OwnerId", "Items", "OwnerId");
        migrationBuilder.CreateIndex("IX_RefreshTokens_TokenHash", "RefreshTokens", "TokenHash", unique: true);
        migrationBuilder.CreateIndex("IX_RefreshTokens_UserId", "RefreshTokens", "UserId");
        migrationBuilder.CreateIndex("IX_Rentals_BorrowerId", "Rentals", "BorrowerId");
        migrationBuilder.CreateIndex("IX_Rentals_ItemId_StartDateUtc_EndDateUtc", "Rentals", new[] { "ItemId", "StartDateUtc", "EndDateUtc" });
        migrationBuilder.CreateIndex("IX_Reviews_ItemId", "Reviews", "ItemId");
        migrationBuilder.CreateIndex("IX_Reviews_RentalId", "Reviews", "RentalId", unique: true);
        migrationBuilder.CreateIndex("IX_Reviews_ReviewerId", "Reviews", "ReviewerId");
        migrationBuilder.CreateIndex("IX_Users_Email", "Users", "Email", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("RefreshTokens");
        migrationBuilder.DropTable("Reviews");
        migrationBuilder.DropTable("Rentals");
        migrationBuilder.DropTable("Items");
        migrationBuilder.DropTable("Users");
    }
}
