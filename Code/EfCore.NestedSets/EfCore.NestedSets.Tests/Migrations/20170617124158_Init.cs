using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace EfCore.NestedSets.Tests.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
              name: "Modules",
              columns: table => new
              {
                  Id = table.Column<int>(nullable: false)
                      .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                  Label = table.Column<string>(nullable: false)
              },
              constraints: table =>
              {
                  table.PrimaryKey("PK_Modules", x => x.Id);
              });

            migrationBuilder.CreateTable(
               name: "ModuleStructures",
               columns: table => new
               {
                   Id = table.Column<int>(nullable: false)
                       .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                   Left = table.Column<int>(nullable: false),
                   Level = table.Column<int>(nullable: false),
                   Name = table.Column<string>(nullable: true),
                   ParentId = table.Column<int>(nullable: true),
                   Right = table.Column<int>(nullable: false),
                   RootId = table.Column<int>(nullable: true),
                   EntryKey = table.Column<int>(nullable: true),
                   NodeInstanceId = table.Column<int>(nullable: true)
               },
               constraints: table =>
               {
                   table.PrimaryKey("PK_ModuleStructures", x => x.Id);
                   table.ForeignKey(
                       name: "FK_ModuleStructures_ModuleStructures_ParentId",
                       column: x => x.ParentId,
                       principalTable: "ModuleStructures",
                       principalColumn: "Id",
                       onDelete: ReferentialAction.Restrict);
                   table.ForeignKey(
                       name: "FK_ModuleStructures_ModuleStructures_RootId",
                       column: x => x.RootId,
                       principalTable: "ModuleStructures",
                       principalColumn: "Id",
                       onDelete: ReferentialAction.Restrict);
                   table.ForeignKey(
                       name: "FK_ModuleStructures_Module_NodeInstanceId",
                       column: x => x.NodeInstanceId,
                       principalTable: "Modules",
                       principalColumn: "Id",
                       onDelete: ReferentialAction.Restrict);
               });


            migrationBuilder.CreateTable(
               name: "ModuleEntries",
               columns: table => new
               {
                   Id = table.Column<int>(nullable: false)
                       .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                   Label = table.Column<string>(nullable: false)/*,
                   IsDeleted = table.Column<int>(nullable: false),
                   CreatedDate = table.Column<DateTime>(nullable: true),
                   UpdatedDate = table.Column<DateTime>(nullable: true)*/
               },
               constraints: table =>
               {
                   table.PrimaryKey("PK_ModuleEntries", x => x.Id);
               });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleStructures_ParentId",
                table: "ModuleStructures",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleStructures_RootId",
                table: "ModuleStructures",
                column: "RootId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModuleStructures");
        }
    }
}
