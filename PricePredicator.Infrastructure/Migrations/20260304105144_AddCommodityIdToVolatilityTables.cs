﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PricePredicator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommodityIdToVolatilityTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CommodityId",
                table: "Volatility_Silver",
                type: "integer",
                nullable: false,
                defaultValue: 2); // Silver

            migrationBuilder.AddColumn<int>(
                name: "CommodityId",
                table: "Volatility_Oil",
                type: "integer",
                nullable: false,
                defaultValue: 4); // Oil

            migrationBuilder.AddColumn<int>(
                name: "CommodityId",
                table: "Volatility_NaturalGas",
                type: "integer",
                nullable: false,
                defaultValue: 3); // NaturalGas

            migrationBuilder.AddColumn<int>(
                name: "CommodityId",
                table: "Volatility_Gold",
                type: "integer",
                nullable: false,
                defaultValue: 1); // Gold

            migrationBuilder.CreateIndex(
                name: "IX_Volatility_Silver_CommodityId",
                table: "Volatility_Silver",
                column: "CommodityId");

            migrationBuilder.CreateIndex(
                name: "IX_Volatility_Oil_CommodityId",
                table: "Volatility_Oil",
                column: "CommodityId");

            migrationBuilder.CreateIndex(
                name: "IX_Volatility_NaturalGas_CommodityId",
                table: "Volatility_NaturalGas",
                column: "CommodityId");

            migrationBuilder.CreateIndex(
                name: "IX_Volatility_Gold_CommodityId",
                table: "Volatility_Gold",
                column: "CommodityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Volatility_Gold_Commodities_CommodityId",
                table: "Volatility_Gold",
                column: "CommodityId",
                principalTable: "Commodities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Volatility_NaturalGas_Commodities_CommodityId",
                table: "Volatility_NaturalGas",
                column: "CommodityId",
                principalTable: "Commodities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Volatility_Oil_Commodities_CommodityId",
                table: "Volatility_Oil",
                column: "CommodityId",
                principalTable: "Commodities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Volatility_Silver_Commodities_CommodityId",
                table: "Volatility_Silver",
                column: "CommodityId",
                principalTable: "Commodities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Volatility_Gold_Commodities_CommodityId",
                table: "Volatility_Gold");

            migrationBuilder.DropForeignKey(
                name: "FK_Volatility_NaturalGas_Commodities_CommodityId",
                table: "Volatility_NaturalGas");

            migrationBuilder.DropForeignKey(
                name: "FK_Volatility_Oil_Commodities_CommodityId",
                table: "Volatility_Oil");

            migrationBuilder.DropForeignKey(
                name: "FK_Volatility_Silver_Commodities_CommodityId",
                table: "Volatility_Silver");

            migrationBuilder.DropIndex(
                name: "IX_Volatility_Silver_CommodityId",
                table: "Volatility_Silver");

            migrationBuilder.DropIndex(
                name: "IX_Volatility_Oil_CommodityId",
                table: "Volatility_Oil");

            migrationBuilder.DropIndex(
                name: "IX_Volatility_NaturalGas_CommodityId",
                table: "Volatility_NaturalGas");

            migrationBuilder.DropIndex(
                name: "IX_Volatility_Gold_CommodityId",
                table: "Volatility_Gold");

            migrationBuilder.DropColumn(
                name: "CommodityId",
                table: "Volatility_Silver");

            migrationBuilder.DropColumn(
                name: "CommodityId",
                table: "Volatility_Oil");

            migrationBuilder.DropColumn(
                name: "CommodityId",
                table: "Volatility_NaturalGas");

            migrationBuilder.DropColumn(
                name: "CommodityId",
                table: "Volatility_Gold");
        }
    }
}

