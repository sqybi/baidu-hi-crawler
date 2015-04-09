namespace BaiduHiCrawler
{
    using System.Collections.Generic;

    public class Article
    {
        public string Title { get; set; }
        
        public string HtmlContent { get; set; }

        public List<Comment> Comments { get; set; }

        public override string ToString()
        {
            return string.Format("{0} ({1} Comment{2})", this.Title, this.Comments.Count, this.Comments.Count == 1 ? "" : "s");
        }
    }
}
