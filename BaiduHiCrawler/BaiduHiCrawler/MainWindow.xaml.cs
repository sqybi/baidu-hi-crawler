namespace BaiduHiCrawler
{
    using System;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ButtonNavigateToLoginPage_Click(object sender, RoutedEventArgs e)
        {
            this.WebBrowserCrawler.Navigate(Constants.LoginUri);
        }

        private async void ButtonStartCrawling_Click(object sender, RoutedEventArgs e)
        {
            string text;

            // Get URI of hi space
            var spaceLinkRegex = new Regex(
                @"<A href=""(http://hi.baidu.com/[^""]+)"">我的主页</A>",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
            if (!await this.NavigateWithRegexCheck(this.WebBrowserCrawler, Constants.HomeUri, spaceLinkRegex, null, out text))
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
            if (!await this.NavigateWithRegexCheck(this.WebBrowserCrawler, Constants.HomeUri, pageCountSucceedRegex, null, out text))
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
            for (int pageId = 1; pageId <= totalPage; pageId++)
            {
                var pageUri = new Uri(spaceLink + "?page=" + pageId);
                var pageContentRegex = new Regex(""); // TODO: what regex?
                if (!await this.NavigateWithRegexCheck(this.WebBrowserCrawler, Constants.HomeUri, pageContentRegex, null, out text))
                {
                    return;
                }

                // TODO: finish this
            }
        }

        private async Task<bool> NavigateWithRegexCheck(WebBrowser webBrowser, Uri uri, Regex succeedRegex, Regex failedRegex, out string text)
        {
            if (succeedRegex == null)
            {
                text = string.Empty;
                return false;
            }

            this.WebBrowserCrawler.Navigate(uri);
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(1000);
                
                dynamic htmlDoc = this.WebBrowserCrawler.Document;
                text = htmlDoc.documentElement.outerHTML;

                if (succeedRegex.Match(text).Success)
                {
                    return true;
                }

                if (failedRegex != null && failedRegex.Match(text).Success)
                {
                    text = string.Empty;
                    return false;
                }
            }

            text = string.Empty;
            return false;
        }
    }
}
