using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abac.WebApi.Migrations.Casbin
{
    /// <inheritdoc />
    public partial class initCasbin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "casbin_rule",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ptype = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    v0 = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    v1 = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    v2 = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    v3 = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    v4 = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    v5 = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    v6 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    v7 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    v8 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    v9 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    v10 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    v11 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    v12 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    v13 = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_casbin_rule", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_casbin_rule_ptype",
                table: "casbin_rule",
                column: "ptype");

            migrationBuilder.CreateIndex(
                name: "IX_casbin_rule_v0",
                table: "casbin_rule",
                column: "v0");

            migrationBuilder.CreateIndex(
                name: "IX_casbin_rule_v1",
                table: "casbin_rule",
                column: "v1");

            migrationBuilder.CreateIndex(
                name: "IX_casbin_rule_v2",
                table: "casbin_rule",
                column: "v2");

            migrationBuilder.CreateIndex(
                name: "IX_casbin_rule_v3",
                table: "casbin_rule",
                column: "v3");

            migrationBuilder.CreateIndex(
                name: "IX_casbin_rule_v4",
                table: "casbin_rule",
                column: "v4");

            migrationBuilder.CreateIndex(
                name: "IX_casbin_rule_v5",
                table: "casbin_rule",
                column: "v5");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "casbin_rule");
        }
    }
}
