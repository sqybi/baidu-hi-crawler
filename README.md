# 软件说明

百度空间将于近日关闭，本软件用于抓取用户百度空间上的文章和评论。

百度本身会将文章内容转移到百度网盘，但并不会保留格式和评论。本软件在抓取时将保留全部HTML格式及评论内容。

# 使用方法

## 抓取百度空间文章/评论

1. 点击“Login Page”按钮，加载百度空间登录页面。
2. 在登录页面输入你的用户名密码登录。（这一步是为了能够拿到你的空间地址和私有文章，也是为了防止使用此工具抓取他人文章。软件并不会记录或上传任何你的用户名和密码信息。）
3. 点击“Start Crawling”按钮，软件开始自动抓取。
4. 若抓取成功，软件会弹出对话框：“Crawling Finished!”。若失败，则会弹出“Crawling Failed, exception: \<exception content\>”，请协助将对话框内容以文字或图片形式添加issue，以便于修复问题。添加issue地址： https://github.com/sqybi/baidu-hi-crawler/issues 。
5. 抓取成功后的文件存储于程序所在目录Archive文件夹下的\<baidu hi url\>.json文件中，其中\<baidu hi url\>为你的百度空间URL后缀。如：百度空间地址为 http://hi.baidu.com/sqybi ，则抓取结果存放在sqybi.json文件里。

## 加载已经抓取的文章/评论

1. 切换到Load from local选项卡。
2. 点击下方的Load from local按钮。
3. 在弹出的对话框中选择之前下载的json文件，点击确定，会看到所有文章都被加载进来。
4. 双击文章查看详细内容和评论。

## 遇到问题？

如果在使用中遇到问题，请遵循以下步骤：

* 如果在抓取时出错，可能是网络情况不好，请先尝试在网络情况良好时重试。
* 检查 https://github.com/sqybi/baidu-hi-crawler/releases 是否有新版本，如果有，请使用新版本重试。
* 如果有解决不了的问题，请在 https://github.com/sqybi/baidu-hi-crawler/issues 使用New Issue功能提出新的issue。尽量详细地描述你遇到问题的过程，你的系统信息，以及所有可以得到的错误信息。

# 更新日志

## v0.2.0

* 添加自动更新功能，会自动连接GitHub检查更新版本并下载。
* 抓取文章的时候会抓取发表时间和文章ID了。
* 增加了Logger，现在可以记录错误了。
* 现在的版本比之前更稳定，更少崩溃。

## v0.1.1

* 修正了一个导致文章页面无法正常抓取的bug。
* 修正了一个若没有手动建立Archive文件夹则不能顺利保存文件的bug。
* 修正了README中的错误。

## v0.1.0

* 第一个正式版本。