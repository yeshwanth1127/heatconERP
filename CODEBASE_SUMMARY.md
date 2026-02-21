# HeatconERP Codebase Summary & Architecture

## 🏗️ System Architecture

### Technology Stack
- **Backend**: C# .NET 10 with ASP.NET Core
- **Database**: PostgreSQL (connection via DatabaseURL environment variable)
- **Frontend**: Blazor (interactive server-side rendering) with Tailwind CSS
- **Desktop**: WPF Application (.NET)
- **ORM**: Entity Framework Core with migrations

### Project Structure (Multi-Tier)
```
src/
├── HeatconERP.API              # REST API Layer
│   ├── Controllers/            # HTTP endpoint handlers
│   ├── Models/                 # DTOs and request/response contracts
│   └── Program.cs              # DI setup, migrations, seeding
├── HeatconERP.Web              # Blazor Frontend
│   ├── Components/Pages/       # Razor page components (.razor files)
│   ├── Services/               # ApiClient, AuthService
│   ├── Models/                 # Web-layer DTOs
│   └── Program.cs              # Blazor config
├── HeatconERP.Domain           # Domain Entities (Business Objects)
│   ├── Entities/               # User, Enquiry, Quotation, etc.
│   ├── Enums/                  # UserRole, WorkOrderStage, etc.
├── HeatconERP.Infrastructure   # Data Access Layer
│   ├── Data/HeatconDbContext   # EF Core DbContext
│   └── Migrations/             # Schema versions
├── HeatconERP.Application      # Business Logic (minimal, for future expansion)
└── HeatconERP.Desktop          # WPF Desktop App
```

---

## 📋 Core Entities & their Relationships

### Quotation Management System (Main Focus)

#### 1. **Quotation** Entity
```csharp
// Key Properties:
- Id (Guid)                     // Unique identifier
- ReferenceNumber (string)      // Auto-generated: QT-2600001 format
- EnquiryId (Guid?)             // Optional link to source enquiry
- Version (string)              // Current version: v1, v2, v3...
- ClientName, ProjectName       // From linked enquiry
- Description, Attachments      // Quotation details
- Status                        // Draft, In Review, Published, Expired
- Amount (decimal?)             // Total = Subtotal + Tax
- CreatedAt, CreatedByUserName  // Audit fields

// Relationships:
- Enquiry? (optional parent)
- LineItems[] (child, cascade delete)
- Revisions[] (child, cascade delete)
```

#### 2. **QuotationLineItem** Entity
```csharp
// Key Properties:
- Id, QuotationId              // Foreign key to Quotation
- SortOrder (int)              // Display order
- PartNumber, Description      // Item details
- Quantity (int), UnitPrice    // Calculation inputs
- TaxPercent (decimal)         // Tax rate for this item
- AttachmentPath               // Optional attachment URL/path

// Formula:
- LineTotal = Quantity × UnitPrice
- LineTax = LineTotal × (TaxPercent / 100)
- Quotation.Amount = Σ(LineTotal + LineTax) for all items
```

#### 3. **QuotationRevision** Entity (Audit & History)
```csharp
// Tracks ALL changes to a quotation
- Id, QuotationId              // Foreign key to Quotation
- Version (string)             // Revision version: v1, v2, v3...
- Action (string)              // "initiated draft", "updated", "created"
- ChangedBy, ChangedAt         // WHO changed it and WHEN
- ChangeDetails                // Description of changes (human-readable)
- AttachmentPath/FileName      // If revision includes attachment

// Snapshots (historical state preservation):
- SnapshotClientName           // State at this revision
- SnapshotProjectName
- SnapshotDescription
- SnapshotAttachments
- SnapshotStatus
- SnapshotAmount
- SnapshotLineItemsJson        // JSON array of LineItems at this revision

// Uses:
- Viewing past revisions sees exact state at that time
- Complete audit trail (who changed what, when)
- Ability to compare versions
```

#### 4. **Enquiry** Entity (Source Document)
```csharp
// 1. Basic Info:
- EnquiryNumber (ENQ-001), DateReceived, Source (IndiaMart/Email/Phone/etc)
- Status (New, Under Review, Feasible, Not Feasible, Converted, Closed)

// 2. Customer Details:
- CompanyName, ContactPerson, Email, Phone, GST, Address

// 3. Product Requirement:
- ProductDescription, Quantity, ExpectedDeliveryDate
- AttachmentPath (drawing/image)

// 4. Classification:
- IsAerospace (boolean)
- Priority (Low/Medium/High)
```

