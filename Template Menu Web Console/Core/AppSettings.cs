namespace EmilsWork.EmilsCMS
{
    public class AppSettings
    {
        // MongoDB connection settings
        public bool MongoEnabled { get; set; } = false;
        public string? MongoHost { get; set; }
        public int MongoPort { get; set; } = 27017;
        public string? MongoUser { get; set; }
        public string? MongoDbPassword { get; set; }
        public string? MongoDatabase { get; set; }
        public string? MongoCollection { get; set; }
        // Add other settings as needed
    }
}
