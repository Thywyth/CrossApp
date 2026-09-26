using System.Globalization;
using System.Text;
using Core.Dto;

namespace Core.Import;

public sealed record MixedImportResult(
    IReadOnlyList<ProductDto> Products,
    IReadOnlyList<WarehouseDto> Warehouses,
    IReadOnlyList<string> Errors);

public static class MixedCsvImporter
{
    private const char Separator = ';';

    public static MixedImportResult Load(string path)
    {
        var products = new List<ProductDto>();
        var warehouses = new List<WarehouseDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path, Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int lineNum = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#') || line.StartsWith("type;", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

            switch (parts)
            {
                // Товар: префікс P (type;id;sku;name;unit;quantity;note)
                case ["P", var id, var sku, var name, var unit, var qty, ..]
                    when int.TryParse(qty, NumberStyles.Integer, CultureInfo.InvariantCulture, out int q) && q >= 0:
                    string? note = parts.Length > 6 && !string.IsNullOrWhiteSpace(parts[6]) ? parts[6] : null;
                    products.Add(new ProductDto(id, sku, name, unit, q, note));
                    break;

                // Склад: префікс W (type;id;name;location;capacity)
                case ["W", var id, var name, var loc, var cap]
                    when int.TryParse(cap, NumberStyles.Integer, CultureInfo.InvariantCulture, out int c) && c >= 0:
                    warehouses.Add(new WarehouseDto(id, name, loc, c));
                    break;

                // Помилкові структури
                case ["P", ..]:
                    errors.Add($"рядок {lineNum}: некоректні дані товару (перевірте к-сть та обов'язкові колонки)");
                    break;
                case ["W", ..]:
                    errors.Add($"рядок {lineNum}: некоректні дані складу (перевірте місткість)");
                    break;
                default:
                    errors.Add($"рядок {lineNum}: невідомий префікс запису або некоректний формат: '{parts[0]}'");
                    break;
            }
        }

        return new MixedImportResult(products, warehouses, errors);
    }
}