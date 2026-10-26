namespace Core.Domain;

// Додаткове завдання 2: інваріант, що охоплює ДВІ сутності —
// «загальний залишок товарів на складі не перевищує місткість складу».
//
// Чому правило в сервісі, а не в сутності:
//  - Product не знає, на якому складі лежить і скільки там інших товарів;
//  - Warehouse не керує залишком товару: прихід — це метод Product.RegisterArrival;
//  - для перевірки потрібен стан обох об'єктів одночасно, тож її робить той,
//    хто координує операцію. На 5-му тижні такий сервіс отримає сховища
//    і завантажуватиме склад і товар за Id перед перевіркою.
public sealed class StockReceivingService
{
    // Розміщення нового товару на складі разом з його початковим залишком
    public void Place(Warehouse warehouse, Product product)
    {
        ArgumentNullException.ThrowIfNull(warehouse);
        ArgumentNullException.ThrowIfNull(product);

        warehouse.EnsureNotPlaced(product);
        EnsureFits(warehouse, product.Quantity);
        warehouse.Add(product);
    }

    // Прихід товару, який уже лежить на складі
    public void Receive(Warehouse warehouse, Product product, int amount)
    {
        ArgumentNullException.ThrowIfNull(warehouse);
        ArgumentNullException.ThrowIfNull(product);

        if (!warehouse.Contains(product))
            throw new InvalidOperationException($"Товар [{product.Sku}] не розміщено на складі {warehouse.Id}");

        EnsureFits(warehouse, amount);
        product.RegisterArrival(amount);
    }

    private static void EnsureFits(Warehouse warehouse, int amount)
    {
        if (amount > warehouse.FreeSpace)
            throw new InvalidOperationException(
                $"Склад {warehouse.Id} переповнено: потрібно місця на {amount}, вільно {warehouse.FreeSpace} з {warehouse.Capacity}");
    }
}
