using Core.Dto;

namespace Core.Domain;

public sealed class Warehouse
{
    private readonly List<Product> _products = [];

    public string Id { get; }
    public string Name { get; }
    public string Location { get; }
    public int Capacity { get; }

    // Назовні — лише представлення для читання: додати товар в обхід перевірок неможливо
    public IReadOnlyList<Product> Products => _products.AsReadOnly();

    public int TotalQuantity => _products.Sum(p => p.Quantity);

    public int FreeSpace => Capacity - TotalQuantity;

    private Warehouse(string id, string name, string location, int capacity)
    {
        Id = id;
        Name = name;
        Location = location;
        Capacity = capacity;
    }

    public static Warehouse Create(string id, string name, string location, int capacity)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор складу обов'язковий", nameof(id));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва складу не може бути порожньою", nameof(name));

        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Місткість складу має бути більшою за нуль");

        return new Warehouse(id.Trim(), name.Trim(), location?.Trim() ?? string.Empty, capacity);
    }

    public bool Contains(Product product) => _products.Contains(product);

    // internal: розміщувати товари може лише код Core (StockReceivingService), а не Cli,
    // бо місткість — правило двох сутностей і перевіряється в сервісі
    internal void Add(Product product)
    {
        EnsureNotPlaced(product);
        _products.Add(product);
    }

    // Власне правило складу: один SKU — одна позиція
    internal void EnsureNotPlaced(Product product)
    {
        if (_products.Any(p => p.Sku == product.Sku))
            throw new InvalidOperationException($"Товар [{product.Sku}] вже розміщено на складі {Id}");
    }

    public WarehouseDto ToDto() => new(Id, Name, Location, Capacity);

    public static Warehouse FromDto(WarehouseDto dto) =>
        Create(dto.Id, dto.Name, dto.Location, dto.Capacity);

    public override string ToString() => $"{Id} {Name} — зайнято {TotalQuantity} з {Capacity} (вільно {FreeSpace})";
}
