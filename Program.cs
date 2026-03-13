using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExtractingBodyFromFile;

internal class Program
{
    static void Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;

            string projectRoot = FindProjectRoot();
            string inputPath = Path.Combine(projectRoot, "InputFile", "sed_log.json");
            string outputPath = Path.Combine(projectRoot, "OutputFile", "extracted_bodies.json");

            Console.WriteLine($"Путь проекта: {projectRoot}");
            Console.WriteLine($"Входной файл: {inputPath}");
            Console.WriteLine($"Выходной файл: {outputPath}");
            Console.WriteLine();

            if (!File.Exists(inputPath))
            {
                Console.WriteLine("Ошибка: файл sed_log.json не найден в папке InputFile.");
                return;
            }

            string json = File.ReadAllText(inputPath, Encoding.UTF8);

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            List<SourceItem>? sourceItems = JsonSerializer.Deserialize<List<SourceItem>>(json, jsonOptions);

            if (sourceItems == null)
            {
                Console.WriteLine("Ошибка: не удалось прочитать JSON-массив.");
                return;
            }

            var result = new List<OutputItem>();
            int skipped = 0;

            foreach (var item in sourceItems)
            {
                if (string.IsNullOrWhiteSpace(item.Body))
                {
                    skipped++;
                    continue;
                }

                try
                {
                    BodyItem? bodyObject = JsonSerializer.Deserialize<BodyItem>(item.Body, jsonOptions);

                    if (bodyObject != null && !string.IsNullOrWhiteSpace(bodyObject.XmlData))
                    {
                        result.Add(new OutputItem
                        {
                            XmlData = bodyObject.XmlData
                        });
                    }
                    else
                    {
                        skipped++;
                    }
                }
                catch
                {
                    skipped++;
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            var writeOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string outputJson = JsonSerializer.Serialize(result, writeOptions);
            File.WriteAllText(outputPath, outputJson, Encoding.UTF8);

            Console.WriteLine("Готово.");
            Console.WriteLine($"Извлечено записей: {result.Count}");
            Console.WriteLine($"Пропущено записей: {skipped}");
            Console.WriteLine($"Результат сохранён в файл:");
            Console.WriteLine(outputPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Произошла ошибка:");
            Console.WriteLine(ex.ToString());
        }
    }

    static string FindProjectRoot()
    {
        string current = Directory.GetCurrentDirectory();

        while (!string.IsNullOrEmpty(current))
        {
            string csprojPath = Path.Combine(current, "ExtractingBodyFromFile.csproj");
            string inputDir = Path.Combine(current, "InputFile");
            string outputDir = Path.Combine(current, "OutputFile");

            if (File.Exists(csprojPath) || (Directory.Exists(inputDir) && Directory.Exists(outputDir)))
            {
                return current;
            }

            DirectoryInfo? parent = Directory.GetParent(current);
            if (parent == null)
                break;

            current = parent.FullName;
        }

        throw new DirectoryNotFoundException("Не удалось найти корень проекта.");
    }
}

public class SourceItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }
}

public class BodyItem
{
    [JsonPropertyName("xmlData")]
    public string? XmlData { get; set; }
}

public class OutputItem
{
    [JsonPropertyName("xmlData")]
    public string? XmlData { get; set; }
}