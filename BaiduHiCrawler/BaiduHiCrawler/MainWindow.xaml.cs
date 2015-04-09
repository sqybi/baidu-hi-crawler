using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BaiduHiCrawler
{
    using System;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using Microsoft.Win32;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isLoaded;

        private bool isNavigationFailed;

        private System.Windows.Forms.WebBrowser webBrowserCrawler;

        private ArticleWindow articleWindow;

        public MainWindow()
        {
            InitializeComponent();
            this.articleWindow = new ArticleWindow();
            this.articleWindow.Hide();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.webBrowserCrawler = this.WindowsFormsHostBrowser.Child as System.Windows.Forms.WebBrowser;
            this.webBrowserCrawler.AllowNavigation = true;
        }

        private void ButtonNavigateToLoginPage_Click(object sender, RoutedEventArgs e)
        {
            this.webBrowserCrawler.Navigate(Constants.LoginUri);
        }

        private async void ButtonStartCrawling_Click(object sender, RoutedEventArgs e)
        {
            // Get URI of hi space
            var spaceLinkRegex = new Regex(
                @"<A href=""(http://hi.baidu.com/[^""]+)"">我的主页</A>",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var text = await this.NavigateWithRegexCheck(this.webBrowserCrawler, Constants.HomeUri, spaceLinkRegex, null);
            if (text == null)
            {
                return;
            }
            var spaceLink = spaceLinkRegex.Match(text).Groups[1].Value;
            var spaceUri = new Uri(spaceLink);

            // Get pages count
            var pageCountRegex = new Regex(
                @"<A class=last href=""" + spaceLink + @"\?page=(\d+)"">尾页</A>",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var pageCountSucceedRegex = new Regex(
                @"<DIV class=mod-pagerbar>",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
            text = await this.NavigateWithRegexCheck(this.webBrowserCrawler, spaceUri, pageCountSucceedRegex, null);
            if (text == null)
            {
                return;
            }
            var match = pageCountRegex.Match(text);
            int totalPage;
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out totalPage))
            {
                totalPage = 1;
            }

            // Get articles on each page
            var articles = new List<Article>();
            for (int pageId = 1; pageId <= totalPage; pageId++)
            {
                // Get article list
                var pageUri = new Uri(spaceLink + "?page=" + pageId);
                var pageContentRegex = new Regex(@"<A class=""a-incontent a-title cs-contentblock-hoverlink"" href=""([^""]+)"" target=_blank>[^<]*</A>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                var pageSucceedRegex = pageCountSucceedRegex;
                text = await this.NavigateWithRegexCheck(this.webBrowserCrawler, pageUri, pageSucceedRegex, null);
                if (text == null)
                {
                    continue;
                }

                var articleMatches = pageContentRegex.Matches(text);
                foreach (Match articleMatch in articleMatches)
                {
                    // Get single article link
                    var articleLink = "http://hi.baidu.com" + articleMatch.Groups[1].Value;
                    var articleUri = new Uri(articleLink);

                    // Regexes
                    var articleTitleRegex =
                        new Regex(
                            @"<h2 class=""title content-title"">([^<]*)</h2>",
                            RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    var articleHtmlContentRegex =
                        new Regex(
                            @"<div id=content class=""content mod-cs-content text-content clearfix"">(.*)</div>\s+<div class=""mod-tagbox clearfix"">",
                            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    // Get article page text
                    text = await this.NavigateWithRegexCheck(this.webBrowserCrawler, articleUri, articleTitleRegex, null);
                    if (text == null)
                    {
                        continue;
                    }

                    // Get article
                    var article = new Article();
                    article.Title = articleTitleRegex.Match(text).Groups[1].Value;
                    article.HtmlContent = articleHtmlContentRegex.Match(text).Groups[1].Value;
                    
                    // Get article comments (by json, not regex)
                    article.Comments = new List<Comment>();
                    var articleId = articleLink.TrimEnd('/').Substring(articleLink.TrimEnd('/').LastIndexOf('/') + 1);
                    var articleCommentJsonText = GetCommentJsonText(articleId, 0, 1);
                    var articleCommentJsonObject = JObject.Parse(articleCommentJsonText);
                    var articleCommentCount = articleCommentJsonObject["data"][0]["total_count"].Value<int>();

                    if (articleCommentCount > 0)
                    {
                        articleCommentJsonText = GetCommentJsonText(articleId, 0, articleCommentCount);
                        articleCommentJsonObject = JObject.Parse(articleCommentJsonText);
                        for (int commentId = 0; commentId < articleCommentCount; commentId++)
                        {
                            var comment = new Comment
                            {
                                Json = articleCommentJsonText,
                                Author = articleCommentJsonObject["data"][0]["items"][commentId]["un"].Value<string>(),
                                Timestamp = Constants.CommentBaseDateTime.AddSeconds(
                                    articleCommentJsonObject["data"][0]["items"][commentId]["cdatetime"].Value<int>()),
                                Content =
                                    articleCommentJsonObject["data"][0]["items"][commentId]["content"].Value<string>()
                            };

                            article.Comments.Add(comment);
                        }
                    }

                    // Add into result
                    articles.Add(article);
                }
            }

            var spaceName = spaceLink.TrimEnd('/').Substring(spaceLink.TrimEnd('/').LastIndexOf('/') + 1);
            using (var fileStream = new FileStream(@".\Archive\" + spaceName + ".json", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (var streamWriter = new StreamWriter(fileStream))
                {
                    streamWriter.Write(JsonConvert.SerializeObject(articles));
                }
            }
        }

        private void ButtonLoadFromLocal_Click(object sender, RoutedEventArgs e)
        {
            var archiveDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Archive");
            if (!Directory.Exists(archiveDirectory))
            {
                archiveDirectory = "";
            }

            var openFileDialog = new OpenFileDialog
                                     {
                                         DefaultExt = ".json",
                                         Filter = "JSON File (.json)|*.json",
                                         InitialDirectory = archiveDirectory
                                     };
            
            // Show open file dialog box
            var result = openFileDialog.ShowDialog();

            // Process open file dialog box results 
            if (!result.HasValue || result == false)
            {
                return;
            }

            string fileName = openFileDialog.FileName;
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            string jsonText;
            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var streamReader = new StreamReader(fileStream))
                {
                    jsonText = streamReader.ReadToEnd();
                }
            }

            var articles = JsonConvert.DeserializeObject<List<Article>>(jsonText);

            this.ListBoxCrawlResult.Items.Clear();
            foreach (var article in articles)
            {
                this.ListBoxCrawlResult.Items.Add(article);
            }
        }

        private string GetCommentJsonText(string articleId, int start, int count)
        {
            string articleJsonText = null;
            while (articleJsonText == null)
            {
                var request = WebRequest.Create(string.Format(Constants.CommentRetrivalUrlPattern, articleId, start, count));
                request.Timeout = 2000 * count;
                try
                {
                    using (var response = request.GetResponse())
                    {
                        using (var responseStream = response.GetResponseStream())
                        {
                            if (responseStream == null)
                            {
                                return null;
                            }

                            using (var streamReader = new StreamReader(responseStream))
                            {
                                articleJsonText = streamReader.ReadToEnd();
                            }
                        }
                    }
                    Thread.Sleep(500);
                }
                catch (WebException)
                {
                    Thread.Sleep(2000);
                }
            }

            var escapeRegex = new Regex(@"\\x([0-9a-fA-F][0-9a-fA-F])", RegexOptions.Compiled);
            articleJsonText = escapeRegex.Replace(articleJsonText, m => @"\\u0" + m.Groups[1].Value);

            return articleJsonText;
        }

        private async Task<string> NavigateWithRegexCheck(System.Windows.Forms.WebBrowser webBrowser, Uri uri, Regex succeedRegex, Regex failedRegex)
        {
            if (succeedRegex == null)
            {
                return null;
            }

            do
            {
                webBrowser.Navigate(uri);
                this.isLoaded = false;
                this.isNavigationFailed = false;
                webBrowser.Navigated += (sender, args) =>
                {
                    this.isLoaded = true;
                };
                for (int i = 0; i < 10; i++)
                {
                    if (this.isLoaded)
                    {
                        break;
                    }
                    await Task.Delay(1000);
                }
            } while (!this.isLoaded);

            for (int i = 0; i < 60; i++)
            {
                await Task.Delay(1000);

                var htmlDoc = webBrowser.Document;
                if (htmlDoc == null)
                {
                    continue;
                }

                var htmlBody = htmlDoc.Body;
                if (htmlBody == null)
                {
                    continue;
                }

                var text = htmlBody.OuterHtml;
                if (succeedRegex.Match(text).Success)
                {
                    return text;
                }

                if (failedRegex != null && failedRegex.Match(text).Success)
                {
                    return null;
                }
            }

            return null;
        }

        private void ListBoxCrawlResult_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DependencyObject obj = (DependencyObject)e.OriginalSource;

            while (obj != null && obj != this.ListBoxCrawlResult)
            {
                if (obj.GetType() == typeof(ListBoxItem))
                {
                    var item = obj as ListBoxItem;
                    if (item == null)
                    {
                        return;
                    }

                    var article = item.Content as Article;
                    if (article == null)
                    {
                        return;
                    }

                    this.articleWindow.LoadArticle(article);
                    this.articleWindow.Show();
                }
                obj = VisualTreeHelper.GetParent(obj);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
