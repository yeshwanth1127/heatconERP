namespace HeatconERP.Web.Models;

public record CreateVendorPoRequest(Guid VendorId, DateTime OrderDate, List<CreateVendorPoLine> Lines);

public record CreateVendorPoLine(Guid MaterialVariantId, decimal OrderedQuantity, decimal UnitPrice);


