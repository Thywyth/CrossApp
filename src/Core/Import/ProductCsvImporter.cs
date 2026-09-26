using System.Globalization;
using System.Text;
using Core.Dto;

namespace Core.Import;

public static class ProductCsvImporter
{
    // Роздільник — крапка з комою: запобігає конфліктам із комами в назвах товарів
    private const char Separator = ';';

    public static ImportResult<ProductDto> Load(string path)
    {
        var items = new List<ProductDto>();
        var errors = new List<string>();

        // Читаємо файл явно в кодуванні UTF-8 для коректної кирилиці
        string[] lines = File.ReadAllLines(path, Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int lineNumber = i + 1;
            string line = lines[i];

            // Пропускаємо порожні рядки та рядки-коментарі
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            // Пропускаємо заголовок (без прив'язки до регістру id/ID)
            if (lineNumber == 1 && line.StartsWith("id", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            switch (ParseLine(line))
            {
                case ParseOk ok:
                    items.Add(ok.Value);
                    break;
                case ParseFailed failed:
                    errors.Add($"рядок {lineNumber}: {failed.Reason}");
                    break;
            }
        }

        return new ImportResult<ProductDto>(items, errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            // 1. Патерн властивості + реляційний патерн
            { Length: < 5 } => new ParseFailed($"очікую щонайменше 5 колонок, отримав {parts.Length}"),

            // 2. Патерн списку + константні патерни + логічний патерн or
            [_, "", ..] or [_, _, "", ..] => new ParseFailed("SKU або назва є порожніми"),

            // 3. Патерн списку + зріз (..) + охоронна умова when + out-параметр + InvariantCulture
            [_, _, _, _, var qty, ..] when !int.TryParse(qty, NumberStyles.Integer, CultureInfo.InvariantCulture, out int q) || q < 0
                => new ParseFailed($"кількість '{qty}' не є коректним невід'ємним цілим числом"),

            // 4. Патерн списку з 5 елементів (основний випадок без примітки)
            [var id, var sku, var name, var unit, var qty]
                => new ParseOk(new ProductDto(id, sku, name, unit, int.Parse(qty, CultureInfo.InvariantCulture))),

            // 5. Патерн списку з 6 елементів (випадок із приміткою Note)
            [var id, var sku, var name, var unit, var qty, var note]
                => new ParseOk(new ProductDto(id, sku, name, unit, int.Parse(qty, CultureInfo.InvariantCulture), string.IsNullOrWhiteSpace(note) ? null : note)),

            // 6. Гілка за замовчуванням (дискард _)
            _ => new ParseFailed($"занадто багато колонок: {parts.Length}")
        };
    }

    // Приватна ієрархія типів для представлення результату розбору (Discriminated Union pattern)
    private abstract record ParseOutcome;
    private sealed record ParseOk(ProductDto Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}