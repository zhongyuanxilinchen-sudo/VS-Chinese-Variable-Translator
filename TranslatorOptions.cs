using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace 中文代码翻译助手
{
    public class TranslatorOptions : DialogPage
    {
        [Category("DeepSeek")]
        [DisplayName("API Key")]
        [Description("请输入你自己的 DeepSeek API Key")]
        [PasswordPropertyText(true)]
        public string ApiKey { get; set; } = "";


        [Category("DeepSeek")]
        [DisplayName("API 地址")]
        [Description("DeepSeek Chat Completions API 地址")]
        public string ApiUrl { get; set; } =
            "https://api.deepseek.com/chat/completions";


        [Category("DeepSeek")]
        [DisplayName("模型")]
        [Description("DeepSeek 模型名称")]
        public string Model { get; set; } =
            "deepseek-v4-flash";
    }
}