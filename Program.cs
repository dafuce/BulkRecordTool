using System;
using System.Net;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.IO;
class Program
{
    static int recordCount = 100000; // Number of records to generate
    static double faultyChance = 0.01; // 1% chance to make a card number faulty

    private static Dictionary<string, long> counters = new Dictionary<string, long>(); // To ensure unique card numbers per BIN 

    static string[] firstNames = { // TODO: let this and other hardcoded lists be read from a file
    "Alice",
    "Bob",
    "Charlie",
    "David",
    "Emma",
    "Frank",
    "Grace",
    "Henry",
    "Isabella",
    "Jack",
    "Karen",
    "Liam",
    "Mia",
    "Noah",
    "Olivia",
    "Paul",
    "Quinn",
    "Robert",
    "Sophia",
    "Thomas",
    "Uma",
    "Victor",
    "William",
    "Xavier",
    "Yara",
    "Zoe"
};

    static string[] lastNames = {
    "Anderson",
    "Brown",
    "Clark",
    "Davis",
    "Evans",
    "Foster",
    "Garcia",
    "Harris",
    "Ingram",
    "Johnson",
    "King",
    "Lewis",
    "Miller",
    "Nelson",
    "Owens",
    "Parker",
    "Quinn",
    "Roberts",
    "Smith",
    "Taylor",
    "Underwood",
    "Vargas",
    "Williams",
    "Xiong",
    "Young",
    "Zimmerman"
};

    static string[] countries = {
    "India",
    "China",
    "United States of America",
    "Indonesia",
    "Pakistan",
    "Nigeria",
    "Brazil",
    "Bangladesh",
    "Russian Federation",
    "Ethiopia",
    "Mexico",
    "Japan",
    "Egypt",
    "Philippines",
    "Democratic Republic of the Congo",
    "Viet Nam",
    "Iran (Islamic Republic of)",
    "Türkiye",
    "Germany",
    "Thailand",
    "United Republic of Tanzania",
    "United Kingdom",
    "France",
    "South Africa",
    "Italy",
    "Kenya",
    "Myanmar",
    "Colombia",
    "Republic of Korea",
    "Sudan",
    "Uganda",
    "Spain",
    "Algeria",
    "Iraq",
    "Argentina",
    "Afghanistan",
    "Yemen",
    "Canada",
    "Angola",
    "Ukraine",
    "Morocco",
    "Poland",
    "Uzbekistan",
    "Malaysia",
    "Mozambique",
    "Ghana",
    "Peru",
    "Saudi Arabia",
    "Madagascar",
    "Côte d’Ivoire",
    "Nepal",
    "Venezuela",
    "Sri Lanka",
    "Cambodia",
    "Zimbabwe",
    "Rwanda",
    "Benin",
    "Tunisia",
    "Burundi",
    "Bolivia",
    "Belgium",
    "Cuba",
    "Haiti",
    "South Sudan",
    "Dominican Republic",
    "Greece",
    "Czech Republic",
    "Sweden",
    "Portugal",
    "Jordan",
    "Azerbaijan",
    "United Arab Emirates",
    "Hungary",
    "Honduras",
    "Belarus",
    "Tajikistan",
    "Israel",
    "Austria",
    "Papua New Guinea",
    "Switzerland",
    "Togo",
    "Sierra Leone",
    "Hong Kong",
    "Laos",
    "Paraguay",
    "Libya",
    "El Salvador",
    "Nicaragua",
    "Kyrgyzstan",
    "Bulgaria",
    "Serbia",
    "Lebanon",
    "Turkmenistan",
    "Singapore",
    "Denmark",
    "Finland",
    "Slovakia",
    "Norway",
    "Oman",
    "Palestine State"
};

    static Dictionary<string,string> cardBins = new Dictionary<string, string>
{
    { "Visa Classic Consumer", "40000001" },
    { "Visa Gold Consumer", "40120022" },
    { "Visa Platinum Consumer", "40230033" },
    { "Visa Infinite Consumer", "40340044" },
    { "Visa Gold Business", "40450055" },
    { "Visa Platinum Business", "40560066" },
    { "Visa Corporate Card", "40670077" },

    { "Mastercard Standard Consumer", "51000001" },
    { "Mastercard Gold Consumer", "52010022" },
    { "Mastercard Platinum Consumer", "53020033" },
    { "Mastercard World Elite Consumer", "54030044" },
    { "Mastercard Gold Business", "55040055" },
    { "Mastercard Platinum Business", "22230045" }, // 2-series
    { "Mastercard Corporate Card", "27200099" },    // upper end of 2-series

    { "American Express Green Consumer", "34000012" },
    { "American Express Gold Consumer", "37010034" },
    { "American Express Platinum Consumer", "37120056" },

    { "Discover Cashback Consumer", "60110078" },
    { "UnionPay Platinum Consumer", "62000011" },
    { "UnionPay Business Card", "62123456" }
};


