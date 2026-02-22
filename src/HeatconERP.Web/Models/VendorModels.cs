namespace HeatconERP.Web.Models;

public record VendorDto(Guid Id, string Name, string? GSTNumber, string? ContactDetails, bool IsApprovedVendor);

public record CreateVendorRequest(string Name, string? GSTNumber, string? ContactDetails, bool IsApprovedVendor);