---

## 🔌 API Endpoints (QuotationsController)

### List Quotations
```
GET /api/quotations
Query Parameters:
  ?enquiryId={guid}          - Filter by source enquiry
  ?searchId={id}             - Search reference number (contains)
  ?client={name}             - Filter by client name
  ?status={status}           - Filter by status
  ?limit=100                 - Max results

Returns: QuotationListDto[]
- Id, ReferenceNumber, EnquiryNumber, ClientName, ProjectName, Version, CreatedAt, Amount, Status
```

### Get Quotation Detail
```
GET /api/quotations/{id}
Returns: QuotationDetailDto
- All quotation fields
- LineItems[] (sorted by SortOrder)
- Revisions[] (sorted by ChangedAt DESC)
```

### Get Specific Revision
```
GET /api/quotations/{quotationId}/revisions/{revisionId}
Returns: QuotationRevisionDetailDto
- Revision metadata
- Snapshot of entire quotation at that revision
- Snapshot line items deserialized and returned
```

### Create Quotation
```
POST /api/quotations
Body: { enquiryId?: guid, createdBy?: string }
- Auto-generates ReferenceNumber (QT-2600{count})
- Auto-extracts ClientName, ProjectName from enquiry
- Creates initial v1 with "initiated draft" revision
Returns: QuotationDetailDto
```

### Update Quotation
```
PUT /api/quotations/{id}
Body: {
  status?: string,
  clientName?: string,
  projectName?: string,
  description?: string,
  attachments?: string (comma-separated),
  lineItems?: [{ partNumber, description, quantity, unitPrice, taxPercent, attachmentPath }],
  changedBy?: string
}
- Creates revision with Action = "updated"
- Recalculates Amount based on line items
- Stores snapshot of current state
Returns: 204 No Content
```

### Generate New Revision
```
POST /api/quotations/{id}/revision
Body: {
  lineItems?: [...],
  changeDetails?: string,
  changedBy?: string
}
- Increments version (v1 → v2 → v3)
- Creates revision with Action = "created"
- Stores snapshot of new state
Returns: QuotationDetailDto (updated quotation)
```

---

## 🎨 Frontend (Web Component)

### Quotations.razor Page
**Location**: `src/HeatconERP.Web/Components/Pages/Quotations.razor` (792 lines)

#### Layout
- **Left Panel**: List of quotations with search/filter controls
- **Right Panel**: Detail view of selected quotation with revision history

#### Features
1. **Quotations List**
   - Searchable by ID, Client, Status
   - Sortable, expandable rows
   - Real-time filtering

2. **Detail View**
   - Editable inline: Client, Project, Description, Status
   - Attachments (comma-separated URLs)
   - Line items table with add/remove rows
   - Real-time calculations: Subtotal, Tax, Grand Total

3. **Revision History Timeline**
   - Visual timeline of all revisions
   - Click revision to view snapshot
   - Shows: Version, Action, ChangedBy, Timestamp, ChangeDetails

4. **Actions**
   - **Save Draft**: Saves current edits, creates "updated" revision
   - **Generate Revision**: Creates new version (v2, v3...) with new revision record
   - **Create New**: Creates new quotation (triggers dialog or redirects)

#### Code Structure (C# component logic)
```csharp
// Properties
private List<QuotationListDto> _quotations;
private QuotationDetailDto? _selectedDetail;
private QuotationRevisionDetailDto? _viewingRevisionDetail;
private List<EditableLineItem> _editingLineItems;
private decimal _subtotal, _totalTax, _grandTotal; // Auto-calculated

// Methods
- LoadAsync() → Fetch quotations from API
- SelectQuotation(id) → Load detail for selected quotation
- SaveQuotationAsync() → PUT to API with updates
- GenerateRevision() → POST to API to create new version
- SelectRevisionAsync(id) → Load snapshot of specific revision
- ClearRevisionView() → Hide revision detail panel
```

---

## 🗄️ Database Migrations (Key Files)

### Migration Files
1. `20260220120000_AddQuotationLineItemsAndRevisions.cs`
   - Creates QuotationLineItems table with FK to Quotations
   - Creates QuotationRevisions table with FK to Quotations
   - Adds Quotations columns: Version, ClientName, ProjectName, CreatedByUserName, Description, Attachments

