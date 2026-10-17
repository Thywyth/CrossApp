using Core.Dto;

namespace Core.Domain;

// Додаткове завдання 3: Явний стан-перелічування для сутності
public enum StockStatus
{
    OutOfStock,
    LowStock,
    InStock
}

public sealed class Product
{
    private int _quantity;

    public string Id { get; }
    public string Sku { get; }
    public string Name { get; }
    public string Unit { get; }

    public int Quantity => _quantity;

    // Властивість-вираз зі switch expression для визначення статусу за залишком
    public StockStatus Status => _quantity switch
    {
        0 => StockStatus.OutOfStock,
        < 50 => StockStatus.LowStock,
        _ => StockStatus.InStock
    };

    private Product(string id, string sku, string name, string unit, int quantity)
    {
        Id = id;
        Sku = sku;
        Name = name;
        Unit = unit;
        _quantity = quantity;
    }

    public static Product Create(string id, string sku, string name, string unit, int quantity)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор обов'язковий і не може бути порожнім", nameof(id));

        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU не може бути порожнім", nameof(sku));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва товару не може бути порожньою", nameof(name));

        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("Одиниця виміру обов'язкова", nameof(unit));

        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Початковий залишок не може бути від'ємним");

        return new Product(
            id.Trim(),
            sku.Trim().ToUpperInvariant(),
            name.Trim(),
            unit.Trim(),
            quantity);
    }

    // Додаткове завдання 1: Метод конвертації ImportResult у список доменних сутностей
    public static (IReadOnlyList<Product> Products, IReadOnlyList<string> Errors) CreateFromImport(ImportResult<ProductDto> importResult)
    {
        List<Product> products = [];
        List<string> errors = [.. importResult.Errors];

        foreach (ProductDto dto in importResult.Items)
        {
            try
            {
                products.Add(FromDto(dto));
            }
            catch (Exception ex)
            {
                errors.Add($"Помилка інваріанту для DTO [{dto.Id}]: {ex.Message}");
            }
        }

        return (products.AsReadOnly(), errors.AsReadOnly());
    }

    public void RegisterArrival(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Кількість приходу має бути більшою за нуль");

        _quantity += amount;
    }

    public void Issue(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Кількість видачі має бути більшою за нуль");

        if (amount > _quantity)
            throw new InvalidOperationException($"Не можна видати {amount} {Unit}: поточний залишок товару [{Sku}] становить {_quantity} {Unit}");

        _quantity -= amount;
    }

    public ProductDto ToDto() => new(Id, Sku, Name, Unit, Quantity);

    public static Product FromDto(ProductDto dto) =>
        Create(dto.Id, dto.Sku, dto.Name, dto.Unit, dto.Quantity);

    public override string ToString() => $"{Id} [{Sku}] {Name} — {Quantity} {Unit} (Статус: {Status})";
}