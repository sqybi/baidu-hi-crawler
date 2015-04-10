namespace BaiduHiCrawler
{
    using System;
    using System.Globalization;
    using System.Windows;

    /// <summary>
    /// Interaction logic for ArticleWindow.xaml
    /// </summary>
    public partial class ArticleWindow : Window
    {
        private Article article;

        private System.Windows.Forms.WebBrowser webBrowserCrawler;

        public ArticleWindow()
        {
            Logger.LogVerbose("Initialize ArticleWindow");

            InitializeComponent();

            this.article = null;

            this.webBrowserCrawler = this.WindowsFormsHostBrowser.Child as System.Windows.Forms.WebBrowser;
            this.webBrowserCrawler.AllowNavigation = true;
        }

        public void LoadArticle(Article article)
        {
            this.article = article;
            this.LoadArticleInternal();
        }

        private void LoadArticleInternal()
        {
            Logger.LogInfo("ArticleWindow start to load article");
            Logger.LogVerbose(
                "Article: Title [{0}], HtmlContent [{1}], Comments Count [{2}]",
                this.article.Title,
                this.article.HtmlContent,
                this.article.Comments.Count);

            if (this.article == null)
            {
                Logger.LogWarning("Article to load is null, exit");
                return;
            }

            this.Title = string.Format(
                "Article [{0}{1}]: {2}",
                this.article.Id,
                this.article.Timestamp.HasValue
                    ? " @ "
                      + TimeZoneInfo.ConvertTime(this.article.Timestamp.Value, TimeZoneInfo.Utc, TimeZoneInfo.Local)
                            .ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                    : string.Empty,
                this.article.Title);
            this.webBrowserCrawler.DocumentText = this.article.HtmlContent ?? string.Empty;

            this.ListBoxComments.Items.Clear();
            if (this.article.Comments != null)
            {
                foreach (var comment in this.article.Comments)
                {
                    this.ListBoxComments.Items.Add(
                        string.Format(
                            "{0} @ {1}:{2}{3}",
                            comment.Author,
                            TimeZoneInfo.ConvertTime(comment.Timestamp, TimeZoneInfo.Utc, TimeZoneInfo.Local)
                                .ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                            Environment.NewLine,
                            comment.Content));
                }
            }

            Logger.LogInfo("ArticleWindow finished to load article");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Logger.LogVerbose("Close ArticleWindow");
            this.Hide();
            e.Cancel = true;
        }
    }
}
