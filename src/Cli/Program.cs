using Core;
using Core.Domain;
using Core.Dto;
using Core.Import;

Console.OutputEncoding = System.Text.Encoding.UTF8;

if (args.Contains("--info") || args.Contains("--env"))
{
    PrintEnvironmentInfo();
    return 0;
}

// Прапорець --convert: детальний вивід конвертації ImportResult у доменні сутності (за замовчуванням data/sample.csv)
if (args.Contains("--convert"))
{
    string convertPath = args.FirstOrDefault(a => a != "--" && !a.StartsWith('-')) ?? Path.Combine("data", "sample.csv");
    return RunConversionDemo(convertPath);
}

// Прапорець --extra: демонстрація додаткових завдань 2 і 3 ЛР4
if (args.Contains("--extra"))
{
    RunExtraTasksDemo();
    return 0;
}

// Якщо передано прапорець --domain або --lab4, або якщо CLI запущено без аргументів — виконуємо демонстрацію ЛР4
if (args.Contains("--domain") || args.Contains("--lab4") || args.Length == 0)
{
    RunDomainDemo();
    return 0;
}

// Пошук аргументу шляху до файлу імпорту: пропускаємо розділювач "--" та будь-які прапорці "-"
string? inputPath = args.FirstOrDefault(a => a != "--" && !a.StartsWith('-'));

if (!string.IsNullOrEmpty(inputPath))
{
    if (!File.Exists(inputPath))
    {
        Console.WriteLine($"Помилка: файл не знайдено за шляхом: {Path.GetFullPath(inputPath)}");
        return 1;
    }

    string fileName = Path.GetFileName(inputPath).ToLowerInvariant();
    string extension = Path.GetExtension(inputPath).ToLowerInvariant();

    switch (extension)
    {
        case ".csv" when fileName.Contains("mixed"):
            ProcessMixedCsv(inputPath);
            break;

        case ".csv":
            ProcessProductImport(ProductCsvImporter.Load(inputPath), "CSV");
            break;

        case ".json":
            ProcessProductImport(ProductJsonImporter.Load(inputPath), "JSON");
            break;

        default:
            Console.WriteLine($"Помилка: непідтримуваний формат файлу '{extension}'. Очікується .csv або .json");
            return 1;
    }
}

return 0;

static void RunDomainDemo()
{
    Console.WriteLine("=== ЛАБОРАТОРНА РОБОТА 4: ДЕМОНСТРАЦІЯ ДОМЕННОЇ МОДЕЛІ ===");
    Console.WriteLine();

    Console.WriteLine("--- Сценарій 1: Успішна робота з доменною сутністю ---");
    Product product = Product.Create("P-001", "sku-101", "Молоко пастеризоване 2.5%", "пак", 100);
    Console.WriteLine($"Створено об'єкт   : {product}");

    product.RegisterArrival(50);
    Console.WriteLine($"Після приходу (+50): {product}");

    product.Issue(110);
    Console.WriteLine($"Після видачі (-110): {product}"); // Потім буде статус LowStock (<50)

    // Демонстрація мапінгу ToDto та FromDto
    ProductDto dto = product.ToDto();
    Product restored = Product.FromDto(dto);
    Console.WriteLine($"Відновлено з DTO  : {restored}");
    Console.WriteLine();

    Console.WriteLine("--- Сценарій 2: Порушення інваріантів домену ---");
    TryDo("Видача більша за залишок", () => product.Issue(1000));
    TryDo("Прихід з від'ємною кількістю", () => product.RegisterArrival(-10));
    TryDo("Створення з порожнім SKU", () => Product.Create("P-002", "", "Хліб пшеничний", "шт", 10));
    TryDo("Створення з від'ємним залишком", () => Product.Create("P-003", "SKU-103", "Сир Гауда", "кг", -5));

    Console.WriteLine();
    Console.WriteLine($"Фінальний стан об'єкта не змінився після відмов: {product}");
    Console.WriteLine();

    Console.WriteLine("--- Сценарій 3 (Додаткове завдання): Конвертація DTO 3-го тижня у Домен ---");
    ImportResult<ProductDto> csvResult = ProductCsvImporter.Load(Path.Combine("data", "sample.csv"));
    var (domainProducts, domainErrors) = Product.CreateFromImport(csvResult);

    Console.WriteLine($"Успішно перетворено в доменні сутності: {domainProducts.Count} записів");
    Console.WriteLine($"Усього помилок (включаючи інваріанти): {domainErrors.Count}");
}

