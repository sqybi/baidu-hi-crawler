namespace BaiduHiCrawler
{
    using System;

    static class Constants
    {
        public const string CommentRetrivalUrlPattern =
            "http://hi.baidu.com/qcmt/data/cmtlist?qing_request_source=new_request&thread_id_enc={0}&start={1}&count={2}&orderby_type=0&favor=2&type=smblog";

        public const string LocalArchiveFolder = @".\Archive\";

        public const string LogsFolder = @".\Logs\";

        public static readonly Uri LoginUri = new Uri("http://hi.baidu.com/go/login");

        public static readonly Uri HomeUri = new Uri("http://hi.baidu.com/home");

        public static readonly DateTime CommentBaseDateTime = new DateTime(1970, 1, 1, 0, 0, 0);

        public static readonly LogLevel LogLevel = LogLevel.Warning;
    }
}
