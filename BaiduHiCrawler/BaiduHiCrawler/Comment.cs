namespace BaiduHiCrawler
{
    using System;

    using Newtonsoft.Json;

    public class Comment
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Json { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Author { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime Timestamp { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Content { get; set; }
    }
}
