using System.Collections.Generic;
using System.IO;
using System.Net;
using mshtml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BaiduHiCrawler
{
    using System;
    using System.Globalization;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    using HtmlAgilityPack;

    using Microsoft.Win32;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Private Variables

        private bool isLoaded;

        private bool isNavigationFailed;

        private System.Windows.Forms.WebBrowser webBrowserCrawler;

        private ArticleWindow articleWindow;

        #endregion

        #region Ctor.

        public MainWindow()
        {
            Logger.LogVerbose("Initialize MainWindow");

            InitializeComponent();

            this.articleWindow = new ArticleWindow();
            this.articleWindow.Hide();
        }

        #endregion

        #region UI Event Handlers

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Logger.LogVerbose("Loaded MainWindow");

            this.webBrowserCrawler = this.WindowsFormsHostBrowser.Child as System.Windows.Forms.WebBrowser;
            if (this.webBrowserCrawler == null)
            {
                Logger.LogWarning("Cannot get WebBrowser for crawling");
            }
            else
            {
                this.webBrowserCrawler.AllowNavigation = true;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void ButtonNavigateToLoginPage_Click(object sender, RoutedEventArgs e)
        {
            Logger.LogInfo("Start navigating to login page");

            this.ButtonNavigateToLoginPage.IsEnabled = false;
            this.ButtonStartCrawling.IsEnabled = false;

            try
            {
                this.webBrowserCrawler.Navigate(Constants.LoginUri);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Navigating to login page failed. Exception: {0}", ex);

                return;
            }
            finally
            {
                this.ButtonNavigateToLoginPage.IsEnabled = true;
                this.ButtonStartCrawling.IsEnabled = true;
            }

            Logger.LogInfo("Finished navigating to login page");
        }

        private async void ButtonStartCrawling_Click(object sender, RoutedEventArgs e)
        {
            Logger.LogInfo("Start crawling");

            this.ButtonNavigateToLoginPage.IsEnabled = false;
            this.ButtonStartCrawling.IsEnabled = false;

            // Initialize
            var warningTriggered = false;

            try
            {
                // Get URI of hi space
                Logger.LogVerbose("Getting URL of personal space home page");

                var spaceLinkRegex = new Regex(
                    @"<A href=""(http://hi.baidu.com/[^""]+)"">我的主页</A>",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled);

                var htmlDoc =
                    await
                    this.NavigateAndGetHtmlDocumentWithCheck(
                        this.webBrowserCrawler,
                        Constants.HomeUri,
                        d => spaceLinkRegex.IsMatch(d.DocumentNode.OuterHtml),
                        null);
                if (htmlDoc == null)
                {
                    throw new Exception("Cannot get space home page URL");
                }

                var spaceLink = spaceLinkRegex.Match(htmlDoc.DocumentNode.OuterHtml).Groups[1].Value;
                var spaceUri = new Uri(spaceLink);

                // Get pages count
                Logger.LogVerbose("Getting total page count of space");

                var pageCountRegex =
                    new Regex(
                        @"var PagerInfo = {\s+allCount : '(\d+)',\s+pageSize : '(\d+)',\s+curPage : '(\d+)'\s+};",
                        RegexOptions.IgnoreCase | RegexOptions.Compiled);

                htmlDoc =
                    await
                    this.NavigateAndGetHtmlDocumentWithCheck(
                        this.webBrowserCrawler,
                        spaceUri,
                        d => pageCountRegex.IsMatch(d.DocumentNode.OuterHtml),
                        null);
                if (htmlDoc == null)
                {
                    throw new Exception("Cannot navigate to space page");
                }

                var match = pageCountRegex.Match(htmlDoc.DocumentNode.OuterHtml);
                int allCount;
                int pageSize;
                int totalPage = 1;
                if (match.Success && int.TryParse(match.Groups[1].Value, out allCount)
                    && int.TryParse(match.Groups[2].Value, out pageSize))
                {
                    totalPage = (allCount + pageSize - 1) / pageSize;
                }
                else
                {
                    Logger.LogWarning("Cannot get total page count of space, using count = 1 as default");
                }

                // Get articles on each page
                var articles = new List<Article>();
                for (int pageId = 1; pageId <= totalPage; pageId++)
                {
                    // Get article list
                    Logger.LogVerbose("Getting articles list on page {0}", pageId);

                    var pageUri = new Uri(spaceLink + "?page=" + pageId);
                    const string ArticleNodesSelector = @"//a[@class=""a-incontent a-title cs-contentblock-hoverlink""]";

                    htmlDoc = await this.NavigateAndGetHtmlDocumentWithCheck(
                        this.webBrowserCrawler,
                        pageUri,
                        d =>
                            {
                                var nodes = d.DocumentNode.SelectNodes(ArticleNodesSelector);
                                return nodes != null && nodes.Count > 0;
                            },
                        null);
                    if (htmlDoc == null)
                    {
                        Logger.LogWarning("Failed to retrieve articles list on page {0}", pageId);

                        warningTriggered = true;

                        continue;
                    }

                    var articleNodes = htmlDoc.DocumentNode.SelectNodes(ArticleNodesSelector); // articleNodes here should never be null

                    // Get articles
                    foreach (var articleNode in articleNodes)
                    {
                        // Get single article link
                        var articleLink = "http://hi.baidu.com" + articleNode.Attributes["href"].Value;
                        var articleId = articleLink.TrimEnd('/')
                            .Substring(articleLink.TrimEnd('/').LastIndexOf('/') + 1);
                        var articleUri = new Uri(articleLink);

                        Logger.LogVerbose("Getting article {0}", articleLink);

                        // Regexes
                        const string ArticleTitleSelector = @"//h2[@class=""title content-title""]";
                        const string ArticleTimestampSelector = @"//div[@class=""content-other-info""]/span";
                        const string ArticleHtmlContentSelector = @"//div[@id=""content""]";

                        // Get article page text
                        htmlDoc =
                            await this.NavigateAndGetHtmlDocumentWithCheck(
                                this.webBrowserCrawler,
                                articleUri,
                                d =>
                                    {
                                        var nodes = d.DocumentNode.SelectNodes(ArticleTitleSelector);
                                        return nodes != null && nodes.Count > 0;
                                    },
                                null);
                        if (htmlDoc == null)
                        {
                            Logger.LogWarning("Failed to retrieve article page {0}", articleLink);

                            warningTriggered = true;

                            continue;
                        }

                        try
                        {
                            // Get article
                            var article = new Article();

                            var articleTitleNodes = htmlDoc.DocumentNode.SelectNodes(ArticleTitleSelector);
                            var articleTimestampNodes = htmlDoc.DocumentNode.SelectNodes(ArticleTimestampSelector);
                            var articleHtmlContentNodes = htmlDoc.DocumentNode.SelectNodes(ArticleHtmlContentSelector);

                            article.Id = articleId;
                            article.Title = articleTitleNodes == null || articleTitleNodes.Count == 0
                                                ? null
                                                : articleTitleNodes[0].InnerText;
                            article.HtmlContent = articleHtmlContentNodes == null || articleHtmlContentNodes.Count == 0
                                                  ? null
                                                  : articleHtmlContentNodes[0].InnerHtml;

                            article.Timestamp = null;
                            if (articleTimestampNodes != null && articleTimestampNodes.Count > 0)
                            {
                                DateTime articleTimestamp;
                                if (
                                    DateTime.TryParseExact(
                                        articleTimestampNodes[0].InnerText,
                                        "yyyy-MM-dd HH:mm",
                                        CultureInfo.InvariantCulture,
                                        DateTimeStyles.AdjustToUniversal,
                                        out articleTimestamp))
                                {
                                    article.Timestamp = articleTimestamp.AddHours(-8); // Use UTC time
                                }
                            }

                            // Get article comments (by json, not regex)
                            article.Comments = new List<Comment>();
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
                                        Author =
                                            articleCommentJsonObject["data"][0]["items"][commentId][
                                                "un"].Value<string>(),
                                        Timestamp =
                                            Constants.CommentBaseDateTime.AddSeconds(
                                                articleCommentJsonObject["data"][0]["items"][
                                                    commentId]["cdatetime"].Value<int>()),
                                        Content =
                                            articleCommentJsonObject["data"][0]["items"][commentId][
                                                "content"].Value<string>()
                                    };

                                    article.Comments.Add(comment);
                                }
                            }

                            // Add into result
                            articles.Add(article);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning(
                                "Failed to get article {0}, Message: {1}, StackTrace: {2}",
                                articleLink,
                                ex.Message,
                                ex.StackTrace);

                            warningTriggered = true;

                            continue;
                        }
                    }
                }

                // Write to json file
                var spaceName = spaceLink.TrimEnd('/').Substring(spaceLink.TrimEnd('/').LastIndexOf('/') + 1);

                Logger.LogVerbose("Writing to file {0}.json", spaceName);

                if (!Directory.Exists(Constants.LocalArchiveFolder))
                {
                    Logger.LogInfo("Local archive folder not exist, creating: {0}", Constants.LocalArchiveFolder);

                    Directory.CreateDirectory(Constants.LocalArchiveFolder);
                }

                using (
                    var fileStream = new FileStream(
                        Path.Combine(Constants.LocalArchiveFolder, spaceName + ".json"),
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None))
                {
                    using (var streamWriter = new StreamWriter(fileStream))
                    {
                        streamWriter.Write(JsonConvert.SerializeObject(articles));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Crawling Failed, Message: {0}, StackTrace: {1}", ex.Message, ex.StackTrace);

                MessageBox.Show(
                    "Crawling failed. For more information, please refer to log file.",
                    "BaiduHiCrawler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }
            finally
            {
                this.ButtonNavigateToLoginPage.IsEnabled = true;
                this.ButtonStartCrawling.IsEnabled = true;
            }

            if (warningTriggered)
            {
                MessageBox.Show(
                    "Crawling finished with warning, some of the articles may lost during crawling. For more information, please refer to log file.",
                    "BaiduHiCrawler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show(
                    "Crawling finished!",
                    "BaiduHiCrawler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            Logger.LogInfo("Finished crawling");
        }

        private async void ButtonLoadFromLocal_Click(object sender, RoutedEventArgs e)
        {
            Logger.LogInfo("Start loading local JSON file");

            this.ButtonLoadFromLocal.IsEnabled = false;

            try
            {
                Logger.LogVerbose("Show open file dialog");

                // Prepare open file dialog
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

                // Show open file dialog
                var result = openFileDialog.ShowDialog();

                // Process open file dialog box results 
                if (!result.HasValue || result == false)
                {
                    Logger.LogInfo("No JSON file selected");

                    return;
                }

                string fileName = openFileDialog.FileName;
                if (string.IsNullOrEmpty(fileName))
                {
                    Logger.LogInfo("No JSON file selected");

                    return;
                }

                if (!File.Exists(fileName))
                {
                    Logger.LogWarning("JSON file not exist: {0}", fileName);

                    MessageBox.Show(
                        "File not exist!",
                        "BaiduHiCrawler",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                Logger.LogInfo("JSON file selected: {0}", fileName);

                // Load JSON file
                Logger.LogVerbose("Load JSON file: {0}", fileName);

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
            catch (Exception ex)
            {
                Logger.LogError(
                    "Failed to load JSON file from local, Message: {0}, StackTrace: {1}",
                    ex.Message,
                    ex.StackTrace);

                MessageBox.Show(
                   "Loading JSON file failed. For more information, please refer to log file.",
                   "BaiduHiCrawler",
                   MessageBoxButton.OK,
                   MessageBoxImage.Error);

                return;
            }
            finally
            {
                this.ButtonLoadFromLocal.IsEnabled = true;
            }

            MessageBox.Show(
                "Successfully loaded JSON file!",
                "BaiduHiCrawler",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Logger.LogInfo("Finished loading local JSON file");
        }

        private async void ListBoxCrawlResult_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var obj = (DependencyObject)e.OriginalSource;

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

                    Logger.LogInfo("Start loading article content: {0}", article);

                    this.articleWindow.LoadArticle(article);
                    this.articleWindow.Show();

                    Logger.LogInfo("Finished loading article content: {0}", article);
                }

                obj = VisualTreeHelper.GetParent(obj);
            }
        }

        #endregion

        #region Helper Methods

        private static string GetCommentJsonText(string articleId, int start, int count)
        {
            var requestUrl = string.Format(Constants.CommentRetrivalUrlPattern, articleId, start, count);
            string articleJsonText = null;

            Logger.LogInfo("Start getting JSON text for comments of article {0} from URL: {1}", articleId, requestUrl);

            while (articleJsonText == null)
            {
                // Build request
                var request = WebRequest.Create(requestUrl);
                request.Timeout = 2000 * count;

                try
                {
                    // Get response
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

                    // Wait for a short while in case requests sending too fast
                    Thread.Sleep(500);
                }
                catch (WebException ex)
                {
                    Logger.LogWarning(
                        "Failed to get JSON text for comments of article {0} from URL: {1}, retry after a while, Message: {2}, StackTrace: {3}",
                        articleId,
                        requestUrl,
                        ex.Message,
                        ex.StackTrace);

                    Thread.Sleep(2000);
                }
            }

            // Change escape \x to \u because Newtonsoft.JSON cannot deal with it
            var escapeRegex = new Regex(@"\\x([0-9a-fA-F][0-9a-fA-F])", RegexOptions.Compiled);
            articleJsonText = escapeRegex.Replace(articleJsonText, m => @"\\u0" + m.Groups[1].Value);

            Logger.LogInfo(
                "Finished getting JSON text for comments of article {0} from URL: {1}",
                articleId,
                requestUrl);

            return articleJsonText;
        }

        private async Task<HtmlAgilityPack.HtmlDocument> NavigateAndGetHtmlDocumentWithCheck(
            System.Windows.Forms.WebBrowser webBrowser,
            Uri uri,
            Func<HtmlAgilityPack.HtmlDocument, bool> isSucceed,
            Func<HtmlAgilityPack.HtmlDocument, bool> isFailed)
        {
            Logger.LogInfo("Start navigating to {0} and getting HTML document", uri.AbsoluteUri);

            // Check if should return null
            if (isSucceed == null)
            {
                Logger.LogInfo("Navigation to {} returns null becuase isSucceed is null");

                return null;
            }

            // Navigate to URI
            try
            {
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

                    try
                    {
                        var text = webBrowser.DocumentText;
                        if (text == null)
                        {
                            continue;
                        }

                        var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                        htmlDoc.LoadHtml(text);

                        if (isSucceed(htmlDoc))
                        {
                            Logger.LogInfo("Finished navigating to {0} and getting HTML document", uri.AbsoluteUri);

                            return htmlDoc;
                        }

                        if (isFailed != null && isFailed(htmlDoc))
                        {
                            Logger.LogWarning(
                                "Failed to navigate to {0} and getting HTML document because isFailed condition is matched",
                                uri.AbsoluteUri);

                            return null;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(
                            "Failed to navigate to {0} and getting HTML document, retry after a while, Message: {0}, StackTrace: {1}",
                            uri.AbsoluteUri,
                            ex.Message,
                            ex.StackTrace);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    "Failed to navigate to {0} and getting HTML document, Message: {1}, StackTrace: {2}",
                    uri.AbsoluteUri,
                    ex.Message,
                    ex.StackTrace);

                return null;
            }

            Logger.LogWarning(
                "Failed to navigate to {0} and getting HTML document because cannot satisfy isSucceed condition",
                uri.AbsoluteUri);

            return null;
        }

        #endregion
    }
}
