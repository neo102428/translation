using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Tesseract;

public class OcrService
{
    private const string TessDataPath = @"C:\TesseractData";

    public OcrService()
    {
        Directory.CreateDirectory(TessDataPath);
    }

    // --- 核心修改 (1): 方法现在接收一个 language 参数 ---
    public async Task<string> RecognizeTextAsync(System.Windows.Rect region, string language)
    {
        if (region.Width <= 0 || region.Height <= 0) return string.Empty;

        // --- 核心修改 (2): 根据 language 参数决定加载哪些 OCR 模型 ---
        string ocrLanguages;
        switch (language?.ToLower())
        {
            case "zh":
                ocrLanguages = "chi_sim+eng";
                break;
            case "jp":
                ocrLanguages = "jpn+eng";
                break;
            case "en":
                ocrLanguages = "eng";
                break;
            case "kor":
                ocrLanguages = "kor";
                break;
            case "auto":
            default:
                ocrLanguages = "chi_sim+eng+jpn+kor";
                break;
        }

        using (var bitmap = CaptureScreen(region))
        {
            return await Task.Run(() =>
            {
                try
                {
                    // --- 核心修改 (3): 使用动态计算出的 ocrLanguages ---
                    using (var engine = new TesseractEngine(TessDataPath, ocrLanguages, EngineMode.Default))
                    {
                        using (var ms = new MemoryStream())
                        {
                            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            using (var pix = Pix.LoadFromMemory(ms.ToArray()))
                            {
                                using (var page = engine.Process(pix, PageSegMode.Auto))
                                {
                                    string text = page.GetText().Trim();
                                    return text;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    string errorMessage = $"OCR 引擎初始化失败！\n\n" +
                                          $"请确保在 C:\\TesseractData 文件夹中，\n" +
                                          $"已放入所需的语言文件 (例如: '{ocrLanguages.Split('+')[0]}.traineddata')。\n\n" +
                                          $"详细错误: {ex.Message}";
                    MessageBox.Show(errorMessage, "OCR 错误");
                    return string.Empty;
                }
            });
        }
    }

    // CaptureScreen 方法保持不变
    private Bitmap CaptureScreen(System.Windows.Rect region)
    {
        var source = PresentationSource.FromVisual(Application.Current.MainWindow) ?? PresentationSource.FromVisual(new System.Windows.Controls.Grid());
        Matrix transform;
        if (source != null && source.CompositionTarget != null) { transform = source.CompositionTarget.TransformToDevice; }
        else { using (var src = new HwndSource(new HwndSourceParameters())) { transform = src.CompositionTarget.TransformToDevice; } }
        var topLeft = transform.Transform(region.TopLeft);
        var bottomRight = transform.Transform(region.BottomRight);
        var pixelRect = new System.Drawing.Rectangle((int)topLeft.X, (int)topLeft.Y, (int)Math.Abs(bottomRight.X - topLeft.X), (int)Math.Abs(bottomRight.Y - topLeft.Y));
        if (pixelRect.Width <= 0 || pixelRect.Height <= 0) { return new Bitmap(1, 1); }
        var bmp = new Bitmap(pixelRect.Width, pixelRect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp)) { g.CopyFromScreen(pixelRect.X, pixelRect.Y, 0, 0, bmp.Size); }
        return bmp;
    }
}