using HeatconERP.Domain.Entities.Inventory;

namespace HeatconERP.Application.Services.Srs;

public interface ISrsService
{
    Task<SRS> CreateSrsFromWorkOrderAsync(Guid workOrderId, IReadOnlyList<CreateSrsLine> lines, CancellationToken ct = default);
    Task<SRS> ApproveSrsAsync(Guid srsId, string? approvedBy, CancellationToken ct = default);
    Task AllocateBatchesFifoAsync(Guid srsId, CancellationToken ct = default);
}

public record CreateSrsLine(Guid MaterialVariantId, decimal RequiredQuantity);


