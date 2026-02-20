using System.Text.Json.Serialization;

namespace HeatconERP.API.Models;

public record LoginRequest(
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("password")] string? Password);
