using System.Collections.Generic;
using System.IO;
using System.Net;
using mshtml;
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
            try
            {
                // Get URI of hi space
                var spaceLinkRegex = new Regex(
                    @"<A href=""(http://hi.baidu.com/[^""]+)"">我的主页</A>",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled);
                var htmlDoc = await this.NavigateAndGetHtmlDocumentWithCheck(this.webBrowserCrawler, Constants.HomeUri, d => spaceLinkRegex.IsMatch(d.DocumentNode.OuterHtml), null);
                if (htmlDoc == null)
                {
                    throw new Exception("Cannot navigate to home page");
                }
                var spaceLink = spaceLinkRegex.Match(htmlDoc.DocumentNode.OuterHtml).Groups[1].Value;
                var spaceUri = new Uri(spaceLink);

                // Get pages count
                var pageCountRegex = new Regex(
                    @"var PagerInfo = {\s+allCount : '(\d+)',\s+pageSize : '(\d+)',\s+curPage : '(\d+)'\s+};",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled);
                htmlDoc = await this.NavigateAndGetHtmlDocumentWithCheck(this.webBrowserCrawler, spaceUri, d => pageCountRegex.IsMatch(d.DocumentNode.OuterHtml), null);
                if (htmlDoc == null)
                {
                    throw new Exception("Cannot navigate to space page");
                }
                var match = pageCountRegex.Match(htmlDoc.DocumentNode.OuterHtml);
                int allCount;
                int pageSize;
                int totalPage = 1;
                if (match.Success  && int.TryParse(match.Groups[1].Value, out allCount) && int.TryParse(match.Groups[2].Value, out pageSize))
                {
                    totalPage = (allCount + pageSize - 1) / pageSize;
                }

                // Get articles on each page
                var articles = new List<Article>();
                for (int pageId = 1; pageId <= totalPage; pageId++)
                {
                    // Get article list
                    var pageUri = new Uri(spaceLink + "?page=" + pageId);
                    var articleNodesSelector = @"//a[@class=""a-incontent a-title cs-contentblock-hoverlink""]";
                    htmlDoc = await this.NavigateAndGetHtmlDocumentWithCheck(this.webBrowserCrawler, pageUri, d =>
                    {
                        var nodes = d.DocumentNode.SelectNodes(articleNodesSelector);
                        return nodes != null && nodes.Count > 0;
                    }, null);
                    if (htmlDoc == null)
                    {
                        continue;
                    }

                    var articleNodes = htmlDoc.DocumentNode.SelectNodes(articleNodesSelector);
                    if (articleNodes == null)
                    {
                        continue;
                    }

                    foreach (var articleNode in articleNodes)
                    {
                        // Get single article link
                        var articleLink = "http://hi.baidu.com" + articleNode.Attributes["href"].Value;
                        var articleUri = new Uri(articleLink);

                        // Regexes
                        var articleTitleSelector = @"//h2[@class=""title content-title""]";
                        var articleHtmlContentSelector = @"//div[@id=""content""]";

                        // Get article page text
                        htmlDoc = await this.NavigateAndGetHtmlDocumentWithCheck(this.webBrowserCrawler, articleUri,
                            d =>
                            {
                                var nodes = d.DocumentNode.SelectNodes(articleTitleSelector);
                                return nodes != null && nodes.Count > 0;
                            }, null);
                        if (htmlDoc == null)
                        {
                            continue;
                        }

                        // Get article
                        var article = new Article();
                        article.Title = htmlDoc.DocumentNode.SelectNodes(articleTitleSelector)[0].InnerText;
                        article.HtmlContent = htmlDoc.DocumentNode.SelectNodes(articleHtmlContentSelector)[0].InnerHtml;

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

                // Write to json file
                if (!Directory.Exists(Constants.LocalArchiveFolder))
                {
                    Directory.CreateDirectory(Constants.LocalArchiveFolder);
                }

                var spaceName = spaceLink.TrimEnd('/').Substring(spaceLink.TrimEnd('/').LastIndexOf('/') + 1);
                using (var fileStream = new FileStream(Path.Combine(Constants.LocalArchiveFolder, spaceName + ".json"), FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (var streamWriter = new StreamWriter(fileStream))
                    {
                        streamWriter.Write(JsonConvert.SerializeObject(articles));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Crawling Failed, exception: " + ex.Message);
                return;
            }

            MessageBox.Show("Crawling Finished!");
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

        private async Task<HtmlAgilityPack.HtmlDocument> NavigateAndGetHtmlDocumentWithCheck(
            System.Windows.Forms.WebBrowser webBrowser,
            Uri uri,
            Func<HtmlAgilityPack.HtmlDocument, bool> isSucceed,
            Func<HtmlAgilityPack.HtmlDocument, bool> isFailed)
        {
            if (isSucceed == null)
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

                var text = webBrowser.DocumentText;
                if (text == null)
                {
                    continue;
                }

                var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(text);

                if (isSucceed(htmlDoc))
                {
                    return htmlDoc;
                }

                if (isFailed != null && isFailed(htmlDoc))
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
