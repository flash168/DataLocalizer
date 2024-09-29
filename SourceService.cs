using System;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RestSharp;
using System.IO;
using System.Collections.Generic;
using Microsoft.VisualBasic;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.Json.Serialization.Metadata;

namespace DataLocalizer
{
    public class SourceService
    {
        private RestClient _client;
        private string url;
        private string Data;

        Uri baseUri;
        private SourceModel DataJson;
        private JsonSerializerOptions options;
        public DownloadManager downloadManager;

        public SourceService()
        {
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
            //ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            var handler = new RestClientOptions
            {
                // 模拟浏览器的 User-Agent
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36 Edg/129.0.0.0",
                // 自动处理 3xx 重定向
                //FollowRedirects = true,
                // 启用 GZip/Deflate/Brotli 压缩
                // AutomaticDecompression = DecompressionMethods.GZip,
                // 设置超时时间
                MaxTimeout = 30000,  // 10 秒超时
                                     // 配置 Cookie 容器（模拟浏览器的 Cookie 管理）
                                     // CookieContainer = new System.Net.CookieContainer(),
                                     // 最大连接数，类似浏览器对服务器的最大连接数
                                     //MaxRedirects = 10,
                                     // 启用 HTTP/2，如果服务器支持
                                     //ThrowOnAnyError = false
            };
            _client = new RestClient();

            options = new JsonSerializerOptions
            {
                TypeInfoResolver = MyJsonContext.Default,
                WriteIndented = true, // 美化输出
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // 忽略为 null 的字段
                AllowTrailingCommas = true, // 允许 JSON 末尾有逗号
            };
            downloadManager = new DownloadManager(5);
        }

        public async Task<string> GetSource(string _url)
        {
            url = _url;
            baseUri = new Uri(url);
            try
            {
                var request = new RestRequest(url, Method.Get);
                request.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                var req = await _client.ExecuteAsync(request);
                if (req.IsSuccessful)
                {
                    string? json = req.Content;
                    DataJson = JsonSerializer.Deserialize<SourceModel>(json, options);
                    string formattedJson = JsonSerializer.Serialize(DataJson, options);
                    Data = Regex.Unescape(formattedJson);
                    return Data;
                }
                else
                {
                    return "ERROR:" + req.ErrorMessage;
                }
            }
            catch (Exception e)
            {
                return "ERROR:" + e.Message;
            }
        }

        List<string> strings = new List<string>();
        public async Task<int> SaveLocal(string path)
        {
            strings.Clear();
            await File.WriteAllTextAsync(Path.Combine(path, "index.json"), Data);
            var urls = AssembleUrl(path, DataJson.Spider);
            downloadManager.AddTask(urls.Item1, urls.Item2);

            foreach (var item in DataJson.Sites)
            {
                var ts = AssembleUrl(path, item.api);
                if (!strings.Contains(ts.Item2))
                {
                    strings.Add(ts.Item2);
                    downloadManager.AddTask(ts.Item1, ts.Item2);
                }
                var ts2 = AssembleUrl(path, item.ext);
                if (!strings.Contains(ts2.Item2))
                {
                    strings.Add(ts2.Item2);
                    downloadManager.AddTask(ts2.Item1, ts2.Item2);
                }
            }
            return strings.Count + 1;
        }


        private (string, string) AssembleUrl(string path, string _url)
        {
            if (!path.EndsWith('\\'))
                path += "\\";
            var pt = new Uri(path);
            string fullUri = new Uri(pt, _url).LocalPath;
            int questionMarkIndex = fullUri.IndexOf('?');

            // 如果找到问号，截取问号之前的部分
            if (questionMarkIndex != -1)
            {
                fullUri = fullUri.Substring(0, questionMarkIndex);
            }
            // 使用 baseUri 拼接相对路径
            Uri uri = new Uri(baseUri, _url);
            return (uri.AbsoluteUri, fullUri);
        }




    }
}
