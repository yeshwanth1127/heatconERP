using HeatconERP.Application.Services.Procurement;
using HeatconERP.Domain.Enums.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeatconERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProcurementController : ControllerBase
{
    private readonly IProcurementService _procurement;

    public ProcurementController(IProcurementService procurement) => _procurement = procurement;

    [HttpPost("vendor-po")]
    public async Task<ActionResult<VendorPoDto>> CreateVendorPo([FromBody] CreateVendorPoRequest req, CancellationToken ct)
    {
        try
        {
            var po = await _procurement.CreateVendorPoAsync(req.VendorId, req.OrderDate, req.Lines, ct);
            return Ok(new VendorPoDto(po.Id, po.VendorId, po.OrderDate, po.Status.ToString()));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Concurrency conflict. Refresh and retry.");
        }
    }

    [HttpPost("grn")]
    public async Task<ActionResult<GrnDto>> CreateGrn([FromBody] CreateGrnRequest req, CancellationToken ct)
    {
        try
        {
            var grn = await _procurement.CreateGrnAsync(req.VendorPurchaseOrderId, req.ReceivedDate, req.InvoiceNumber, req.Lines, ct);
            return Ok(new GrnDto(grn.Id, grn.VendorPurchaseOrderId, grn.ReceivedDate, grn.InvoiceNumber));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Concurrency conflict. Refresh and retry.");
        }
    }

    [HttpPost("grn/line/{grnLineItemId:guid}/process")]
    public async Task<ActionResult<StockBatchDto>> ProcessGrnLine(Guid grnLineItemId, [FromBody] ProcessGrnLineRequest req, CancellationToken ct)
    {
        try
        {
            var batch = await _procurement.ProcessGrnLineAndCreateBatchAsync(grnLineItemId, req.VendorId, req.QualityStatus, ct);
            return Ok(new StockBatchDto(batch.Id, batch.MaterialVariantId, batch.BatchNumber, batch.QuantityReceived, batch.QuantityAvailable, batch.QuantityReserved, batch.QuantityConsumed, batch.QualityStatus.ToString()));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Concurrency conflict. Refresh and retry.");
        }
    }
}

public record VendorPoDto(Guid Id, Guid VendorId, DateTime OrderDate, string Status);

public record CreateVendorPoRequest(Guid VendorId, DateTime OrderDate, List<CreateVendorPoLine> Lines);

public record GrnDto(Guid Id, Guid VendorPurchaseOrderId, DateTime ReceivedDate, string InvoiceNumber);

public record CreateGrnRequest(Guid VendorPurchaseOrderId, DateTime ReceivedDate, string InvoiceNumber, List<CreateGrnLine> Lines);

public record ProcessGrnLineRequest(Guid VendorId, QualityStatus QualityStatus);

public record StockBatchDto(Guid Id, Guid MaterialVariantId, string BatchNumber, decimal QuantityReceived, decimal QuantityAvailable, decimal QuantityReserved, decimal QuantityConsumed, string QualityStatus);


