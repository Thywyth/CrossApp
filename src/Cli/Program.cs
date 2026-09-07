using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var sysInfo = new
{
    Student = "Ціздин Роман, група ФЕІ-35",
    OsDescription = RuntimeInformation.OSDescription,
    OsVersion = Environment.OSVersion.ToString(),
    ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
    DotNetVersion = Environment.Version.ToString(),
    Runtime = RuntimeInformation.FrameworkDescription,
    AppDirectory = AppContext.BaseDirectory,
    CurrentDirectory = Environment.CurrentDirectory,
    Domain = "Склад (товари, партії, залишки, переміщення)"
};

if (args.Contains("--json"))
{
    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
    };
    Console.WriteLine(JsonSerializer.Serialize(sysInfo, options));
}
else
{
    Console.WriteLine("CrossApp - практикум з крос-платформного програмування");
    Console.WriteLine($"Студент: {sysInfo.Student}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"ОС (OSDescription): {sysInfo.OsDescription}");
    Console.WriteLine($"ОС (Environment)  : {sysInfo.OsVersion}");
    Console.WriteLine($"Архітектура процесу: {sysInfo.ProcessArchitecture}");
    Console.WriteLine($"Версія .NET (CLR) : {sysInfo.DotNetVersion}");
    Console.WriteLine($"Runtime            : {sysInfo.Runtime}");
    Console.WriteLine($"Каталог застосунку: {sysInfo.AppDirectory}");
    Console.WriteLine($"Поточний каталог  : {sysInfo.CurrentDirectory}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"Предметна область: {sysInfo.Domain}");
}