    static void Main(string[] args)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew(); // Start timing

        Console.WriteLine($"Generating {recordCount} records...");

        // Console.WriteLine("Combinations possible :" + (firstNames.Length * lastNames.Length * countries.Length * cardBins.Count).ToString());

        List<Record> records = new List<Record>(recordCount);

        for (int i = 0; i < recordCount; i++)
        {
            Record record = GenerateRecord();

            records.Add(record);

            // Console.WriteLine($"{record.FirstName},{record.LastName},{record.Country},{record.CardType},{record.CardNumber},{record.ExpiryDate},{record.CVV}");
        }

        // Write to JSON file

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // To avoid escaping of special characters like ü,ô
        };

        string json = JsonSerializer.Serialize(records, options);

        string fileName = "TestData_" + recordCount.ToString()+ "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".json";

        string projectRoot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..");
        projectRoot = Path.GetFullPath(projectRoot);

        string filePath = Path.Combine(projectRoot, fileName);

        File.WriteAllText(filePath, json, Encoding.UTF8);

        var fileInfo = new FileInfo(filePath);
        long fileSizeInBytes = fileInfo.Length;

        Console.WriteLine($"Data written to {filePath}. \n File size: {ToReadableSize(fileSizeInBytes)} bytes");

        stopwatch.Stop(); // Stop timing

        Console.WriteLine($"Elapsed time: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
    }
    
    public static string ToReadableSize(long bytes)
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

    private static Record GenerateRecord()
    {
        var rand = new Random();

        string firstName = firstNames[rand.Next(firstNames.Length)];
        string lastName = lastNames[rand.Next(lastNames.Length)];
        string country = countries[rand.Next(countries.Length)];

        var cardTypeEntry = cardBins.ElementAt(rand.Next(cardBins.Count));
        string cardType = cardTypeEntry.Key;
        string cardBin = cardTypeEntry.Value;

        string cardNumber = GenerateCardNumber(cardBin);
        string expiryDate = GenerateExpiryDate(rand);
        string cvv = rand.Next(100, 999).ToString("D3");

        var record = new Record
        {
            FirstName = firstName,
            LastName = lastName,
            Country = country,
            CardType = cardType,
            CardNumber = cardNumber,
            ExpiryDate = expiryDate,
            CVV = cvv
        };
        return record;

    }

    private static string GenerateExpiryDate(Random rand)
    {
        Random rand2 = new Random();

        bool makeFaulty = rand2.NextDouble() < faultyChance;

        int month = rand.Next(1, 13);

        if (makeFaulty)
        {
            month = rand.Next(13, 99); // Invalid month
        }

        int year = DateTime.Now.Year + rand.Next(1, 6); // 1 to 5 years in the future
        return $"{month.ToString("D2")}/{(year % 100).ToString("D2")}";
    }
    private static string GenerateCardNumber(string bin)
    {
        int length = 16;

        if (bin.StartsWith("34") || bin.StartsWith("37")) length = 15;
        else if (bin.StartsWith("62")) length = 19;

        int randomLength = length - bin.Length - 1;

        if (!counters.ContainsKey(bin)) counters[bin] = 0;
        long counterValue = counters[bin]++;
        string randomPart = counterValue.ToString().PadLeft(randomLength, '0');

        string partialCardNumber = bin + randomPart;
        int checkDigit = CalculateLuhnCheckDigit(partialCardNumber);
        return partialCardNumber + checkDigit.ToString();
    }

    private static int CalculateLuhnCheckDigit(string number)
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

    private class Record
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Country { get; set; }
        public required string CardType { get; set; }
        public required string CardNumber { get; set; }
        public required string ExpiryDate { get; set; }
        public required string CVV { get; set; }
    }
}