static void RunExtraTasksDemo()
{
    Console.WriteLine("=== ЛАБОРАТОРНА РОБОТА 4: ДОДАТКОВІ ЗАВДАННЯ 2 І 3 ===");
    Console.WriteLine();

    Console.WriteLine("--- Додаткове завдання 2: інваріант двох сутностей (Warehouse + Product) ---");
    var receiving = new StockReceivingService();
    Warehouse warehouse = Warehouse.Create("W-01", "Холодильний термінал", "Корпус Б", 500);
    Product milk = Product.Create("P-001", "SKU-101", "Молоко пастеризоване 2.5%", "пак", 100);
    Product cheese = Product.Create("P-003", "SKU-103", "Сир Гауда", "кг", 45);
    Product bread = Product.Create("P-002", "SKU-102", "Хліб пшеничний", "шт", 10);

    receiving.Place(warehouse, milk);
    receiving.Place(warehouse, cheese);
    Console.WriteLine($"Розміщено 2 товари      : {warehouse}");

    receiving.Receive(warehouse, milk, 200);
    Console.WriteLine($"Прихід молока (+200)    : {warehouse}");

    TryDo("Прихід понад місткість складу", () => receiving.Receive(warehouse, cheese, 300));
    TryDo("Повторне розміщення того ж SKU", () => receiving.Place(warehouse, milk));
    TryDo("Прихід товару, якого немає на складі", () => receiving.Receive(warehouse, bread, 10));

    Console.WriteLine($"Стан складу після відмов: {warehouse}");
    foreach (Product product in warehouse.Products)
    {
        Console.WriteLine($"  {product}");
    }

    Console.WriteLine();

    Console.WriteLine("--- Додаткове завдання 3: стан товару і допустимі переходи (switch expression) ---");
    Product yogurt = Product.Create("P-020", "SKU-120", "Йогурт полуничний 2.5%", "шт", 30);
    Console.WriteLine($"Створено                : {yogurt.Sku}, стан {yogurt.State}, залишок {yogurt.Quantity}");

    yogurt.Suspend();
    Console.WriteLine($"Suspend()               : стан {yogurt.State}");
    TryDo("Видача призупиненого товару", () => yogurt.Issue(5));
    TryDo("Повторне призупинення", () => yogurt.Suspend());

    yogurt.Resume();
    Console.WriteLine($"Resume()                : стан {yogurt.State}");
    TryDo("Зняття з обліку товару із залишком", () => yogurt.Discontinue());

    yogurt.Issue(30);
    yogurt.Discontinue();
    Console.WriteLine($"Issue(30) + Discontinue(): стан {yogurt.State}, залишок {yogurt.Quantity}");

    TryDo("Повернення знятого з обліку товару", () => yogurt.Resume());
    TryDo("Прихід на знятий з обліку товар", () => yogurt.RegisterArrival(10));
}

static int RunConversionDemo(string path)
{
    if (!File.Exists(path))
    {
        Console.WriteLine($"Помилка: файл не знайдено за шляхом: {Path.GetFullPath(path)}");
        return 1;
    }

    ImportResult<ProductDto> importResult = Path.GetExtension(path).ToLowerInvariant() == ".json"
        ? ProductJsonImporter.Load(path)
        : ProductCsvImporter.Load(path);

    var (products, errors) = Product.CreateFromImport(importResult);

    // CreateFromImport спочатку копіює помилки імпортера, а потім додає помилки інваріантів
    int importErrorCount = importResult.Errors.Count;
    IEnumerable<string> importErrors = errors.Take(importErrorCount);
    IEnumerable<string> invariantErrors = errors.Skip(importErrorCount);

    Console.WriteLine($"=== Конвертація ImportResult у доменні сутності: {Path.GetFileName(path)} ===");
    Console.WriteLine($"Записів після імпорту (DTO): {importResult.Items.Count}");
    Console.WriteLine();

    Console.WriteLine($"--- Доменні сутності Product: {products.Count} ---");
    foreach (Product product in products)
    {
        Console.WriteLine($"  {product}");
    }

    Console.WriteLine();
    Console.WriteLine($"--- Відхилено імпортером (розбір рядка, ЛР3): {importErrorCount} ---");
    foreach (string err in importErrors)
    {
        Console.WriteLine($"  ! {err}");
    }

    Console.WriteLine();
    Console.WriteLine($"--- Відхилено доменом (інваріанти Product, ЛР4): {errors.Count - importErrorCount} ---");
    foreach (string err in invariantErrors)
    {
        Console.WriteLine($"  ! {err}");
    }

    Console.WriteLine(new string('-', 68));
    Console.WriteLine($"[СТАТИСТИКА КОНВЕРТАЦІЇ] Сутностей: {products.Count} | Помилок імпорту: {importErrorCount} | Порушень інваріантів: {errors.Count - importErrorCount}");
    return 0;
}

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"  [ПОМИЛКА МОДЕЛІ] {title}: виняток НЕ спрацював, інваріант відсутній!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  [УСПІХ ІНВАРІАНТА] {title}: {ex.GetType().Name} — {ex.Message}");
    }
}

