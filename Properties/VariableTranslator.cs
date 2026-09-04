using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace 中文代码翻译助手
{
    public static class VariableTranslator
    {
        private static readonly HttpClient _httpClient =
            new HttpClient();


        private static bool _showedApiKeyWarning = false;


        public static async Task<string> TranslateAsync(
            string chineseName)
        {
            if (string.IsNullOrWhiteSpace(chineseName))
                return null;


            try
            {
                //
                // 读取插件设置
                //

                await ThreadHelper.JoinableTaskFactory
                    .SwitchToMainThreadAsync();


                TranslatorOptions options =
                    中文代码翻译助手Package.Instance?
                    .GetTranslatorOptions();


                if (options == null)
                    return null;


                string apiKey =
                    options.ApiKey?.Trim();

                string apiUrl =
                    options.ApiUrl?.Trim();

                string model =
                    options.Model?.Trim();


                //
                // 没有设置 API Key
                //

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    if (!_showedApiKeyWarning)
                    {
                        _showedApiKeyWarning = true;

                        VsShellUtilities.ShowMessageBox(
                            中文代码翻译助手Package.Instance,
                            "请先设置 DeepSeek API Key。\n\n" +
                            "Visual Studio：\n" +
                            "工具 → 选项 → 中文代码翻译助手 → DeepSeek",
                            "中文代码翻译助手",
                            OLEMSGICON.OLEMSGICON_INFO,
                            OLEMSGBUTTON.OLEMSGBUTTON_OK,
                            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    }

                    return null;
                }


                if (string.IsNullOrWhiteSpace(apiUrl))
                {
                    apiUrl =
                        "https://api.deepseek.com/chat/completions";
                }


                if (string.IsNullOrWhiteSpace(model))
                {
                    model =
                        "deepseek-v4-flash";
                }


                ServicePointManager.SecurityProtocol =
                    SecurityProtocolType.Tls12;


                //
                // DeepSeek Prompt
                //

                var data = new
                {
                    model = model,

                    thinking = new
                    {
                        type = "disabled"
                    },

                    messages = new object[]
                    {
                        new
                        {
                            role = "system",

                            content =
@"你是一个 C# 变量命名助手。

把用户输入的中文变量名称转换成专业、简洁、符合 C# 编程习惯的英文变量名称。

要求：

1. 只返回变量名
2. 不要解释
3. 不要Markdown
4. 不要代码块
5. 不要添加注释
6. 不要添加分号
7. 使用camelCase
8. 返回合法C#标识符
9. 保留机器视觉、工业视觉、自动化领域的专业含义
10. bool含义优先使用is、has、can、should等前缀

示例：

找边 -> findEdge
找边次数 -> edgeSearchCount
缺陷面积 -> defectArea
缺陷数量 -> defectCount
当前图像 -> currentImage
图像宽度 -> imageWidth
边缘位置 -> edgePosition
是否找到边 -> isEdgeFound
左侧露头宽度 -> leftOverhangWidth
分切边位置 -> slittingEdgePosition"
                        },

                        new
                        {
                            role = "user",
                            content = chineseName
                        }
                    },

                    max_tokens = 30
                };


                string json =
                    JsonConvert.SerializeObject(data);


                Debug.WriteLine(
                    "DeepSeek：" +
                    chineseName);


                using (HttpRequestMessage request =
                    new HttpRequestMessage(
                        HttpMethod.Post,
                        apiUrl))
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue(
                            "Bearer",
                            apiKey);


                    request.Content =
                        new StringContent(
                            json,
                            Encoding.UTF8,
                            "application/json");


                    HttpResponseMessage response =
                        await _httpClient.SendAsync(request);


                    string result =
                        await response.Content
                        .ReadAsStringAsync();


                    Debug.WriteLine(
                        "HTTP：" +
                        (int)response.StatusCode +
                        " " +
                        response.StatusCode);


                    if (!response.IsSuccessStatusCode)
                    {
                        Debug.WriteLine(
                            "DeepSeek返回：" +
                            result);

                        return null;
                    }


                    JObject obj =
                        JObject.Parse(result);


                    string englishName =
                        obj["choices"]?[0]
                           ?["message"]
                           ?["content"]
                           ?.ToString()
                           .Trim();


                    if (string.IsNullOrWhiteSpace(
                        englishName))
                    {
                        return null;
                    }


                    //
                    // 清理返回值
                    //

                    englishName =
                        englishName
                        .Replace("\"", "")
                        .Replace("'", "")
                        .Replace("`", "")
                        .Replace(";", "")
                        .Trim();


                    Debug.WriteLine(
                        chineseName +
                        " -> " +
                        englishName);


                    return englishName;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "DeepSeek异常：" +
                    ex);

                return null;
            }
        }
    }
}