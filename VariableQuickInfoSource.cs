using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

using System;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace 中文代码翻译助手
{
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("中文变量说明 QuickInfo")]
    [ContentType("CSharp")]
    [Order(After = "default")]
    internal sealed class VariableQuickInfoSourceProvider
        : IAsyncQuickInfoSourceProvider
    {
        public IAsyncQuickInfoSource TryCreateQuickInfoSource(
            ITextBuffer textBuffer)
        {
            return new VariableQuickInfoSource(textBuffer);
        }
    }


    internal sealed class VariableQuickInfoSource
        : IAsyncQuickInfoSource
    {
        private readonly ITextBuffer _buffer;


        public VariableQuickInfoSource(ITextBuffer buffer)
        {
            _buffer = buffer;
        }


        public Task<QuickInfoItem> GetQuickInfoItemAsync(
            IAsyncQuickInfoSession session,
            CancellationToken cancellationToken)
        {
            ITextSnapshot snapshot =
                _buffer.CurrentSnapshot;


            SnapshotPoint? triggerPoint =
                session.GetTriggerPoint(snapshot);


            if (!triggerPoint.HasValue)
            {
                return Task.FromResult<QuickInfoItem>(null);
            }


            int position =
                triggerPoint.Value.Position;


            if (position < 0 ||
                position >= snapshot.Length)
            {
                return Task.FromResult<QuickInfoItem>(null);
            }


            //
            // 1. 找鼠标当前所在的变量名
            //

            int start = position;
            int end = position;


            // 如果鼠标刚好停在变量尾部
            if (start > 0 &&
                !IsIdentifierChar(snapshot[start]) &&
                IsIdentifierChar(snapshot[start - 1]))
            {
                start--;
                end--;
            }


            while (start > 0 &&
                   IsIdentifierChar(snapshot[start - 1]))
            {
                start--;
            }


            while (end < snapshot.Length &&
                   IsIdentifierChar(snapshot[end]))
            {
                end++;
            }


            if (end <= start)
            {
                return Task.FromResult<QuickInfoItem>(null);
            }


            string variableName =
                snapshot.GetText(
                    start,
                    end - start);


            if (string.IsNullOrWhiteSpace(variableName))
            {
                return Task.FromResult<QuickInfoItem>(null);
            }


            //
            // 2. 在当前代码文件中寻找变量声明
            //
            // 例如：
            //
            // string whiteSpotCount = "baidian"; // 白点数量
            //

            string chineseComment =
                FindChineseComment(
                    snapshot,
                    variableName);


            if (string.IsNullOrWhiteSpace(chineseComment))
            {
                return Task.FromResult<QuickInfoItem>(null);
            }


            //
            // 3. 设置 QuickInfo 应用范围
            //

            ITrackingSpan trackingSpan =
                snapshot.CreateTrackingSpan(
                    start,
                    end - start,
                    SpanTrackingMode.EdgeInclusive);


            //
            // 4. 返回要追加到 VS 悬浮框里的信息
            //

            string tooltip =
                "中文含义：" + chineseComment;


            QuickInfoItem item =
                new QuickInfoItem(
                    trackingSpan,
                    tooltip);


            return Task.FromResult(item);
        }


        private static string FindChineseComment(
            ITextSnapshot snapshot,
            string variableName)
        {
            string escapedName =
                Regex.Escape(variableName);


            //
            // 大致匹配：
            //
            // int defectCount = 0; // 缺陷数量
            // string whiteSpotCount = "..."; // 白点
            // double leftWidth; // 左侧宽度
            //
            // 暂时不解析 Roslyn AST，
            // 第一版直接扫描声明行。
            //

            string pattern =
                @"^\s*" +
                @"(?:(?:public|private|protected|internal|" +
                @"static|readonly|const|volatile)\s+)*" +

                @"[\w\.<>\[\],\?]+\s+" +

                escapedName +

                @"\b" +

                @"[^;]*;" +

                @"\s*//\s*" +

                @"(?<comment>.+?)\s*$";


            Regex regex =
                new Regex(
                    pattern,
                    RegexOptions.Compiled);


            for (int i = 0;
                 i < snapshot.LineCount;
                 i++)
            {
                ITextSnapshotLine line =
                    snapshot.GetLineFromLineNumber(i);


                string text =
                    line.GetText();


                Match match =
                    regex.Match(text);


                if (!match.Success)
                    continue;


                string comment =
                    match.Groups["comment"].Value.Trim();


                if (string.IsNullOrWhiteSpace(comment))
                    continue;


                return comment;
            }


            return null;
        }


        private static bool IsIdentifierChar(char c)
        {
            return char.IsLetterOrDigit(c) ||
                   c == '_';
        }


        public void Dispose()
        {
        }
    }
}