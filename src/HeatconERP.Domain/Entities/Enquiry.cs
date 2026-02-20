namespace HeatconERP.Domain.Entities;

public class Enquiry
{
    public Guid Id { get; set; }

    // 1. Basic Info
    public string EnquiryNumber { get; set; } = string.Empty; // ENQ-001
    public DateTime DateReceived { get; set; }
    public string Source { get; set; } = "Manual"; // IndiaMart, Email, WhatsApp, Phone, Manual
    public string? AssignedTo { get; set; } // Staff name
    public string Status { get; set; } = "New"; // New, Under Review, Feasible, Not Feasible, Converted, Closed

    // 2. Customer Details
    public string CompanyName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Gst { get; set; }
    public string? Address { get; set; }

    // 3. Product Requirement
    public string? ProductDescription { get; set; }
    public int? Quantity { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string? AttachmentPath { get; set; } // Path/URL to drawing/image

    // 4. Classification
    public bool IsAerospace { get; set; }
    public string Priority { get; set; } = "Medium"; // Low, Medium, High

    // 5. Feasibility
    public string FeasibilityStatus { get; set; } = "Pending"; // Pending, Feasible, Not Feasible
    public string? FeasibilityNotes { get; set; }

    // 6. System Fields
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
