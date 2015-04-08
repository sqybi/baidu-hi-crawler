namespace BaiduHiCrawler
{
    using System.Collections.Generic;

    class Article
    {
        public string Title { get; set; }
        
        public string HtmlContent { get; set; }

        public List<Comment> Comments { get; set; }
    }
}
