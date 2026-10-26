using Core.Dto;

namespace Core.Domain;

// Похідний статус запасу: не зберігається, а обчислюється із залишку
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

    // Стан життєвого циклу: читається звідусіль, змінюється лише методами Suspend / Resume / Discontinue
    public ProductState State { get; private set; } = ProductState.Active;

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
                errors.Add($"Помилка інваріанту для DTO [Id='{dto.Id}', SKU='{dto.Sku}']: {ex.Message}");
            }
        }

        return (products.AsReadOnly(), errors.AsReadOnly());
    }

    public void RegisterArrival(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Кількість приходу має бути більшою за нуль");

        EnsureActive("прихід");
        _quantity += amount;
    }

    public void Issue(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Кількість видачі має бути більшою за нуль");

        EnsureActive("видача");

        if (amount > _quantity)
            throw new InvalidOperationException($"Не можна видати {amount} {Unit}: поточний залишок товару [{Sku}] становить {_quantity} {Unit}");

        _quantity -= amount;
    }

    public ProductDto ToDto() => new(Id, Sku, Name, Unit, Quantity);

    public static Product FromDto(ProductDto dto) =>
        Create(dto.Id, dto.Sku, dto.Name, dto.Unit, dto.Quantity);

    // Додаткове завдання 3: зміна стану життєвого циклу з перевіркою допустимих переходів
    public void Suspend() => ChangeState(ProductState.Suspended);

    public void Resume() => ChangeState(ProductState.Active);

    public void Discontinue()
    {
        if (_quantity > 0)
            throw new InvalidOperationException($"Не можна зняти з обліку товар [{Sku}]: на складі ще {_quantity} {Unit}");

        ChangeState(ProductState.Discontinued);
    }

    private void ChangeState(ProductState target)
    {
        bool allowed = (State, target) switch
        {
            (ProductState.Active, ProductState.Suspended) => true,
            (ProductState.Suspended, ProductState.Active) => true,
            (ProductState.Active or ProductState.Suspended, ProductState.Discontinued) => true,
            _ => false
        };

        if (!allowed)
            throw new InvalidOperationException($"Недопустимий перехід стану товару [{Sku}]: {State} -> {target}");

        State = target;
    }

    private void EnsureActive(string operation)
    {
        if (State != ProductState.Active)
            throw new InvalidOperationException($"Операція «{operation}» неможлива: товар [{Sku}] у стані {State}");
    }

    public override string ToString() => $"{Id} [{Sku}] {Name} — {Quantity} {Unit} (Статус: {Status})";
}