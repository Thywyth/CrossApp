using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Core;

Console.OutputEncoding = System.Text.Encoding.UTF8;

const string student = "Ціздин Роман, група ФЕІ-35";
EnvironmentReport report = EnvironmentInfo.Collect();

if (args.Contains("--json"))
{
    var jsonPayload = new
    {
        Student = student,
        Report = report
    };

    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
    };

    Console.WriteLine(JsonSerializer.Serialize(jsonPayload, options));
}
else
{
    Console.WriteLine("CrossApp - практикум з крос-платформного програмування");
    Console.WriteLine($"Студент            : {student}");
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