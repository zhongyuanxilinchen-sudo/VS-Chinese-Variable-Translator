using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

using System;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace 中文代码翻译助手
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("CSharp")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal sealed class AutoTranslateListener
        : IWpfTextViewCreationListener
    {
        public void TextViewCreated(IWpfTextView textView)
        {
            new AutoTranslateHandler(textView);
        }
    }


    internal sealed class AutoTranslateHandler
    {
        private readonly IWpfTextView _textView;

        private bool _isUpdating = false;
        private bool _isTranslating = false;


        public AutoTranslateHandler(IWpfTextView textView)
        {
            _textView = textView;

            _textView.TextBuffer.Changed += TextBuffer_Changed;
            _textView.Closed += TextView_Closed;
        }


        private void TextView_Closed(object sender, EventArgs e)
        {
            _textView.TextBuffer.Changed -= TextBuffer_Changed;
            _textView.Closed -= TextView_Closed;
        }


        private async void TextBuffer_Changed(
            object sender,
            TextContentChangedEventArgs e)
        {
            if (_isUpdating)
                return;

            if (_isTranslating)
                return;


            // 判断这次输入里有没有 ;
            //bool hasSemicolon = false;

            //foreach (ITextChange change in e.Changes)
            //{
            //    if (change.NewText.Contains(";"))
            //    {
            //        hasSemicolon = true;
            //        break;
            //    }
            //}


            //if (!hasSemicolon)
            //    return;

            try
            {
                _isTranslating = true;

                // 给中文输入法/VS编辑器一点时间完成文本提交
                await Task.Delay(80);

                await TranslateCurrentLineAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "自动翻译异常：" + ex);
            }
            finally
            {
                _isTranslating = false;
            }
        }


        private async Task TranslateCurrentLineAsync()
        {
            ITextSnapshot snapshot =
                _textView.TextBuffer.CurrentSnapshot;


            SnapshotPoint caret =
                _textView.Caret.Position.BufferPosition;


            int position = caret.Position;

            if (position >= snapshot.Length)
                position = snapshot.Length - 1;

            if (position < 0)
                return;


            ITextSnapshotLine line =
                snapshot.GetLineFromPosition(position);


            string lineText =
                line.GetText();


            // 示例：
            //
            // int 找边 = 0;
            // double 缺陷面积 = 0;
            // bool 是否找到边 = false;
            // Mat 当前图像 = null;

            string pattern =
                @"^(?<indent>\s*)" +

                @"(?<prefix>" +
                @"(?:(?:public|private|protected|internal|" +
                @"static|readonly|const|volatile)\s+)*" +

                @"[\w\.<>\[\],\?]+\s+" +
                @")" +

                @"(?<name>[\u4e00-\u9fff]" +
                @"[\u4e00-\u9fffA-Za-z0-9_]*)" +

                @"(?<rest>\s*(?:=[^;]*)?;)" +

                @"(?<comment>\s*//.*)?$";


            Match match =
                Regex.Match(lineText, pattern);


            if (!match.Success)
                return;


            string chineseName =
                match.Groups["name"].Value;


            // 调用 DeepSeek
            string englishName = await VariableTranslator.TranslateAsync(chineseName);


            if (string.IsNullOrWhiteSpace(englishName))
                return;


            string indent =
                match.Groups["indent"].Value;

            string prefix =
                match.Groups["prefix"].Value;

            string rest =
                match.Groups["rest"].Value;

            string oldComment =
                match.Groups["comment"].Value;


            string newText =
                indent +
                prefix +
                englishName +
                rest;


            // 自动添加中文注释
            if (string.IsNullOrWhiteSpace(oldComment))
            {
                newText += " // " + chineseName;
            }
            else
            {
                newText += oldComment;
            }


            // 网络请求回来以后重新获取当前文本
            ITextSnapshot currentSnapshot =
                _textView.TextBuffer.CurrentSnapshot;


            ITextSnapshotLine currentLine =
                currentSnapshot.GetLineFromLineNumber(
                    line.LineNumber);


            // 防止 API 请求期间用户已经修改了这一行
            if (currentLine.GetText() != lineText)
                return;


            try
            {
                _isUpdating = true;

                using (ITextEdit edit =
                    _textView.TextBuffer.CreateEdit())
                {
                    edit.Replace(
                        currentLine.Start.Position,
                        currentLine.Length,
                        newText);

                    edit.Apply();
                }
            }
            finally
            {
                _isUpdating = false;
            }
        }
    }
}