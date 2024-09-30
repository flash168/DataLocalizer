using System;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RestSharp;
using System.IO;
using System.Collections.Generic;

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

            DataJson.homeLogo = DownLoad(DataJson.homeLogo, path, "./");
            DataJson.Spider = DownLoad(DataJson.Spider, path, "./jar/");

            foreach (var item in DataJson.Sites)
            {
                item.api = DownLoad(item.api, path, "./libs/");
                item.ext = DownLoad(item.ext, path, "./js/");
            }
            var json = JsonSerializer.Serialize(DataJson, options);

            json = Uri.UnescapeDataString(json);
            json = Regex.Unescape(json);
            if (File.Exists(Path.Combine(path, "index.json")))
                File.Delete(Path.Combine(path, "index.json"));
            await File.WriteAllTextAsync(Path.Combine(path, "index.json"), json);
            return strings.Count + 1;
        }

        private string DownLoad(string? url, string path, string path1)
        {
            if (string.IsNullOrEmpty(url) || url.Contains("csp") || (!url.StartsWith("http") && !url.StartsWith(".")))
                return url;
            var sp = AssembleUrl(url);
            if (!strings.Contains(sp.Item2))
            {
                strings.Add(sp.Item2);
                downloadManager.AddTask(sp.Item1, $"{path}{path1.TrimStart('.')}{sp.Item2}");
            }
            return $"{path1}{sp.Item2}";
        }

        private (string, string) AssembleUrl(string _url)
        {
            Uri uri;
            // 使用 baseUri 拼接相对路径
            if (_url?.StartsWith("./") == true)
                uri = new Uri(baseUri, _url);
            else
                uri = new Uri(_url);

            string fullUri = uri.Segments[uri.Segments.Length - 1];
            int questionMarkIndex = fullUri.IndexOf('?');

            // 如果找到问号，截取问号之前的部分
            if (questionMarkIndex != -1)
            {
                fullUri = fullUri.Substring(0, questionMarkIndex);
            }

            fullUri = Uri.UnescapeDataString(fullUri);
            fullUri = Regex.Unescape(fullUri);

            return (uri.AbsoluteUri, fullUri);
        }




    }
}
