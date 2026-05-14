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
            string inputDir = Path.Combine(projectRoot, "InputFile");
            string outputPath = Path.Combine(projectRoot, "OutputFile", "extracted_bodies.json");

            Console.WriteLine($"Путь проекта: {projectRoot}");
            Console.WriteLine($"Папка входных файлов: {inputDir}");
            Console.WriteLine($"Выходной файл: {outputPath}");
            Console.WriteLine();

            if (!Directory.Exists(inputDir))
            {
                Console.WriteLine("Ошибка: папка InputFile не найдена.");
                return;
            }

            string[] jsonFiles = Directory.GetFiles(inputDir, "*.json");

            if (jsonFiles.Length == 0)
            {
                Console.WriteLine("Ошибка: в папке InputFile нет JSON файлов.");
                return;
            }

            Console.WriteLine($"Найдено файлов: {jsonFiles.Length}");
            Console.WriteLine();

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = new List<OutputItem>();
            int skipped = 0;

            foreach (string inputPath in jsonFiles)
            {
                Console.WriteLine($"Обработка файла: {Path.GetFileName(inputPath)}");

                string json = File.ReadAllText(inputPath, Encoding.UTF8);

                List<SourceItem>? sourceItems = JsonSerializer.Deserialize<List<SourceItem>>(json, jsonOptions);

                if (sourceItems == null)
                {
                    Console.WriteLine($"  Предупреждение: не удалось прочитать JSON-массив из файла {Path.GetFileName(inputPath)}, пропускаем.");
                    continue;
                }

                int fileExtracted = 0;

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
                            fileExtracted++;
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

                Console.WriteLine($"  Извлечено из файла: {fileExtracted} записей");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            var writeOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string outputJson = JsonSerializer.Serialize(result, writeOptions);
            File.WriteAllText(outputPath, outputJson, Encoding.UTF8);

            Console.WriteLine();
            Console.WriteLine("Готово.");
            Console.WriteLine($"Итого извлечено записей: {result.Count}");
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