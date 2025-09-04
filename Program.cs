using System;
using System.Net;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
class Program
{
    static int recordCount = 100000; // Number of records to generate
    static double faultyChance = 0.01; // 1% chance to make a card number faulty

    private static Dictionary<string, long> counters = new Dictionary<string, long>(); // To ensure unique card numbers per BIN 
    public static Random rand = new Random();
    static void Main(string[] args)
    {
        var stopwatch = Stopwatch.StartNew(); // Start timing

        Console.WriteLine("Loading sample data...");

        string projectRoot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..");
        projectRoot = Path.GetFullPath(projectRoot);

        string json = File.ReadAllText(Path.Combine(projectRoot,"sampledata.json"));
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

        // Initialize counters for each BIN
        foreach (Product p in products)
        {
            if (!counters.ContainsKey(p.bin!))
            {
                counters[p.bin!] = 0;
            }
        }

        // Console.WriteLine("Combinations possible :" + (firstNames.Length * lastNames.Length * addresses.Length).ToString());

        Console.WriteLine($"Generating {recordCount} records...");

        List<Record> records = new List<Record>(recordCount);

        for (int i = 0; i < recordCount; i++)
        {
            string firstName = firstnames[rand.Next(firstnames.Count)];
            string lastName = lastnames[rand.Next(lastnames.Count)];

            CardHolder cardHolder = new CardHolder(firstName, lastName, "", "");

            AddressBlock address = addresses[rand.Next(addresses.Count)];

            Product prod = products[rand.Next(products.Count)];
            Card card = new Card(prod.product!, prod.cardType!, prod.bin!); // Generate card with unique PAN, ExpiryDate, CVV

            Record record = new Record(
                cardHolder,
                address,
                card,
                null
            );

            records.Add(record);
        }

        // Write to JSON file

        string fileName = "TestData_" + recordCount.ToString() + "_" + DateTime.Now.ToString("yyyyMMddHHmmss");

        string filePath = Path.Combine(projectRoot, fileName);

        WriteRecordsToJson(records, filePath);
        
        stopwatch.Stop(); // Stop timing

        Console.WriteLine($"Elapsed time: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
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

        Console.WriteLine($"Data written to {filePath}. \n File size: {ToReadableSize(fileSizeInBytes)} bytes");
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
        public  string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? AddressLine3 { get; set; }
        public  string? PostalCode { get; set; }
        public  string? City { get; set; }
        public  string? Country { get; set; }
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
        public string ProductName { get; set; }
        public string CardType { get; set; }
        public string PAN { get; set; }
        public string ExpiryDate { get; set; }
        public string CVV { get; set; }

        public Card(string productname, string cardType, string bin)
        {
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
            int checkDigit = CalculateLuhnCheckDigit(partialPAN);
            return partialPAN + checkDigit.ToString();
        }
        private int CalculateLuhnCheckDigit(string number)
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
                return ((10 - (sum % 10)) % 10 + 1) % 10;  // Introduce a fault by returning an incorrect check digit while keeping it 0-9
            }
            return (10 - (sum % 10)) % 10;
        }
        private static string GenerateExpiryDate()
        {
            bool makeFaulty = rand.NextDouble() < faultyChance;

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
        public  CardHolder CardHolder { get; set; }
        public  AddressBlock Address { get; set; }
        public  Card Card { get; set; }
        public Misc? Misc { get; set; }

        public Record(CardHolder cardHolder, AddressBlock address, Card card, Misc? misc = null)
        {
            CardHolder = cardHolder;
            Address = address;
            Card = card;
            Misc = misc;
        }
    }
}