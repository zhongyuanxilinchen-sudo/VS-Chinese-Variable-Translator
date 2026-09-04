# Visual Studio 中文变量翻译助手

一个面向 Visual Studio 2022 的 C# 中文变量名自动翻译扩展。
开发时可以直接使用中文思考和输入变量名，当变量声明完成并输入 `;` 后，插件自动调用 DeepSeek API，将中文变量名转换成符合 C# 命名习惯的 camelCase 英文变量名，同时保留原始中文作为代码注释。
## 功能说明
例如输入：
```csharp
int 缺陷数量 = 10;
会变为
int defectCount = 10; // 缺陷数量


double 缺陷面积 = 0;
bool 是否找到边 = false;
string 当前名称 = "";
会转变为
double defectArea = 0;       // 缺陷面积
bool isEdgeFound = false;    // 是否找到边
string currentName = "";     // 当前名称
