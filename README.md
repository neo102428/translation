# 🌟 划词翻译 & OCR 工具

<div align="center">

[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-0078D4?logo=windows)](https://www.microsoft.com/windows)
[![Language](https://img.shields.io/badge/Language-C%23-239120?logo=csharp)](https://docs.microsoft.com/dotnet/csharp/)

**一款强大、美观、易用的 Windows 屏幕取词翻译工具**

支持百度翻译、腾讯翻译、谷歌翻译 | 支持多显示器 | 现代化 UI

[功能特点](#-功能特点) • [快速开始](#-快速开始) • [使用指南](#-使用指南) • [开发文档](#-开发文档)

</div>

---

## 📖 简介

这是一款专为 Windows 用户打造的屏幕取词翻译工具。只需按住鼠标拖动，即可识别并翻译屏幕上任何区域的文本——无论是图片、视频、PDF、游戏还是受保护的文档。

### ✨ 为什么选择这个工具？

- 🎯 **真正的所见即所得** - 直接在屏幕上框选，无需截图、无需复制
- 🚀 **快速响应** - 智能缓存机制，相同内容秒出结果
- 🎨 **现代化界面** - 精心设计的 UI，支持浅色/暗夜双主题
- 🌐 **多引擎支持** - 百度、腾讯、谷歌三大翻译引擎任选
- 🖥️ **完美多屏支持** - 多显示器、不同 DPI 缩放完美适配
- 💾 **轻量绿色** - 无需安装，解压即用，不留痕迹

---

## 🎯 功能特点

### 核心功能

#### 🔍 强大的 OCR 识别
- 基于 Tesseract 4.1 引擎
- 支持中文、英文、日文、韩文等多种语言
- 可识别图片、视频、PDF、游戏界面等任何屏幕内容
- 智能反色处理，暗色背景文字也能准确识别

#### 🌐 多翻译引擎支持
| 引擎 | 免费额度 | 特点 | 推荐场景 |
|------|---------|------|---------|
| **百度翻译** | 200万字符/月 | 响应快速，国内稳定 | 日常使用 |
| **腾讯翻译** | 500万字符/月 | 企业级质量，免费额度大 | 专业翻译 |
| **谷歌翻译** | 50万字符/月 | 支持100+语言，质量最佳 | 国际用户 |

#### 🖱️ 灵活的触发方式
- **中键拖动** - 按住鼠标中键拖动选区
- **右键拖动** - 按住鼠标右键拖动选区
- **Alt+左键** - 按住 Alt 键同时拖动左键

#### 🎨 现代化用户界面
- ☀️ **浅色模式** - 清新明亮，适合白天使用
- 🌙 **暗夜模式** - 护眼舒适，适合夜间使用
- 🎴 **卡片式设计** - 圆角、阴影、渐变，视觉层次分明
- 🔍 **实时搜索** - 历史记录支持实时搜索过滤

#### 🖥️ 完美的多显示器支持
- ✅ 支持任意数量的显示器
- ✅ 自动检测每个显示器的 DPI 缩放
- ✅ 支持不同分辨率和缩放比例混合
- ✅ 支持横屏、竖屏显示器
- ✅ 翻译结果窗口智能定位到正确显示器

#### 📚 智能历史记录
- 💾 自动保存所有翻译记录
- 🔍 支持搜索原文和译文
- 📊 显示翻译时间和统计信息
- 🗑️ 支持删除单条或清空全部
- 💡 卡片式展示，选中高亮

#### ⚡ 性能优化
- 🚀 智能缓存机制，重复内容秒出
- 🔄 自动重试，网络波动不怕
- 🎯 异步处理，界面永不卡顿
- 💾 轻量级运行，资源占用低

---

## 🚀 快速开始

### 系统要求

- **操作系统**: Windows 7 / 8 / 10 / 11 (x64)
- **.NET 运行时**: .NET 8.0 Desktop Runtime
- **内存**: 至少 100 MB 可用内存

### 安装步骤

#### 1. 下载程序

前往 [Releases](https://github.com/neo102428/translation/releases) 页面下载最新版本。

#### 2. 安装 .NET 8 运行时

如果您的系统尚未安装 .NET 8，请下载并安装：

- [.NET 8 Desktop Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/8.0)

或使用 winget 快速安装：
```powershell
winget install Microsoft.DotNet.DesktopRuntime.8
```

#### 3. 解压并运行

1. 将下载的 zip 文件解压到任意目录
2. 双击运行 `translation.exe`
3. 程序将在系统托盘显示图标 🌐

### 首次配置

#### 配置翻译引擎（必须）

程序需要翻译 API 才能工作，请选择以下任一引擎并配置：

##### 选项 1: 百度翻译（推荐国内用户）

1. 访问 [百度翻译开放平台](https://fanyi-api.baidu.com/)
2. 注册并申请"通用文本翻译" API
3. 获取 **APP ID** 和 **密钥**
4. 在程序设置中填入并保存

##### 选项 2: 腾讯翻译（推荐专业用户）

1. 访问 [腾讯云机器翻译](https://cloud.tencent.com/product/tmt)
2. 开通服务并获取 **Secret ID** 和 **Secret Key**
3. 在程序设置中选择"腾讯翻译"并填入密钥

##### 选项 3: 谷歌翻译（推荐国际用户）

1. 访问 [Google Cloud Translation](https://cloud.google.com/translate)
2. 创建项目并启用 Translation API
3. 创建 API 密钥
4. 在程序设置中选择"谷歌翻译"并填入密钥

---

## 📖 使用指南

### 基本使用

1. **启动程序** - 双击 `translation.exe`，程序将在托盘运行
2. **框选文字** - 按住触发键（默认为鼠标中键）拖动选择屏幕区域
3. **查看翻译** - 松开鼠标，翻译结果自动弹出
4. **复制结果** - 鼠标选中结果窗口中的文字即可复制

### 高级功能

#### 📝 查看历史记录

1. 右键点击托盘图标
2. 选择「查看翻译历史」
3. 使用搜索框快速查找历史记录
4. 点击记录可查看详情

#### ⚙️ 自定义设置

右键托盘图标 → 选择「设置」

**可配置项：**
- 🌐 **翻译引擎** - 选择百度/腾讯/谷歌
- 🔑 **API 密钥** - 配置对应引擎的密钥
- 🖱️ **触发方式** - 选择触发快捷键
- 🌍 **语言配置** - 设置源语言和目标语言
- 🎨 **显示模式** - 切换浅色/暗夜主题

#### 🖥️ 多显示器使用

程序完美支持多显示器环境：

- ✅ 在任意显示器上框选都能正确识别
- ✅ 翻译结果窗口自动出现在当前显示器
- ✅ 自动适配不同显示器的 DPI 缩放（100%、125%、150%、200%）
- ✅ 支持横屏、竖屏显示器混合使用

---

## 🎨 界面预览

### 设置窗口
- 🌐 翻译引擎选择
- 🔑 API 密钥配置（根据引擎动态显示）
- 🖱️ 触发方式自定义
- 🌍 语言配置
- 🎨 主题模式切换
- 💡 带超链接的申请指引

### 历史记录窗口
- 📊 记录数量统计
- 🔍 实时搜索功能
- 🎴 卡片式列表展示
- ✅ 明显的选中状态
- 🗑️ 删除和清空功能

### 翻译结果窗口
- 🌓 支持浅色/暗夜主题
- 📝 可选择和复制文本
- 🖱️ 可拖动位置
- ⏱️ 5 秒自动隐藏
- 🔄 鼠标悬停暂停隐藏

---

## 🛠️ 开发文档

### 技术栈

| 组件 | 技术 | 版本 |
|------|------|------|
| **框架** | .NET | 8.0 |
| **UI** | WPF | - |
| **语言** | C# | 12.0 |
| **OCR 引擎** | Tesseract | 4.1.1 |
| **JSON** | Newtonsoft.Json | 13.0.3 |
| **托盘图标** | Hardcodet.NotifyIcon.Wpf | 1.1.0 |
| **绘图** | System.Drawing.Common | 8.0.0 |

### 项目结构

```
translation/
├── App.xaml                      # 应用程序主入口
├── App.xaml.cs                   # 应用启动逻辑、单例检查、DPI 感知
├── MainWindow.xaml.cs            # 核心逻辑：鼠标钩子、坐标转换、OCR 调用
├── SettingsWindow.xaml           # 设置界面 UI
├── SettingsWindow.xaml.cs        # 设置逻辑：引擎切换、配置保存
├── HistoryWindow.xaml            # 历史记录界面 UI
├── HistoryWindow.xaml.cs         # 历史记录逻辑：搜索、删除
├── ResultWindow.xaml             # 翻译结果窗口 UI
├── ResultWindow.xaml.cs          # 结果窗口逻辑：主题切换、自动隐藏
├── SelectionWindow.xaml          # 选择框窗口（红色边框）
├── TranslationService.cs         # 翻译服务：多引擎实现
├── OcrService.cs                 # OCR 服务：Tesseract 封装
├── SettingsService.cs            # 设置服务：配置管理
├── HistoryService.cs             # 历史服务：记录管理
├── CacheService.cs               # 缓存服务
├── LoggerService.cs              # 日志服务
├── GlobalMouseHook.cs            # 全局鼠标钩子
└── tessdata/                     # Tesseract 语言数据文件
    ├── chi_sim.traineddata       # 简体中文
    ├── eng.traineddata           # 英语
    ├── jpn.traineddata           # 日语（可选）
    └── kor.traineddata           # 韩语（可选）
```

### 核心功能实现

#### 多显示器 DPI 支持

程序通过 Windows API 动态获取每个显示器的 DPI 缩放：

```csharp
// 获取鼠标位置所在显示器的 DPI
IntPtr monitor = MonitorFromPoint(mousePoint, MONITOR_DEFAULTTONEAREST);
GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out uint dpiX, out uint dpiY);
double scaleX = dpiX / 96.0;
double scaleY = dpiY / 96.0;

// 坐标转换
double wpfX = screenX / scaleX;
double wpfY = screenY / scaleY;
```

#### 翻译引擎架构

```csharp
public async Task<string> TranslateAsync(string query, string from, string to)
{
    return _settingsService.Settings.Engine switch
    {
        TranslationEngine.Baidu => await TranslateBaiduAsync(query, from, to),
        TranslationEngine.Tencent => await TranslateTencentAsync(query, from, to),
        TranslationEngine.Google => await TranslateGoogleAsync(query, from, to),
        _ => "错误：未知的翻译引擎"
    };
}
```

### 本地构建

#### 1. 克隆仓库

```bash
git clone https://github.com/neo102428/translation.git
cd translation
```

#### 2. 环境要求

- Visual Studio 2022 或更高版本
- .NET 8.0 SDK

#### 3. 构建项目

```bash
dotnet restore
dotnet build --configuration Release
```

#### 4. 运行

```bash
dotnet run --project translation.csproj
```

或在 Visual Studio 中按 F5 启动调试。

### 发布

```bash
# 发布单文件版本
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

# 发布完全独立版本（包含运行时）
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## ❓ 常见问题

### 程序无法启动

**Q: 双击程序没反应**

A: 请检查是否安装了 .NET 8 Desktop Runtime。运行以下命令检查：
```powershell
dotnet --list-runtimes
```

如果没有看到 `Microsoft.WindowsDesktop.App 8.0.x`，请[下载安装](https://dotnet.microsoft.com/download/dotnet/8.0)。

### OCR 识别问题

**Q: OCR 无法识别文字**

A: 请确保：
1. `tessdata` 文件夹和语言文件（如 `chi_sim.traineddata`）在程序目录下
2. 选择的源语言与实际文字语言一致
3. 文字清晰度足够（尝试放大选区）

**Q: 识别结果不准确**

A: 
- Tesseract 对打印体文字识别效果最好
- 手写体、艺术字体识别率较低
- 建议尽量选择清晰、大小适中的文字区域

### 翻译问题

**Q: 提示 API 密钥未配置**

A: 请按照[首次配置](#首次配置)章节申请并配置 API 密钥。

**Q: 翻译请求失败**

A: 
1. 检查网络连接
2. 确认 API 密钥正确
3. 检查是否超出免费额度
4. 查看日志文件了解详细错误信息

### 多显示器问题

**Q: 副显示器上框选位置不对**

A: 此问题已在最新版本修复。请确保使用最新版本。

**Q: 翻译结果窗口出现在错误的显示器**

A: 已修复。程序会自动检测鼠标位置并在正确的显示器上显示结果。

---

## 📋 更新日志

### v2.0.0 (最新)

#### 新功能
- ✨ 添加腾讯翻译引擎支持
- ✨ 添加谷歌翻译引擎支持
- ✨ 现代化 UI 设计（浅色/暗夜双主题）
- ✨ 历史记录实时搜索功能
- ✨ 完善的多显示器支持

#### 改进
- 🔧 修复多显示器 DPI 缩放问题
- 🔧 优化坐标转换逻辑
- 🔧 改进选中状态视觉反馈
- 🔧 增强错误处理和日志记录
- 🔧 优化翻译缓存机制

#### 技术升级
- ⬆️ 升级到 .NET 8.0
- ⬆️ 优化性能和内存占用
- ⬆️ 代码架构重构

### v1.0.0

- 🎉 首次发布
- ✅ 基本的 OCR 识别功能
- ✅ 百度翻译集成
- ✅ 系统托盘支持
- ✅ 历史记录功能

---

## 🤝 贡献

欢迎贡献代码、报告问题或提出建议！

### 如何贡献

1. Fork 本仓库
2. 创建您的特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交您的更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启一个 Pull Request

### 报告问题

如果您发现了 bug 或有功能建议，请在 [Issues](https://github.com/neo102428/translation/issues) 页面提交。

---

## 📄 许可证

本项目采用 [MIT License](LICENSE) 开源协议。

```
MIT License

Copyright (c) 2024 translation

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---

## 🙏 致谢

- [Tesseract OCR](https://github.com/tesseract-ocr/tesseract) - 强大的开源 OCR 引擎
- [Tesseract.NET](https://github.com/charlesw/tesseract) - Tesseract 的 .NET 包装
- [百度翻译 API](https://fanyi-api.baidu.com/) - 提供翻译服务
- [腾讯云翻译](https://cloud.tencent.com/product/tmt) - 提供翻译服务
- [Google Cloud Translation](https://cloud.google.com/translate) - 提供翻译服务
- [Hardcodet.NotifyIcon.Wpf](http://www.hardcodet.net/wpf-notifyicon) - WPF 托盘图标控件

---

## 📞 联系方式

- **GitHub**: [@neo102428](https://github.com/neo102428)
- **项目主页**: [https://github.com/neo102428/translation](https://github.com/neo102428/translation)
- **Issues**: [https://github.com/neo102428/translation/issues](https://github.com/neo102428/translation/issues)

---

<div align="center">

**如果这个项目对您有帮助，请给个 ⭐ Star 支持一下！**

Made with ❤️ by neo102428

</div>