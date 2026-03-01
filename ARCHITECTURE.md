# HeatconERP System Architecture

## Table of Contents
1. [Overview](#overview)
2. [Technology Stack](#technology-stack)
3. [Architecture Pattern](#architecture-pattern)
4. [Project Structure](#project-structure)
5. [Core Modules](#core-modules)
6. [Data Model](#data-model)
7. [API Layer](#api-layer)
8. [Frontend Layer](#frontend-layer)
9. [Database Design](#database-design)
10. [Key Workflows](#key-workflows)
11. [Security & Authentication](#security--authentication)
12. [Deployment & Configuration](#deployment--configuration)

---

## Overview

**HeatconERP** is a comprehensive Enterprise Resource Planning (ERP) system designed for manufacturing companies specializing in thermal sensors and aerospace components. The system manages the complete business lifecycle from customer enquiries through production, quality control, and inventory management.

### Key Features
- **CRM Module**: Lead management, quotations with versioning, purchase orders, and invoices
- **Production Module**: Work order management with stage-based workflow and pipeline tracking
- **Quality Control**: Stage-based quality gates, inspections, and non-conformance reports (NCR)
- **Inventory Management**: Material master data, batch traceability, FIFO stock allocation, and procurement
- **Role-Based Access Control**: Multi-role system with role-specific dashboards and permissions

---

## Technology Stack

### Backend
- **Framework**: .NET 10.0 (ASP.NET Core)
- **Database**: PostgreSQL (via Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0)
- **ORM**: Entity Framework Core 10.0.0
- **API**: RESTful API with Swagger/OpenAPI documentation
- **Authentication**: BCrypt.Net-Next 4.1.0 for password hashing
- **Configuration**: DotNetEnv 3.1.1 for environment variable management

### Frontend
- **Framework**: Blazor Server (Interactive Server Components)
- **Styling**: Tailwind CSS 3.4.0
- **UI Components**: Material Symbols Icons, Bootstrap
- **Theme**: Dark industrial theme optimized for manufacturing environments

### Desktop (Optional)
- **Framework**: WPF (.NET 10.0) - Separate desktop application

---

## Architecture Pattern

The system follows **Clean Architecture** principles with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────┐
│                    Presentation Layer                    │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │  Blazor Web  │  │  WPF Desktop  │  │  REST API    │  │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  │
└─────────┼─────────────────┼─────────────────┼──────────┘
          │                 │                 │
┌─────────┼─────────────────┼─────────────────┼──────────┐
│         │   Application Layer (Services)     │         │
│         │  ┌──────────────────────────────┐  │         │
│         │  │  InventoryService           │  │         │
│         │  │  ProcurementService         │  │         │
│         │  │  SrsService                 │  │         │
│         │  └──────────────────────────────┘  │         │
└─────────┼────────────────────────────────────┼─────────┘
          │                                    │
┌─────────┼────────────────────────────────────┼──────────┐
│         │   Domain Layer (Entities & Enums)  │         │
│         │  ┌──────────────────────────────┐  │         │
│         │  │  WorkOrder, Quotation, etc. │  │         │
│         │  │  Business Rules & Logic      │  │         │
│         │  └──────────────────────────────┘  │         │
└─────────┼────────────────────────────────────┼─────────┘
          │                                    │
┌─────────┼────────────────────────────────────┼──────────┐
│         │   Infrastructure Layer             │         │
│         │  ┌──────────────────────────────┐  │         │
│         │  │  HeatconDbContext            │  │         │
│         │  │  EF Core Migrations          │  │         │
│         │  │  PostgreSQL Provider          │  │         │
│         │  └──────────────────────────────┘  │         │
└─────────┼────────────────────────────────────┼─────────┘
          │                                    │
          └────────────────────────────────────┘
                    PostgreSQL Database
```

### Dependency Flow
- **Domain** ← Application ← Infrastructure
- **API** → Application + Infrastructure
- **Web** → API (via HTTP client)
- **Desktop** → API (via HTTP client)

---

## Project Structure

```
src/
├── HeatconERP.API/              # REST API Layer
│   ├── Controllers/            # API endpoints
│   │   ├── AuthController.cs
│   │   ├── DashboardController.cs
│   │   ├── EnquiriesController.cs
│   │   ├── QuotationsController.cs
│   │   ├── PurchaseOrdersController.cs
│   │   ├── PurchaseInvoicesController.cs
│   │   ├── WorkOrdersController.cs
│   │   ├── ProductionController.cs
│   │   ├── WorkOrderQualityController.cs
│   │   ├── InventoryController.cs
│   │   ├── MaterialController.cs
│   │   ├── VendorController.cs
│   │   ├── VendorInvoicesController.cs
│   │   ├── ProcurementController.cs
│   │   └── SRSController.cs
│   └── Program.cs              # API startup & configuration
│
├── HeatconERP.Web/              # Blazor Frontend
│   ├── Components/
│   │   ├── Pages/              # Page components
│   │   │   ├── Dashboard.razor
│   │   │   ├── Enquiries.razor
│   │   │   ├── Quotations.razor
│   │   │   ├── WorkOrders.razor
│   │   │   ├── WorkOrderPipeline.razor
│   │   │   ├── Production.razor
│   │   │   ├── Quality.razor
│   │   │   ├── Inventory.razor
│   │   │   └── ...
│   │   └── Layout/             # Layout components
│   │       ├── MainLayout.razor
│   │       └── NavMenu.razor
│   ├── Services/
│   │   ├── ApiClient.cs        # HTTP client wrapper
│   │   └── AuthService.cs      # Authentication state
│   ├── Models/                 # DTOs and view models
│   └── Program.cs              # Web startup
│
├── HeatconERP.Domain/           # Domain Layer
│   ├── Entities/               # Domain entities
│   │   ├── BaseEntity.cs      # Base class with audit fields
│   │   ├── User.cs
│   │   ├── Enquiry.cs
│   │   ├── Quotation.cs
│   │   ├── PurchaseOrder.cs
│   │   ├── PurchaseInvoice.cs
│   │   ├── WorkOrder.cs
│   │   ├── WorkOrderQualityGate.cs
│   │   ├── WorkOrderQualityCheck.cs
│   │   ├── Ncr.cs
│   │   └── Inventory/         # Inventory entities
│   │       ├── MaterialCategory.cs
│   │       ├── MaterialVariant.cs
│   │       ├── Vendor.cs
│   │       ├── VendorPurchaseOrder.cs
│   │       ├── VendorPurchaseInvoice.cs
│   │       ├── GRN.cs
│   │       ├── StockBatch.cs
│   │       ├── StockTransaction.cs
│   │       ├── SRS.cs
│   │       └── ...
│   └── Enums/                  # Domain enums
│       ├── UserRole.cs
│       ├── WorkOrderStage.cs
│       ├── QualityGateStatus.cs
│       └── Inventory/          # Inventory enums
│
├── HeatconERP.Application/      # Application Layer
│   ├── Services/               # Business logic services
│   │   ├── Inventory/
│   │   │   ├── IInventoryService.cs
│   │   │   └── InventoryService.cs
│   │   ├── Procurement/
│   │   │   ├── IProcurementService.cs
│   │   │   └── ProcurementService.cs
│   │   └── Srs/
│   │       ├── ISrsService.cs
│   │       └── SrsService.cs
│   └── Abstractions/
│       └── IHeatconDbContext.cs
│
├── HeatconERP.Infrastructure/   # Infrastructure Layer
│   ├── Data/
│   │   └── HeatconDbContext.cs # EF Core DbContext
│   └── Migrations/             # Database migrations
│       └── YYYYMMDDHHMMSS_Description.cs
│
└── HeatconERP.Desktop/          # WPF Desktop Application (Optional)
```

---

## Core Modules

### 1. CRM Module

**Purpose**: Manage customer relationships from initial enquiry to order fulfillment.

**Key Entities**:
- `Enquiry`: Customer enquiries with feasibility tracking
- `Quotation`: Quotations with versioning support (v1, v2, v3...)
- `QuotationLineItem`: Line items with tax calculations
- `QuotationRevision`: Complete revision history with snapshots
- `PurchaseOrder`: Customer purchase orders
- `PurchaseInvoice`: Customer invoices

**Workflow**:
1. Enquiry received → Status tracking (New, Under Review, Feasible, Not Feasible, Converted)
2. Create Quotation from Enquiry (optional)
3. Edit & Save Draft → Revision tracking
4. Generate Revision (creates new version)
5. Send to Customer
6. Convert to Purchase Order
7. Create Purchase Invoice from PO
8. Convert Invoice to Work Order

### 2. Production Module

**Purpose**: Manage production work orders through a stage-based workflow.

**Key Entities**:
- `WorkOrder`: Production work orders with stage tracking
- `WorkOrderLineItem`: Line items for work orders
- `WorkOrderMaterialRequirement`: Material requirements planning

**Work Order Stages**:
1. **Planning**: Initial planning and setup
2. **Material**: Material procurement and allocation
3. **Assembly**: Product assembly
4. **Testing**: Product testing
5. **QC**: Quality control inspection
6. **Packing**: Final packing and dispatch

**Workflow**:
1. Purchase Invoice → Create Work Order
2. Send to Production (CRM → Production handoff)
3. Production Receives
4. Start Work (begin production)
5. Stage progression with timestamps
6. Quality gates block forward movement if not passed
7. NCRs block progression if open
8. Complete Work Order

**Pipeline Tracking**:
- Visual timeline showing stage progression
- Timestamps for each stage completion
- Quality gate status per stage
- NCR tracking per work order

### 3. Quality Control Module

**Purpose**: Ensure product quality through stage-based quality gates and inspections.

**Key Entities**:
- `WorkOrderQualityGate`: Quality gates per production stage
- `WorkOrderQualityCheck`: Quality inspection records
- `Ncr`: Non-conformance reports

**Quality Gate Status**:
- `Pending`: Not yet checked
- `Passed`: Quality check passed
- `Failed`: Quality check failed

**NCR Disposition**:
- `UseAsIs`: Use despite non-conformance
- `Rework`: Requires rework
- `Scrap`: Scrap the item
- `ReturnToVendor`: Return to vendor

**Workflow**:
1. Quality gates created automatically per stage
2. Quality checks recorded per gate
3. Failed checks create NCRs
4. NCRs must be closed before progression
5. Quality gates must pass before stage advancement

### 4. Inventory & Procurement Module

**Purpose**: Manage materials, vendors, procurement, and stock with batch traceability.

**Key Entities**:
- `MaterialCategory`: Material categories (Stainless Steel, Aluminium, Titanium)
- `MaterialVariant`: Material variants with SKU, grade, size
- `Vendor`: Vendor master data
- `VendorPurchaseOrder`: Vendor purchase orders
- `VendorPurchaseInvoice`: Vendor invoices
- `GRN`: Goods Receipt Note (receiving)
- `StockBatch`: Stock batches with batch numbers
- `StockTransaction`: Stock movements (Reserve, Consume, Adjust)
- `SRS`: Store Requisition Slip (material issue requests)
- `SRSBatchAllocation`: FIFO batch allocations

**Inventory Workflow**:
1. Create Vendor PO
2. Receive Vendor Invoice
3. Create GRN from Vendor Invoice
4. GRN creates Stock Batches with batch numbers
5. SRS requests material for work orders
6. FIFO allocation from batches
7. Stock consumption on work order completion

**Stock Transaction Types**:
- `Reserve`: Reserve stock for allocation
- `Consume`: Consume reserved stock
- `Adjust`: Manual stock adjustment

**FIFO Algorithm**:
- First-In-First-Out stock allocation
- Automatic batch selection based on receipt date
- Batch traceability throughout production

---

## Data Model

### Base Entity Pattern

All domain entities inherit from `BaseEntity`:

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public byte[] RowVersion { get; set; }  // Optimistic concurrency
}
```

**Features**:
- **Soft Delete**: `IsDeleted` flag prevents hard deletes
- **Audit Trail**: `CreatedAt` and `UpdatedAt` timestamps
- **Optimistic Concurrency**: `RowVersion` prevents concurrent update conflicts
- **Auto-population**: DbContext automatically sets these fields on save

### Entity Relationships

**CRM Relationships**:
```
Enquiry (optional) → Quotation → QuotationRevision
Quotation → PurchaseOrder → PurchaseInvoice → WorkOrder
```

**Production Relationships**:
```
WorkOrder → WorkOrderLineItem (1:N)
WorkOrder → WorkOrderQualityGate (1:N)
WorkOrder → WorkOrderQualityCheck (1:N)
WorkOrder → Ncr (1:N)
WorkOrder → WorkOrderMaterialRequirement (1:N)
```

**Inventory Relationships**:
```
MaterialCategory → MaterialVariant (1:N)
MaterialVariant → StockBatch (1:N)
Vendor → VendorPurchaseOrder (1:N)
VendorPurchaseOrder → VendorPurchaseInvoice (1:N)
VendorPurchaseInvoice → GRN (1:N)
GRN → GRNLineItem → StockBatch (1:1)
StockBatch → StockTransaction (1:N)
SRS → SRSLineItem → SRSBatchAllocation → StockBatch (1:N)
```

### Enum Storage

Enums are stored as strings in PostgreSQL:
- `UserRole`: MD, GM, FrontOffice, CRMManager, CRMStaff, ProductionManager, ProductionStaff, QualityCheck, DispatchStoreManager
- `WorkOrderStage`: Planning, Material, Assembly, Testing, QC, Packing
- `QualityGateStatus`: Pending, Passed, Failed
- `VendorPurchaseOrderStatus`: Draft, Sent, Accepted, Rejected
- `StockTransactionType`: Reserve, Consume, Adjust
- `SrsStatus`: Draft, PendingApproval, Approved, Allocated, Consumed, Rejected

---

## API Layer

### API Structure

The API follows RESTful conventions with clear endpoint naming:

**Base URL**: `http://localhost:5212/api`

### Controllers Overview

1. **AuthController** (`/api/auth`)
   - `POST /login`: User authentication

2. **DashboardController** (`/api/dashboard`)
   - `GET /?role={role}`: Role-based dashboard data

3. **EnquiriesController** (`/api/enquiries`)
   - `GET /`: List enquiries with filters
   - `GET /{id}`: Get enquiry details
   - `POST /`: Create enquiry
   - `PUT /{id}`: Update enquiry
   - `POST /seed`: Seed sample data

4. **QuotationsController** (`/api/quotations`)
   - `GET /`: List quotations
   - `GET /{id}`: Get quotation details
   - `POST /`: Create quotation
   - `PUT /{id}`: Update quotation
   - `POST /{id}/revision`: Generate revision
   - `GET /{id}/revisions/{revisionId}`: Get revision details
   - `POST /{id}/revisions/{revisionId}/send`: Send revision to customer

5. **PurchaseOrdersController** (`/api/purchaseorders`)
   - `GET /`: List purchase orders
   - `GET /{id}`: Get PO details
   - `GET /{id}/compare`: Compare PO with quotation
   - `POST /`: Create PO
   - `PUT /{id}`: Update PO

6. **PurchaseInvoicesController** (`/api/purchaseinvoices`)
   - `GET /`: List purchase invoices
   - `GET /{id}`: Get invoice details
   - `POST /`: Create invoice
   - `PUT /{id}`: Update invoice

7. **WorkOrdersController** (`/api/workorders`)
   - `GET /`: List work orders
   - `GET /{id}`: Get work order details
   - `POST /from-invoice/{invoiceId}`: Create work order from invoice
   - `PUT /{id}`: Update work order
   - `POST /{id}/send-to-production`: Send to production
   - `POST /{id}/receive-by-production`: Receive by production
   - `GET /{id}/quality`: Get quality summary
   - `GET /{id}/ncrs`: List NCRs

8. **ProductionController** (`/api/production`)
   - `GET /workorders`: Get production work orders
   - `GET /workorders?stage={stage}`: Filter by stage
   - `PATCH /{id}/stage`: Update work order stage
   - `GET /{id}/pipeline`: Get pipeline details
   - `GET /pipeline/all`: Get all pipelines
   - `POST /{id}/start-work`: Start work order

9. **WorkOrderQualityController** (`/api/workorders/{id}/quality`)
   - `GET /`: Get quality summary
   - `POST /checks`: Record quality check
   - `GET /ncrs`: List NCRs
   - `POST /ncrs/{ncrId}/close`: Close NCR
   - `DELETE /`: Delete quality data

10. **InventoryController** (`/api/inventory`)
    - `GET /inventory-summary`: Get inventory summary
    - `GET /summary/{materialVariantId}`: Get variant stock summary
    - `GET /batch-history/{batchNumber}`: Get batch history
    - `GET /material-tree`: Get material tree structure

11. **MaterialController** (`/api/material`)
    - `GET /categories`: List material categories
    - `POST /categories`: Create category
    - `GET /variants`: List material variants
    - `POST /variants`: Create variant

12. **VendorController** (`/api/vendor`)
    - `GET /`: List vendors
    - `POST /`: Create vendor

13. **VendorInvoicesController** (`/api/vendor-invoices`)
    - `GET /`: List vendor invoices
    - `GET /{id}`: Get invoice details
    - `POST /from-vendor-po/{poId}`: Create invoice from PO
    - `POST /{id}/accept`: Accept vendor invoice

14. **ProcurementController** (`/api/procurement`)
    - `POST /vendor-po`: Create vendor PO
    - `GET /vendor-pos`: List vendor POs
    - `GET /vendor-pos/{id}`: Get vendor PO details
    - `POST /direct-grn`: Create direct GRN
    - `GET /grns`: List GRNs
    - `GET /grns/{id}`: Get GRN details
    - `POST /grns/{id}/submit-draft`: Submit GRN draft
    - `GET /next-batch-number/{materialVariantId}`: Get next batch number

15. **SRSController** (`/api/srs`)
    - `GET /`: List SRS requests
    - `GET /{id}`: Get SRS details
    - `POST /`: Create SRS
    - `POST /{id}/approve`: Approve SRS
    - `POST /{id}/allocate-fifo`: Allocate FIFO
    - `POST /{id}/consume`: Consume allocated stock
    - `POST /{id}/create-vendor-po`: Create vendor PO from SRS

### API Response Patterns

**Success Response**: Standard HTTP status codes (200, 201, 204)

**Error Response**: 
```json
{
  "error": "Error message description"
}
```

**DTO Pattern**: Separate DTOs for API communication to avoid exposing internal entities.

---

## Frontend Layer

### Blazor Server Architecture

**Technology**: Blazor Server with Interactive Server Components

**Key Components**:
- **Pages**: Full-page components (`*.razor`)
- **Layout**: `MainLayout.razor` with navigation menu
- **Services**: `ApiClient` for HTTP calls, `AuthService` for authentication state

### Page Structure

**Main Pages**:
- `Dashboard.razor`: Role-based dashboard
- `Enquiries.razor`: Enquiry list and management
- `EnquiryDetail.razor`: Enquiry detail view
- `Quotations.razor`: Quotation editor with revisions
- `PurchaseOrders.razor`: Purchase order management
- `PurchaseInvoices.razor`: Purchase invoice management
- `WorkOrders.razor`: Work order list
- `WorkOrderPipeline.razor`: Production pipeline visual timeline
- `Production.razor`: Production dashboard
- `Quality.razor`: Quality control queues
- `Inventory.razor`: Inventory overview
- `StockOverview.razor`: Stock levels by variant
- `StockAdjustments.razor`: Manual stock adjustments
- `SrsRequests.razor`: SRS management
- `GrnPage.razor`: GRN processing
- `VendorPos.razor`: Vendor PO management
- `VendorInvoices.razor`: Vendor invoice processing
- `BatchTraceability.razor`: Batch tracking and history
- `MaterialPlanner.razor`: Material planning for work orders
- `Vendors.razor`: Vendor master data
- `Reports.razor`: Reporting dashboard

### Frontend Services

**ApiClient** (`Services/ApiClient.cs`):
- Centralized HTTP client wrapper
- Methods for all API endpoints
- Error handling and JSON serialization
- Base URL configuration

**AuthService** (`Services/AuthService.cs`):
- Authentication state management
- User session handling
- Role-based access control

### Styling

- **Tailwind CSS**: Utility-first CSS framework
- **Dark Theme**: Industrial dark theme optimized for manufacturing
- **Material Symbols**: Icon library
- **Bootstrap**: Additional UI components

---

## Database Design

### Database: PostgreSQL

**Connection**: Configured via `DATABASE_URL` environment variable or `appsettings.json`

### Key Design Patterns

1. **Soft Delete**: All entities use `IsDeleted` flag
   - Query filters automatically exclude deleted records
   - Data retention for audit purposes

2. **Optimistic Concurrency**: `RowVersion` byte array
   - Prevents concurrent update conflicts
   - Auto-updated on save

3. **Audit Fields**: `CreatedAt`, `UpdatedAt`
   - Automatic timestamp management
   - Full audit trail

4. **Cascade Rules**:
   - **Cascade Delete**: Line items deleted with parent
   - **Restrict Delete**: Prevent deletion if referenced (inventory entities)
   - **Set Null**: Optional relationships set to null on delete

5. **Unique Constraints**:
   - Material Variant SKU
   - Stock Batch (MaterialVariantId + BatchNumber)
   - Vendor Invoice (VendorId + InvoiceNumber)
   - Work Order Quality Gate (WorkOrderId + Stage)

### Migrations

**Location**: `HeatconERP.Infrastructure/Migrations/`

**Naming**: `YYYYMMDDHHMMSS_Description.cs`

**Auto-Migration**: Migrations applied automatically on API startup

**Recent Migrations**:
- Work Order Pipeline additions
- Quotation pricing fields
- Vendor purchase invoices and GRN invoice links
- Inventory procurement module
- Work order production dispatch
- Quality inspection additions

---

## Key Workflows

### 1. Quotation Workflow

```
Enquiry → Create Quotation (v1)
  ↓
Edit & Save Draft (revision tracking)
  ↓
Generate Revision (v2, v3...)
  ↓
Send to Customer
  ↓
Convert to Purchase Order
```

**Features**:
- Complete revision history with snapshots
- Line item tracking with tax calculations
- Manual pricing or price breakdown
- Attachment support

### 2. Work Order Production Workflow

```
Purchase Invoice → Create Work Order
  ↓
Send to Production (CRM → Production handoff)
  ↓
Production Receives
  ↓
Start Work
  ↓
Stage Progression:
  Planning → Material → Assembly → Testing → QC → Packing
  ↓
Quality Gates (must pass before advancement)
  ↓
NCR Handling (must be closed if opened)
  ↓
Complete Work Order
```

**Pipeline Tracking**:
- Visual timeline with stage timestamps
- Quality gate status per stage
- NCR tracking
- Material requirements planning

### 3. Inventory Procurement Workflow

```
Create Vendor PO
  ↓
Receive Vendor Invoice
  ↓
Create GRN from Vendor Invoice
  ↓
GRN Creates Stock Batches (with batch numbers)
  ↓
SRS Requests Material (for work orders)
  ↓
FIFO Allocation from Batches
  ↓
Stock Consumption (on work order completion)
```

**Features**:
- Batch traceability throughout production
- FIFO stock allocation
- Quality status tracking per batch
- Stock transaction history

### 4. Quality Control Workflow

```
Work Order Created
  ↓
Quality Gates Created (per stage)
  ↓
Quality Checks Recorded
  ↓
If Failed → Create NCR
  ↓
NCR Must Be Closed Before Progression
  ↓
Quality Gate Must Pass Before Stage Advancement
```

**Quality Gates**:
- One gate per production stage
- Status: Pending, Passed, Failed
- Blocks forward movement if not passed

**NCR Disposition**:
- UseAsIs, Rework, Scrap, ReturnToVendor
- Must be closed before work order progression

---

## Security & Authentication

### Authentication

**Method**: Username/Password with BCrypt hashing

**Login Endpoint**: `POST /api/auth/login`

**Response**: Returns user details including role

### User Roles

1. **MD** (Managing Director): Full system access
2. **GM** (General Manager): Management oversight
3. **FrontOffice**: Front office operations
4. **CRMManager**: CRM module management
5. **CRMStaff**: CRM operations
6. **ProductionManager**: Production management
7. **ProductionStaff**: Production operations
8. **QualityCheck**: Quality control operations
9. **DispatchStoreManager**: Inventory and dispatch management

### Role-Based Access Control

**Navigation Menu**: Filtered by role
- Different modules visible per role
- Role-specific menu items

**Dashboard Data**: Filtered by role
- Role-specific KPIs and metrics
- Role-based data visibility

**API Authorization**: Role-based endpoint access (can be extended)

### Seed Users

Default users created on first run:
- `admin` / `admin123` (MD)
- `crmmanager` / `crm123` (CRM Manager)
- `crmstaff` / `crm123` (CRM Staff)
- `prodmanager` / `prod123` (Production Manager)
- `prodstaff` / `prod123` (Production Staff)
- `qualitycheck` / `qc123` (Quality Check)
- `storemanager` / `store123` (Dispatch Store Manager)

---

## Deployment & Configuration

### Configuration Files

**Backend** (`src/HeatconERP.API/`):
- `appsettings.json`: Connection strings, logging
- `appsettings.Development.json`: Development settings
- `.env`: Database connection (`DATABASE_URL`)

**Frontend** (`src/HeatconERP.Web/`):
- `appsettings.json`: API base URL (`ApiBaseUrl`)

### Environment Variables

**DATABASE_URL**: PostgreSQL connection string
```
Host=localhost;Port=5432;Database=heatconerp;Username=postgres;Password=your_password
```

**ApiBaseUrl**: API base URL for frontend
```
http://localhost:5212
```

### Running the Application

**API**:
```powershell
cd src/HeatconERP.API
dotnet run
```
- Auto-migrates database on startup
- Seeds default users and sample data
- Swagger UI: `http://localhost:5212/swagger`

**Web**:
```powershell
cd src/HeatconERP.Web
dotnet run
```
- Blazor Server application
- Default URL: `http://localhost:5118`

**Watch Mode Scripts**:
- `.\scripts\watch-api.ps1`: Watch API for changes
- `.\scripts\watch-web.ps1`: Watch Web for changes

### Default URLs

- **API**: `http://localhost:5212`
- **Swagger**: `http://localhost:5212/swagger`
- **Web**: `http://localhost:5118`
- **Health Check**: `http://localhost:5212/health`

### Database Setup

1. Create PostgreSQL database
2. Set `DATABASE_URL` environment variable or configure in `appsettings.json`
3. Run API - migrations applied automatically
4. Seed data created on first run

---

## Key Architectural Patterns

1. **Clean Architecture**: Separation of Domain, Application, Infrastructure, Presentation
2. **Repository Pattern**: DbContext as repository abstraction
3. **DTO Pattern**: Separate DTOs for API communication
4. **Soft Delete**: IsDeleted flag for data retention
5. **Audit Trail**: ActivityLog + Revision snapshots
6. **Event Sourcing**: QuotationRevisions track all changes
7. **Optimistic Concurrency**: RowVersion for conflict detection
8. **FIFO Inventory**: First-In-First-Out stock allocation
9. **Quality Gates**: Stage-based quality control
10. **Role-Based Access Control**: Navigation and data filtered by role

---

## Summary

HeatconERP is a comprehensive ERP system built with modern .NET technologies, following clean architecture principles. It provides end-to-end management of:

- **CRM**: From enquiries to quotations to purchase orders
- **Production**: Stage-based work order management with pipeline tracking
- **Quality Control**: Quality gates and non-conformance reporting
- **Inventory**: Material management with batch traceability and FIFO allocation
- **Procurement**: Vendor management, purchase orders, invoices, and GRN processing

The system is designed for manufacturing companies requiring:
- Complete traceability from customer order to production
- Quality control at every production stage
- Batch-level inventory tracking
- Role-based access control
- Comprehensive audit trails

The architecture supports scalability, maintainability, and extensibility through clear separation of concerns and modern development practices.
