using System;
using System.Net;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;

using BulkRecordTool;

class Program
{
    static string projectRoot;
    static Settings settings;
    private static Dictionary<string, long> counters = new Dictionary<string, long>();
    public static Random rand = new Random();
    public static int faultyRecords = 0; // To track number of faulty records generated
    static void Main(string[] args)
    {
        var proc = Process.GetCurrentProcess(); // For memory usage tracking

        Logger.Instance.WriteLine("Process start. Loading settings...");

        projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        string settingsPath = Path.Combine(projectRoot, "settings.json");
        settings = SettingsLoader.Load(settingsPath);

        if (settings.enableLogging)
        {
            Logger.Instance.SetLogFile(Path.Combine(projectRoot, settings.logFile));
        }
        Logger.Instance.WriteLine("Settings loaded.");
        var stopwatch = Stopwatch.StartNew(); // Start timing


        string fileName = settings.fileNameRoot + "_" + settings.recordCount.ToString() + "_" + DateTime.Now.ToString(settings.fileNameTimestampFormat);

        if (!Directory.Exists(settings.outputDirectory))
        {
            Directory.CreateDirectory(settings.outputDirectory);
        }

        string json = File.ReadAllText(Path.Combine(projectRoot, "sampledata.json"));
        using JsonDocument doc = JsonDocument.Parse(json);

        // Extract simple arrays
        var firstnames = doc.RootElement
            .GetProperty("firstnames")
            .EnumerateArray()
            .Select(x => x.GetString()!)
            .ToList();

        var lastnames = doc.RootElement
            .GetProperty("lastnames")
            .EnumerateArray()
            .Select(x => x.GetString()!)
            .ToList();

        // Deserialize to List<AddressBlock>
        var addressesJson = doc.RootElement.GetProperty("addresses");
        var addresses = JsonSerializer
                        .Deserialize<List<AddressBlock>>(addressesJson.GetRawText())!
                        .ToList();

        // Deserialize to List<Products>
        var productsJson = doc.RootElement.GetProperty("products");
        var products = JsonSerializer
                        .Deserialize<List<Product>>(productsJson.GetRawText())!
                        .ToList();

        Logger.Instance.WriteLine("Sample data loaded.");

        // Initialize counters for each BIN
        foreach (Product p in products)
        {
            if (!counters.ContainsKey(p.bin!))
            {
                counters[p.bin!] = 0;
            }
        }

        int totalFiles = settings.recordCount % settings.maxRecordsPerFile == 0
        ? settings.recordCount / settings.maxRecordsPerFile
        : settings.recordCount / settings.maxRecordsPerFile + 1;

        if (totalFiles > 100)
        {
            throw new Exception ("Warning: Generating more than 100 files. Consider increasing maxRecordsPerFile to reduce the number of files.");
        }

        Logger.Instance.WriteLine($"Calculating {settings.recordCount.ToString("N0")} records into {totalFiles} file/s...");

        // Logger.Instance.WriteLine("Combinations possible :" + (firstNames.Length * lastNames.Length * addresses.Length).ToString());

        for (int i = 0; i < totalFiles; i++)
        {
            // Generate records for this file
            List<Record> records = new List<Record>(settings.maxRecordsPerFile);

            int recordsInThisFile = Math.Min(
                settings.maxRecordsPerFile,
                settings.recordCount - i * settings.maxRecordsPerFile
            );

            for (int j = 0; j < recordsInThisFile; j++)
            {
                string firstName = firstnames[rand.Next(firstnames.Count)];
                string lastName = lastnames[rand.Next(lastnames.Count)];

                CardHolder cardHolder = new CardHolder(firstName, lastName, "", "");

                AddressBlock address = addresses[rand.Next(addresses.Count)];

                Product prod = products[rand.Next(products.Count)];
                Card card = new Card(
                    firstName + " " + lastName,
                    prod.product!,
                    prod.cardType!,
                    prod.bin!
                );

                Record record = new Record(
                    (i * settings.maxRecordsPerFile + j).ToString().PadLeft(8, '0'), // global unique ID
                    cardHolder,
                    address,
                    card,
                    null
                );

                records.Add(record);
            }

            Logger.Instance.WriteLine($"Generated {records.Count.ToString("N0")} records with {faultyRecords} faulty records.");

            // Write each chunk to a separate file

            string filename = $"{settings.fileNameRoot}_{recordsInThisFile}_{DateTime.Now.ToString(settings.fileNameTimestampFormat)}_{i + 1}";
            string filePath = Path.Combine(settings.outputDirectory, filename);

            if (settings.outputFormat.ToUpper() == "JSON")
            {
                WriteRecordsToJson(records, filePath);
            }
            else if (settings.outputFormat.ToUpper() == "XML")
            {
                // WriteRecordsToXml(records, filePath);
            }
            else
            {
                Logger.Instance.WriteLine($"Unsupported output format: {settings.outputFormat}. Supported formats are JSON and XML.");
            }

            faultyRecords = 0; // Reset for next file
        }

        stopwatch.Stop(); // Stop timing

        Logger.Instance.WriteLine($"Peak Memory Working Set: {ToReadableSize(proc.PeakWorkingSet64)}");

        Logger.Instance.WriteLine($"Process ended. Elapsed time: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        
        // logger.Dispose(); // Ensure logger is disposed to flush all logs
        
    }
    private static void WriteRecordsToJson(List<Record> records, string filePath)
    {
        filePath += ".json";
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // To avoid escaping of special characters like ü,ô
        };

        string outJson = JsonSerializer.Serialize(records, options);

        File.WriteAllText(filePath, outJson, Encoding.UTF8);

        var fileInfo = new FileInfo(filePath);
        long fileSizeInBytes = fileInfo.Length;

        Logger.Instance.WriteLine($"Data written to {filePath}. ");
        Logger.Instance.WriteLine($"File size: {ToReadableSize(fileSizeInBytes)}");
    }
    private static string ToReadableSize(long bytes)
    {
        if (bytes < 0) throw new ArgumentOutOfRangeException(nameof(bytes), "Value must be non-negative.");

        string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
        int i = 0;
        double size = bytes;

        while (size >= 1024 && i < suffixes.Length - 1)
        {
            size /= 1024;
            i++;
        }

        return $"{size:0.##} {suffixes[i]}";
    }
    private class AddressBlock
    {
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? AddressLine3 { get; set; }
        public string? PostalCode { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
    }
    public class CardHolder
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? CompanyName { get; set; }
        public string? Language { get; set; }

