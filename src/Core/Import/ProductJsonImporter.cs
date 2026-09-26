using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Core.Dto;

namespace Core.Import;

public static class ProductJsonImporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
    };

    public static ImportResult<ProductDto> Load(string path)
    {
        var errors = new List<string>();

        try
        {
            string json = File.ReadAllText(path);
            List<ProductDto> items = JsonSerializer.Deserialize<List<ProductDto>>(json, Options) ?? [];
            return new ImportResult<ProductDto>(items, errors);
        }
        catch (JsonException ex)
        {
            errors.Add($"Помилка структури JSON: {ex.Message}");
            return new ImportResult<ProductDto>([], errors);
        }
    }
}