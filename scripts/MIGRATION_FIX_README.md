# Migration Fix Guide

## Problem
EF Core says "No migrations were applied" but the database is missing columns like `SentToCustomerAt`, `QuotationRevisionId`, etc. This happens when migration files exist but weren't actually executed on the database.

## Solution

### Option 1: Run the comprehensive SQL fix script (RECOMMENDED)

**Stop the API first** (Ctrl+C on watch-api), then run:

```powershell
.\scripts\apply-all-migrations.ps1
```

Or manually:
```powershell
psql -U postgres -d heatconerp -f scripts/fix-all-migrations.sql
```

This script will:
- Add all missing columns from migrations `20260221100000`, `20260221110000`, and `20260221120000`
- Create missing tables (`PurchaseOrderLineItems`, `PurchaseInvoices`, `PurchaseInvoiceLineItems`)
- Add indexes and foreign keys
- Update the `__EFMigrationsHistory` table so EF Core knows migrations are applied

### Option 2: Use EF Core migration command (if Option 1 fails)

**Stop the API first**, then:

```powershell
dotnet ef database update --project src/HeatconERP.Infrastructure --startup-project src/HeatconERP.API
```

If this still says "No migrations were applied", manually insert the migration records:

```sql
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES 
    ('20260221100000_AddPurchaseOrderQuotationAndLineItems', '10.0.0'),
    ('20260221110000_AddPurchaseInvoices', '10.0.0'),
    ('20260221120000_AddSentToCustomerAndPoRevisionLink', '10.0.0')
ON CONFLICT DO NOTHING;
```

Then run the SQL script from Option 1 to add the actual columns/tables.

## What Gets Fixed

### Migration 20260221100000_AddPurchaseOrderQuotationAndLineItems
- `PurchaseOrders.QuotationId` (nullable FK to Quotations)
- `PurchaseOrders.CustomerPONumber`
- `PurchaseOrders.PODate`
- `PurchaseOrders.DeliveryTerms`
- `PurchaseOrders.PaymentTerms`
- `PurchaseOrderLineItems` table (with FK to PurchaseOrders)

### Migration 20260221110000_AddPurchaseInvoices
- `PurchaseInvoices` table
- `PurchaseInvoiceLineItems` table
- Foreign keys and indexes

### Migration 20260221120000_AddSentToCustomerAndPoRevisionLink
- `QuotationRevisions.SentToCustomerAt`
- `QuotationRevisions.SentToCustomerBy`
- `PurchaseOrders.QuotationRevisionId` (nullable FK to QuotationRevisions)

## After Running the Fix

1. **Restart the API**: `.\scripts\watch-api.ps1`
2. **Verify**: The error `column q1.SentToCustomerAt does not exist` should be gone
3. **Test**: Try accessing `/api/quotations` endpoint - it should work

## Troubleshooting

If you get "relation does not exist" errors:
- Make sure PostgreSQL is running
- Check your `.env` file has correct `DATABASE_URL`
- Verify database `heatconerp` exists: `psql -U postgres -l | grep heatconerp`

If migrations still fail:
- Check PostgreSQL logs for detailed errors
- Verify you have permissions: `GRANT ALL ON DATABASE heatconerp TO postgres;`
