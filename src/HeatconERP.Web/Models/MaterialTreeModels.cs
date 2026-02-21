namespace HeatconERP.Web.Models;

public record MaterialTypeNodeDto(Guid Id, string Name, string? Description, IReadOnlyList<MaterialGradeNodeDto> Grades);

public record MaterialGradeNodeDto(string Grade, IReadOnlyList<MaterialVariantNodeDto> Variants);

public record MaterialVariantNodeDto(
    Guid Id,
    string SKU,
    string Grade,
    string Size,
    string Unit,
    decimal Received,
    decimal Available,
    decimal Reserved,
    decimal Consumed);