        public CardHolder(string firstName, string lastName, string? companyName = null, string? language = null)
        {
            FirstName = firstName;
            LastName = lastName;
            CompanyName = companyName;
            Language = language;
        }
    }

    private class Product
    {
        public string? product { get; set; }
        public string? cardType { get; set; }
        public string? bin { get; set; }

    }

    private class Card
    {
        public string EmbossedName { get; set; }
        public string ProductName { get; set; }
        public string CardType { get; set; }
        public string PAN { get; set; }
        public string ExpiryDate { get; set; }
        public string CVV { get; set; }
        public string Track1 => $"%B{PAN}^{EmbossedName}^{ExpiryDate.Replace("/", "").Substring(2)}00000000000000000?;{PAN}={ExpiryDate.Replace("/", "").Substring(2)}00000000000000000?";
        public string Track2 => $";{PAN}={ExpiryDate.Replace("/", "").Substring(2)}00000000000000000?";

        public Card(string embossedname, string productname, string cardType, string bin)
        {
            EmbossedName = embossedname.ToUpper();
            ProductName = productname;
            CardType = cardType;
            PAN = GeneratePAN(bin);
            ExpiryDate = GenerateExpiryDate();
            CVV = rand.Next(100, 999).ToString("D3"); ;
        }
        private string GeneratePAN(string bin)
        {
            int length = 16;

            if (bin.StartsWith("34") || bin.StartsWith("37")) length = 15;
            else if (bin.StartsWith("62")) length = 19;

            int randomLength = length - bin.Length - 1;

            if (!counters.ContainsKey(bin)) counters[bin] = 0;
            long counterValue = counters[bin]++;
            string randomPart = counterValue.ToString().PadLeft(randomLength, '0');

            string partialPAN = bin + randomPart;
            int checkDigit = CalculateLuhnCheckDigit(partialPAN, settings.faultyChance);
            return partialPAN + checkDigit.ToString();
        }
        private int CalculateLuhnCheckDigit(string number, double faultyChance)
        {
            Random rand = new Random();

            bool makeFaulty = rand.NextDouble() < faultyChance;

            int sum = 0;
            bool alternate = true;
            for (int i = number.Length - 1; i >= 0; i--)
            {
                int n = int.Parse(number[i].ToString());
                if (alternate)
                {
                    n *= 2;
                    if (n > 9) n -= 9;
                }
                sum += n;
                alternate = !alternate;
            }
            if (makeFaulty)
            {
                faultyRecords++;
                return ((10 - (sum % 10)) % 10 + 1) % 10;  // Introduce a fault by returning an incorrect check digit while keeping it 0-9
            }
            return (10 - (sum % 10)) % 10;
        }
        private static string GenerateExpiryDate()
        {
            bool makeFaulty = rand.NextDouble() < settings.faultyChance;

            int month = rand.Next(1, 13);

            if (makeFaulty)
            {
                month = rand.Next(13, 99); // Invalid month
            }

            int year = DateTime.Now.Year + rand.Next(1, 6); // 1 to 5 years in the future
            return $"{month.ToString("D2")}/{(year % 100).ToString("D2")}";
        }
    }

    private class Misc
    {
        public string? BusinessField1 { get; set; }
        public string? BusinessField2 { get; set; }
        public string? BusinessField3 { get; set; }
        public string? BusinessField4 { get; set; }
        public string? BusinessField5 { get; set; }
    }
    private class Record
    {
        public string Id { get; set; }
        public CardHolder CardHolder { get; set; }
        public AddressBlock Address { get; set; }
        public Card Card { get; set; }
        public Misc? Misc { get; set; }

        public Record(string id,CardHolder cardHolder, AddressBlock address, Card card, Misc? misc = null)
        {
            Id = id;
            CardHolder = cardHolder;
            Address = address;
            Card = card;
            Misc = misc;
        }
    }
}