2. `20260220130000_AddQuotationDescriptionAndAttachments.cs`
   - Adds Description and Attachments columns

3. `20260220140000_AddQuotationRevisionSnapshot.cs`
   - Adds snapshot columns to QuotationRevisions for historical state preservation

### Connection Setup
- Uses `DATABASE_URL` environment variable from `.env` file
- Format: `Host=localhost;Port=5432;Database=heatconerp;Username=postgres;Password=xxx`
- Migrations applied on API startup via `db.Database.MigrateAsync()`

---

## 📊 Data Flow Example

### Creating & Revising a Quotation

```
1. CREATE QUOTATION
   POST /api/quotations { enquiryId: "..." }
   ↓
   - Quotation created with v1
   - Reference number generated: QT-2600001
   - QuotationRevision created: Version=v1, Action="initiated draft"
   - Returns: QuotationDetailDto with empty line items

2. EDIT & SAVE DRAFT
   PUT /api/quotations/{id} { lineItems: [...], description: "...", changedBy: "user123" }
   ↓
   - Quotation updated: Description, Amount
   - QuotationLineItems replaced with new ones
   - QuotationRevision created: Version=v1, Action="updated", stores snapshot
   - Amount calculated from line items

3. GENERATE NEW VERSION (Revision)
   POST /api/quotations/{id}/revision { lineItems: [...], changeDetails: "..." }
   ↓
   - Quotation updated: Version=v2, Amount recalculated
   - QuotationRevision created: Version=v2, Action="created", stores snapshot
   - Now _quotations has 2 revisions: both v1 and v2 snapshots preserved

4. VIEW PAST REVISION
   GET /api/quotations/{id}/revisions/{revisionId}
   ↓
   - Returns QuotationRevisionDetailDto with snapshot data
   - Shows exact state of quotation at that revision time
   - Line items deserialized from JSON
```

---

## 🧠 Key System Concepts

### Versioning
- **Quotation.Version**: Main version (v1, v2, v3...)
- **QuotationRevision.Version**: Matches the quotation version at revision time
- New revision = New main version

### Snapshots & Audit Trail
- **Why Snapshots?**: Can view exact quotation state at any point in time
- **SnapshotLineItemsJson**: Stores line items as JSON for historical viewing
- **ChangedBy + ChangedAt**: Complete audit trail of all changes with ownership

### Status Lifecycle
- **Draft**: Being edited, not yet finalized
- **In Review**: Sent for approval
- **Published**: Approved and locked
- **Expired**: No longer valid

### Calculation
- Automatic total = Σ(Quantity × UnitPrice × (1 + TaxPercent/100))
- Recalculated on every line item change or save

---

## 🚀 Development Quick Reference

### Build & Run
```powershell
# Build
dotnet build HeatconERP.slnx

# Run API (will auto-migrate database)
dotnet run --project src/HeatconERP.API
# Or with watch mode
.\scripts\watch-api.ps1

# Run Web (Blazor)
dotnet run --project src/HeatconERP.Web --launch-profile http
# Or with watch mode
.\scripts\watch-web.ps1
```

### Test URLs
- API: http://localhost:5212
- Swagger: http://localhost:5212/swagger
- Web: http://localhost:5118

### Sample Users (seeded on startup)
- **admin** / admin123 (MD role)
- **crmmanager** / crm123 (CRM Manager)
- **crmstaff** / crm123 (CRM Staff)

---

## 📚 Ready to Build From Here

**Current Status**: Quotation management system is fully functional with:
- ✅ Creation, editing, revision history
- ✅ Line item management
- ✅ Automatic versioning
- ✅ Complete audit trail
- ✅ Tax calculations
- ✅ Status tracking

**Next Areas for Development**:
- Quotation approval workflow (pending approvals)
- Export/Print quotation PDFs
- Email quotation to client
- Quotation templates
- Advanced revision comparison
- Document management improvements
- Integration with Purchase Orders (next stage in workflow)

---

## 🎯 Architecture Patterns Used

1. **Repository Pattern**: DbContext with DbSet<T> for data access
2. **MVVM (Blazor)**: Components manage state, ApiClient handles HTTP
3. **DTO Pattern**: Separate models for API communication
4. **Audit Pattern**: ActivityLog + Revision snapshots
5. **Event Sourcing**: QuotationRevisions track all state changes
6. **Soft Delete**: IsDeleted flag for enquiries (can extend globally)
