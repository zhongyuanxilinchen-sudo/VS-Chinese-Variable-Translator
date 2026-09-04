using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace 中文代码翻译助手
{
    [PackageRegistration(
        UseManagedResourcesOnly = true,
        AllowsBackgroundLoading = true)]

    [Guid(PackageGuidString)]

    [ProvideOptionPage(
        typeof(TranslatorOptions),
        "中文代码翻译助手",
        "DeepSeek",
        0,
        0,
        true)]

    public sealed class 中文代码翻译助手Package : AsyncPackage
    {
        public const string PackageGuidString =
            "9338c7b4-ba40-4474-a6ea-c9eaf6db3082";


        // 关键：增加这个
        public static 中文代码翻译助手Package Instance
        {
            get;
            private set;
        }


        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            // 关键：启动插件时保存当前 Package 实例
            Instance = this;

            await JoinableTaskFactory
                .SwitchToMainThreadAsync(cancellationToken);
        }


        public TranslatorOptions GetTranslatorOptions()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return (TranslatorOptions)GetDialogPage(
                typeof(TranslatorOptions));
        }
    }
}