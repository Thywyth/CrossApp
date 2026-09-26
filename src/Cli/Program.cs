using Core;
using Core.Dto;
using Core.Import;

Console.OutputEncoding = System.Text.Encoding.UTF8;

if (args.Contains("--info") || args.Contains("--env"))
{
    PrintEnvironmentInfo();
    return 0;
}

string path = args.Length > 0 && !args[0].StartsWith('-')
    ? args[0]
    : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Помилка: файл не знайдено за шляхом: {Path.GetFullPath(path)}");
    return 1;
}

string fileName = Path.GetFileName(path).ToLowerInvariant();
string extension = Path.GetExtension(path).ToLowerInvariant();

// Вибір стратегії імпорту через switch expression
switch (extension)
{
    case ".csv" when fileName.Contains("mixed"):
        ProcessMixedCsv(path);
        break;

    case ".csv":
        ProcessProductImport(ProductCsvImporter.Load(path), "CSV");
        break;

    case ".json":
        ProcessProductImport(ProductJsonImporter.Load(path), "JSON");
        break;

    default:
        Console.WriteLine($"Помилка: непідтримуваний формат файлу '{extension}'. Очікується .csv або .json");
        return 1;
}

return 0;

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

    // Додаткове завдання 3: Статистичний рядок
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