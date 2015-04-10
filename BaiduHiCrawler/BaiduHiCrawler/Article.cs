namespace BaiduHiCrawler
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class Article
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Title { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? Timestamp { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string HtmlContent { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<Comment> Comments { get; set; }

        public override string ToString()
        {
            return string.Format("{0} ({1} Comment{2})", this.Title, this.Comments.Count, this.Comments.Count == 1 ? "" : "s");
        }
    }
}
