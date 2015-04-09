using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BaiduHiCrawler
{
    /// <summary>
    /// Interaction logic for ArticleWindow.xaml
    /// </summary>
    public partial class ArticleWindow : Window
    {
        private Article article;

        private System.Windows.Forms.WebBrowser webBrowserCrawler;

        public ArticleWindow()
        {
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
            if (this.article == null)
            {
                return;
            }

            this.Title = string.Format("Article: {0}", this.article.Title);
            this.webBrowserCrawler.DocumentText = this.article.HtmlContent;

            this.ListBoxComments.Items.Clear();
            foreach (var comment in this.article.Comments)
            {
                this.ListBoxComments.Items.Add(
                    string.Format(
                        "{0} @ {1}:{2}{3}",
                        comment.Author,
                        TimeZoneInfo.ConvertTime(comment.Timestamp, TimeZoneInfo.Utc, TimeZoneInfo.Local),
                        Environment.NewLine,
                        comment.Content));
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }
    }
}
