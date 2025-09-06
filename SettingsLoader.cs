using System.Text.Json;
namespace BulkRecordTool
{
    public class SettingsLoader
    {
        public static Settings Load(string filePath)
        {
            if (!File.Exists(filePath))
            {

                using (File.Create(filePath)) { }
                var defaultSettings = new Settings();
                defaultSettings.recordCount = 1000;
                defaultSettings.maxRecordsPerFile = 1000;
                defaultSettings.faultyChance = 0.01;  
                defaultSettings.fileNameRoot = "TestData_";
                defaultSettings.fileNameTimestampFormat = "yyyyMMddHHmmss";
                defaultSettings.outputDirectory = ".";
                defaultSettings.outputFormat = "JSON"; // Options: XML, JSON
                defaultSettings.overwriteOutputFile = false;
                defaultSettings.compressOutputFile = false;
                defaultSettings.enableLogging = true;
                defaultSettings.logFile = "app.log";

                var defaultJson = System.Text.Json.JsonSerializer.Serialize(defaultSettings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, defaultJson);


                Logger.Instance.WriteLine($"Settings file not found. A default settings file has been created in {filePath}. Please review and update it as needed, then run again", filePath);
                throw new FileNotFoundException($"Settings file not found. A default settings file has been created in {filePath}. Please review and update it as needed, then run again", filePath);
            }

            var json = File.ReadAllText(filePath);
            var settings = System.Text.Json.JsonSerializer.Deserialize<Settings>(json);

            if (settings == null)
            {
                Logger.Instance.WriteLine("Failed to deserialize settings.");
                throw new InvalidOperationException("Failed to deserialize settings.");
            }

            return settings;
        }
    }
    public class Settings
    {
        public Settings()
        {
            // Default constructor
        }
        // Protect against too large record counts
        private int _recordCount;
        public int recordCount
        {
            get => _recordCount;
            set
            {
            if (value > 2_000_000)
            {
                Logger.Instance.WriteLine("recordCount exceeds 2,000,000. Limiting to 2,000,000.");
                _recordCount = 1_000_000;
            }
            else
            {
                _recordCount = value;
            }
            }
        }
        public int maxRecordsPerFile { get; set; }
        public double faultyChance { get; set; }
        public string fileNameRoot { get; set; }
        public string fileNameTimestampFormat { get; set; }
        public string outputDirectory { get; set; }
        public string outputFormat { get; set; } // Options: XML, JSON
        public bool overwriteOutputFile { get; set; }
        public bool compressOutputFile { get; set; }
        public bool enableLogging { get; set; }
        public string logFile { get; set; }
    }
}