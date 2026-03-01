using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeatconERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorInvoiceQcDecisionHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrders_PurchaseInvoices_PurchaseInvoiceId",
                table: "WorkOrders");

            migrationBuilder.DropTable(
                name: "QualityInspections");

            migrationBuilder.AddColumn<decimal>(
                name: "ManualPrice",
                table: "Quotations",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceBreakdown",
                table: "Quotations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SnapshotManualPrice",
                table: "QuotationRevisions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SnapshotPriceBreakdown",
                table: "QuotationRevisions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MaterialCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ncrs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Stage = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Disposition = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedBy = table.Column<string>(type: "text", nullable: true),
                    ClosureNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ncrs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ncrs_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SRSs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SRSs", x => x.Id);
                });

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
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderLineItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    PartNumber = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderLineItems_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderQualityGates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Stage = table.Column<string>(type: "text", nullable: false),
                    GateStatus = table.Column<string>(type: "text", nullable: false),
                    PassedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PassedBy = table.Column<string>(type: "text", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderQualityGates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderQualityGates_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaterialVariants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Grade = table.Column<string>(type: "text", nullable: false),
                    Size = table.Column<string>(type: "text", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    SKU = table.Column<string>(type: "text", nullable: false),
                    MinimumStockLevel = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
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
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "WorkOrderQualityChecks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderQualityGateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Stage = table.Column<string>(type: "text", nullable: false),
                    Result = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderQualityChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderQualityChecks_WorkOrderQualityGates_WorkOrderQuali~",
                        column: x => x.WorkOrderQualityGateId,
                        principalTable: "WorkOrderQualityGates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrderQualityChecks_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SRSLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SRSLineItems_MaterialVariants_MaterialVariantId",
                        column: x => x.MaterialVariantId,
                        principalTable: "MaterialVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SRSLineItems_SRSs_SRSId",
                        column: x => x.SRSId,
                        principalTable: "SRSs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderMaterialRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderMaterialRequirements_MaterialVariants_MaterialVari~",
                        column: x => x.MaterialVariantId,
                        principalTable: "MaterialVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VendorPurchaseInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorPurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "text", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorPurchaseInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorPurchaseInvoices_VendorPurchaseOrders_VendorPurchaseO~",
                        column: x => x.VendorPurchaseOrderId,
                        principalTable: "VendorPurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendorPurchaseInvoices_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorPurchaseOrderLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorPurchaseOrderLineItems_MaterialVariants_MaterialVaria~",
                        column: x => x.MaterialVariantId,
                        principalTable: "MaterialVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendorPurchaseOrderLineItems_VendorPurchaseOrders_VendorPur~",
                        column: x => x.VendorPurchaseOrderId,
                        principalTable: "VendorPurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GRNs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorPurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorPurchaseInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceivedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GRNs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GRNs_VendorPurchaseInvoices_VendorPurchaseInvoiceId",
                        column: x => x.VendorPurchaseInvoiceId,
                        principalTable: "VendorPurchaseInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GRNs_VendorPurchaseOrders_VendorPurchaseOrderId",
                        column: x => x.VendorPurchaseOrderId,
                        principalTable: "VendorPurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VendorInvoiceQcDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorPurchaseInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Decision = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DecidedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorInvoiceQcDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorInvoiceQcDecisions_VendorPurchaseInvoices_VendorPurch~",
                        column: x => x.VendorPurchaseInvoiceId,
                        principalTable: "VendorPurchaseInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VendorPurchaseInvoiceLineItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorPurchaseInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorPurchaseInvoiceLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorPurchaseInvoiceLineItems_MaterialVariants_MaterialVar~",
                        column: x => x.MaterialVariantId,
                        principalTable: "MaterialVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendorPurchaseInvoiceLineItems_VendorPurchaseInvoices_Vendo~",
                        column: x => x.VendorPurchaseInvoiceId,
                        principalTable: "VendorPurchaseInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
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
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockBatches_GRNLineItems_GRNLineItemId",
                        column: x => x.GRNLineItemId,
                        principalTable: "GRNLineItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockBatches_MaterialVariants_MaterialVariantId",
                        column: x => x.MaterialVariantId,
                        principalTable: "MaterialVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockBatches_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
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
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransactions_SRSs_LinkedSRSId",
                        column: x => x.LinkedSRSId,
                        principalTable: "SRSs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                });

            migrationBuilder.CreateIndex(
                name: "IX_GRNLineItems_GRNId",
                table: "GRNLineItems",
                column: "GRNId");

            migrationBuilder.CreateIndex(
                name: "IX_GRNLineItems_MaterialVariantId",
                table: "GRNLineItems",
                column: "MaterialVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_GRNs_VendorPurchaseInvoiceId",
                table: "GRNs",
                column: "VendorPurchaseInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_GRNs_VendorPurchaseOrderId",
                table: "GRNs",
                column: "VendorPurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialVariants_MaterialCategoryId",
                table: "MaterialVariants",
                column: "MaterialCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialVariants_SKU",
                table: "MaterialVariants",
                column: "SKU",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ncrs_WorkOrderId",
                table: "Ncrs",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SRSBatchAllocations_SRSLineItemId",
                table: "SRSBatchAllocations",
                column: "SRSLineItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SRSBatchAllocations_StockBatchId",
                table: "SRSBatchAllocations",
                column: "StockBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_SRSLineItems_MaterialVariantId",
                table: "SRSLineItems",
                column: "MaterialVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_SRSLineItems_SRSId",
                table: "SRSLineItems",
                column: "SRSId");

            migrationBuilder.CreateIndex(
                name: "IX_StockBatches_GRNLineItemId",
                table: "StockBatches",
                column: "GRNLineItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockBatches_MaterialVariantId_BatchNumber",
                table: "StockBatches",
                columns: new[] { "MaterialVariantId", "BatchNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockBatches_VendorId",
                table: "StockBatches",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_LinkedSRSId",
                table: "StockTransactions",
                column: "LinkedSRSId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_LinkedWorkOrderId",
                table: "StockTransactions",
                column: "LinkedWorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_StockBatchId",
                table: "StockTransactions",
                column: "StockBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorInvoiceQcDecisions_VendorPurchaseInvoiceId_CreatedAt",
                table: "VendorInvoiceQcDecisions",
                columns: new[] { "VendorPurchaseInvoiceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VendorPurchaseInvoiceLineItems_MaterialVariantId",
                table: "VendorPurchaseInvoiceLineItems",
                column: "MaterialVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPurchaseInvoiceLineItems_VendorPurchaseInvoiceId",
                table: "VendorPurchaseInvoiceLineItems",
                column: "VendorPurchaseInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPurchaseInvoices_VendorId_InvoiceNumber",
                table: "VendorPurchaseInvoices",
                columns: new[] { "VendorId", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorPurchaseInvoices_VendorPurchaseOrderId",
                table: "VendorPurchaseInvoices",
                column: "VendorPurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPurchaseOrderLineItems_MaterialVariantId",
                table: "VendorPurchaseOrderLineItems",
                column: "MaterialVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPurchaseOrderLineItems_VendorPurchaseOrderId",
                table: "VendorPurchaseOrderLineItems",
                column: "VendorPurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPurchaseOrders_VendorId",
                table: "VendorPurchaseOrders",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderLineItems_WorkOrderId",
                table: "WorkOrderLineItems",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderMaterialRequirements_MaterialVariantId",
                table: "WorkOrderMaterialRequirements",
                column: "MaterialVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderQualityChecks_WorkOrderId",
                table: "WorkOrderQualityChecks",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderQualityChecks_WorkOrderQualityGateId",
                table: "WorkOrderQualityChecks",
                column: "WorkOrderQualityGateId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderQualityGates_WorkOrderId_Stage",
                table: "WorkOrderQualityGates",
                columns: new[] { "WorkOrderId", "Stage" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrders_PurchaseInvoices_PurchaseInvoiceId",
                table: "WorkOrders",
                column: "PurchaseInvoiceId",
                principalTable: "PurchaseInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrders_PurchaseInvoices_PurchaseInvoiceId",
                table: "WorkOrders");

            migrationBuilder.DropTable(
                name: "Ncrs");

            migrationBuilder.DropTable(
                name: "SRSBatchAllocations");

            migrationBuilder.DropTable(
                name: "StockTransactions");

            migrationBuilder.DropTable(
                name: "VendorInvoiceQcDecisions");

            migrationBuilder.DropTable(
                name: "VendorPurchaseInvoiceLineItems");

            migrationBuilder.DropTable(
                name: "VendorPurchaseOrderLineItems");

            migrationBuilder.DropTable(
                name: "WorkOrderLineItems");

            migrationBuilder.DropTable(
                name: "WorkOrderMaterialRequirements");

            migrationBuilder.DropTable(
                name: "WorkOrderQualityChecks");

            migrationBuilder.DropTable(
                name: "SRSLineItems");

            migrationBuilder.DropTable(
                name: "StockBatches");

            migrationBuilder.DropTable(
                name: "WorkOrderQualityGates");

            migrationBuilder.DropTable(
                name: "SRSs");

            migrationBuilder.DropTable(
                name: "GRNLineItems");

            migrationBuilder.DropTable(
                name: "GRNs");

            migrationBuilder.DropTable(
                name: "MaterialVariants");

            migrationBuilder.DropTable(
                name: "VendorPurchaseInvoices");

            migrationBuilder.DropTable(
                name: "MaterialCategories");

            migrationBuilder.DropTable(
                name: "VendorPurchaseOrders");

            migrationBuilder.DropTable(
                name: "Vendors");

            migrationBuilder.DropColumn(
                name: "AssemblyCompletedAt",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "AssignedToUserName",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "MaterialCompletedAt",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "PackingCompletedAt",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "PlanningCompletedAt",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "ProductionReceivedAt",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "ProductionReceivedBy",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "QcCompletedAt",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "SentToProductionAt",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "SentToProductionBy",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "TestingCompletedAt",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "WorkCompletedAt",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "WorkCompletedBy",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "WorkStartedAt",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "WorkStartedBy",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "ManualPrice",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "PriceBreakdown",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "SnapshotManualPrice",
                table: "QuotationRevisions");

            migrationBuilder.DropColumn(
                name: "SnapshotPriceBreakdown",
                table: "QuotationRevisions");

            migrationBuilder.CreateTable(
                name: "QualityInspections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InspectedBy = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Result = table.Column<string>(type: "text", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderNumber = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityInspections", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrders_PurchaseInvoices_PurchaseInvoiceId",
                table: "WorkOrders",
                column: "PurchaseInvoiceId",
                principalTable: "PurchaseInvoices",
                principalColumn: "Id");
        }
    }
}
