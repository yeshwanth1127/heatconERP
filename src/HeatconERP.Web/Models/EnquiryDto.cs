namespace HeatconERP.Web.Models;

public record EnquiryListDto(Guid Id, string EnquiryNumber, string CompanyName, string ProductDescription, string Status, DateTime DateReceived, string Priority, string Source, string? AssignedTo, string FeasibilityStatus, bool IsAerospace);

public record EnquiryDetailDto(
    Guid Id, string EnquiryNumber, DateTime DateReceived, string Source, string? AssignedTo, string Status,
    string CompanyName, string? ContactPerson, string? Email, string? Phone, string? Gst, string? Address,
    string? ProductDescription, int? Quantity, DateTime? ExpectedDeliveryDate, string? AttachmentPath,
    bool IsAerospace, string Priority, string FeasibilityStatus, string? FeasibilityNotes,
    string? CreatedBy, DateTime CreatedAt, DateTime? UpdatedAt);
