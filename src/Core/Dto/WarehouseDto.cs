namespace Core.Dto;

public sealed record WarehouseDto(
    string Id,
    string Name,
    string Location,
    int Capacity);