static void ProcessProductImport(ImportResult<ProductDto> result, string format)
{
    Console.WriteLine($"=== Результат імпорту товарів ({format}) ===");
    Console.WriteLine($"Завантажено записів: {result.Items.Count}");
    Console.WriteLine(new string('-', 68));
    Console.WriteLine($" {"ID",-6} {"Артикул",-10} {"Назва товару",-34} {"К-сть",6} {"Од.",-4}");
    Console.WriteLine(new string('-', 68));

    foreach (ProductDto p in result.Items.Take(5))
    {
        Console.WriteLine($" {p.Id,-6} {p.Sku,-10} {p.Name,-34} {p.Quantity,6} {p.Unit,-4}");
    }

    Console.WriteLine(new string('-', 68));

    if (result.Errors.Count > 0)
    {
        Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");
        foreach (string err in result.Errors)
        {
            Console.WriteLine($" ! {err}");
        }
    }

    int accepted = result.Items.Count;
    int rejected = result.Errors.Count;
    int total = accepted + rejected;
    double rejectedPct = total > 0 ? (double)rejected / total * 100 : 0;
    double acceptedPct = total > 0 ? (double)accepted / total * 100 : 0;

    Console.WriteLine(new string('-', 68));
    Console.WriteLine($"[СТАТИСТИКА ІМПОРТУ] Усього: {total} | Прийнято: {accepted} ({acceptedPct:F1}%) | Пропущено: {rejected} ({rejectedPct:F1}%)");
}

static void ProcessMixedCsv(string path)
{
    MixedImportResult result = MixedCsvImporter.Load(path);
    Console.WriteLine("=== Результат імпорту поліморфного файлу (Товари + Склади) ===");
    Console.WriteLine($"Товарів завантажено: {result.Products.Count}");
    foreach (ProductDto p in result.Products)
    {
        Console.WriteLine($"  [ТОВАР] {p.Id} {p.Sku} {p.Name} ({p.Quantity} {p.Unit})");
    }

    Console.WriteLine($"\nСкладів завантажено: {result.Warehouses.Count}");
    foreach (WarehouseDto w in result.Warehouses)
    {
        Console.WriteLine($"  [СКЛАД] {w.Id} {w.Name} ({w.Location}, місткість: {w.Capacity})");
    }

    if (result.Errors.Count > 0)
    {
        Console.WriteLine($"\nПропущено помилкових рядків: {result.Errors.Count}");
        foreach (string err in result.Errors)
        {
            Console.WriteLine($"  ! {err}");
        }
    }

    int accepted = result.Products.Count + result.Warehouses.Count;
    int rejected = result.Errors.Count;
    int total = accepted + rejected;
    double errorPct = total > 0 ? (double)rejected / total * 100 : 0;

    Console.WriteLine(new string('-', 68));
    Console.WriteLine($"[СТАТИСТИКА ЗМІШАНОГО ІМПОРТУ] Усього: {total} | Успішно: {accepted} | Помилок: {rejected} ({errorPct:F1}%)");
}

static void PrintEnvironmentInfo()
{
    EnvironmentReport report = EnvironmentInfo.Collect();
    Console.WriteLine("CrossApp - системна інформація середовища");
    Console.WriteLine("Студент            : Ціздин Роман, група ФЕІ-35");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"ОС (OSDescription) : {report.OsDescription}");
    Console.WriteLine($"ОС (Environment)   : {report.OsVersion}");
    Console.WriteLine($"Архітектура процесу: {report.ProcessArchitecture}");
    Console.WriteLine($"Версія .NET (CLR)  : {report.DotNetVersion}");
    Console.WriteLine($"Runtime            : {report.Runtime}");
    Console.WriteLine($"RID (визначено)    : {report.DetectedRid}");
    Console.WriteLine($"RID (від .NET)     : {report.ReportedRid}");
    Console.WriteLine($"Каталог застосунку : {report.AppDirectory}");
    Console.WriteLine($"Поточний каталог   : {report.CurrentDirectory}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"Предметна область  : {report.Domain}");
}