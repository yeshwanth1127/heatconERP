using System;
using HeatconERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeatconERP.Infrastructure.Migrations
{
    [DbContext(typeof(HeatconDbContext))]
    [Migration("20260221170000_AddInventoryProcurementModule")]
    public partial class AddInventoryProcurementModule : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Materials
            migrationBuilder.CreateTable(
                name: "MaterialCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaterialVariants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Grade = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    Size = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    SKU = table.Column<string>(type: "text", nullable: false),
                    MinimumStockLevel = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialVariants_MaterialCategories_MaterialCategoryId",
                        column: x => x.MaterialCategoryId,
                        principalTable: "MaterialCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialVariants_MaterialCategoryId",
                table: "MaterialVariants",
                column: "MaterialCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialVariants_SKU",
                table: "MaterialVariants",
                column: "SKU",
                unique: true);

            // Vendors
            migrationBuilder.CreateTable(
                name: "Vendors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    GSTNumber = table.Column<string>(type: "text", nullable: true),
                    ContactDetails = table.Column<string>(type: "text", nullable: true),
                    IsApprovedVendor = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors", x => x.Id);
                });

            // Vendor PO
            migrationBuilder.CreateTable(
                name: "VendorPurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorPurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorPurchaseOrders_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VendorPurchaseOrders_VendorId",
                table: "VendorPurchaseOrders",
                column: "VendorId");

            migrationBuilder.CreateTable(
                name: "VendorPurchaseOrderLineItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorPurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorPurchaseOrderLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorPurchaseOrderLineItems_VendorPurchaseOrders_VendorPurchaseOrderId",
                        column: x => x.VendorPurchaseOrderId,
                        principalTable: "VendorPurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendorPurchaseOrderLineItems_MaterialVariants_MaterialVariantId",
                        column: x => x.MaterialVariantId,
                        principalTable: "MaterialVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VendorPurchaseOrderLineItems_VendorPurchaseOrderId",
                table: "VendorPurchaseOrderLineItems",
                column: "VendorPurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPurchaseOrderLineItems_MaterialVariantId",
                table: "VendorPurchaseOrderLineItems",
                column: "MaterialVariantId");

            // GRN
            migrationBuilder.CreateTable(
                name: "GRNs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorPurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GRNs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GRNs_VendorPurchaseOrders_VendorPurchaseOrderId",
                        column: x => x.VendorPurchaseOrderId,
                        principalTable: "VendorPurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GRNs_VendorPurchaseOrderId",
                table: "GRNs",
                column: "VendorPurchaseOrderId");

            migrationBuilder.CreateTable(
                name: "GRNLineItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GRNId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNumber = table.Column<string>(type: "text", nullable: false),
                    QuantityReceived = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    QualityStatus = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GRNLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GRNLineItems_GRNs_GRNId",
                        column: x => x.GRNId,
                        principalTable: "GRNs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GRNLineItems_MaterialVariants_MaterialVariantId",
                        column: x => x.MaterialVariantId,
                        principalTable: "MaterialVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GRNLineItems_GRNId",
                table: "GRNLineItems",
                column: "GRNId");

            migrationBuilder.CreateIndex(
                name: "IX_GRNLineItems_MaterialVariantId",
                table: "GRNLineItems",
                column: "MaterialVariantId");

            // Stock Batches
            migrationBuilder.CreateTable(
                name: "StockBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNumber = table.Column<string>(type: "text", nullable: false),
                    GRNLineItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityReceived = table.Column<decimal>(type: "numeric", nullable: false),
                    QuantityAvailable = table.Column<decimal>(type: "numeric", nullable: false),
                    QuantityReserved = table.Column<decimal>(type: "numeric", nullable: false),
                    QuantityConsumed = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    QualityStatus = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockBatches_MaterialVariants_MaterialVariantId",
                        column: x => x.MaterialVariantId,
                        principalTable: "MaterialVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockBatches_GRNLineItems_GRNLineItemId",
                        column: x => x.GRNLineItemId,
                        principalTable: "GRNLineItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockBatches_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockBatches_MaterialVariantId_BatchNumber",
                table: "StockBatches",
                columns: new[] { "MaterialVariantId", "BatchNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockBatches_GRNLineItemId",
                table: "StockBatches",
                column: "GRNLineItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockBatches_VendorId",
                table: "StockBatches",
                column: "VendorId");

            // SRS + allocations
            migrationBuilder.CreateTable(
                name: "SRSs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SRSs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SRSs_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SRSs_WorkOrderId",
                table: "SRSs",
                column: "WorkOrderId");

            migrationBuilder.CreateTable(
                name: "SRSLineItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SRSId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiredQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SRSLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SRSLineItems_SRSs_SRSId",
                        column: x => x.SRSId,
                        principalTable: "SRSs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SRSLineItems_MaterialVariants_MaterialVariantId",
                        column: x => x.MaterialVariantId,
                        principalTable: "MaterialVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SRSLineItems_SRSId",
                table: "SRSLineItems",
                column: "SRSId");

            migrationBuilder.CreateIndex(
                name: "IX_SRSLineItems_MaterialVariantId",
                table: "SRSLineItems",
                column: "MaterialVariantId");

            migrationBuilder.CreateTable(
                name: "SRSBatchAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SRSLineItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    ConsumedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SRSBatchAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SRSBatchAllocations_SRSLineItems_SRSLineItemId",
                        column: x => x.SRSLineItemId,
                        principalTable: "SRSLineItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SRSBatchAllocations_StockBatches_StockBatchId",
                        column: x => x.StockBatchId,
                        principalTable: "StockBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SRSBatchAllocations_SRSLineItemId",
                table: "SRSBatchAllocations",
                column: "SRSLineItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SRSBatchAllocations_StockBatchId",
                table: "SRSBatchAllocations",
                column: "StockBatchId");

            // Work order material requirements
            migrationBuilder.CreateTable(
                name: "WorkOrderMaterialRequirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiredQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    ReservedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    ConsumedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderMaterialRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderMaterialRequirements_MaterialVariants_MaterialVariantId",
                        column: x => x.MaterialVariantId,
                        principalTable: "MaterialVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrderMaterialRequirements_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderMaterialRequirements_WorkOrderId",
                table: "WorkOrderMaterialRequirements",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderMaterialRequirements_MaterialVariantId",
                table: "WorkOrderMaterialRequirements",
                column: "MaterialVariantId");

            // Stock Transactions
            migrationBuilder.CreateTable(
                name: "StockTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionType = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    LinkedWorkOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkedSRSId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransactions_StockBatches_StockBatchId",
                        column: x => x.StockBatchId,
                        principalTable: "StockBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransactions_WorkOrders_LinkedWorkOrderId",
                        column: x => x.LinkedWorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransactions_SRSs_LinkedSRSId",
                        column: x => x.LinkedSRSId,
                        principalTable: "SRSs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_StockBatchId",
                table: "StockTransactions",
                column: "StockBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_LinkedWorkOrderId",
                table: "StockTransactions",
                column: "LinkedWorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_LinkedSRSId",
                table: "StockTransactions",
                column: "LinkedSRSId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "StockTransactions");
            migrationBuilder.DropTable(name: "WorkOrderMaterialRequirements");
            migrationBuilder.DropTable(name: "SRSBatchAllocations");
            migrationBuilder.DropTable(name: "SRSLineItems");
            migrationBuilder.DropTable(name: "SRSs");
            migrationBuilder.DropTable(name: "StockBatches");
            migrationBuilder.DropTable(name: "GRNLineItems");
            migrationBuilder.DropTable(name: "GRNs");
            migrationBuilder.DropTable(name: "VendorPurchaseOrderLineItems");
            migrationBuilder.DropTable(name: "VendorPurchaseOrders");
            migrationBuilder.DropTable(name: "Vendors");
            migrationBuilder.DropTable(name: "MaterialVariants");
            migrationBuilder.DropTable(name: "MaterialCategories");
        }
    }
}


