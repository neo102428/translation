using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using Tesseract;
using Application = System.Windows.Application;

public class OcrService
{
    private readonly string TessDataPath;

    public OcrService()
    {
        // 优先使用程序目录下的 tessdata，如果不存在则使用 C 盘
        string localTessData = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
        
        if (Directory.Exists(localTessData) && HasTrainedDataFiles(localTessData))
        {
            TessDataPath = localTessData;
        }
        else
        {
            TessDataPath = @"C:\TesseractData";
            Directory.CreateDirectory(TessDataPath);
            
            // 如果 C 盘目录为空，尝试从本地复制
            if (!HasTrainedDataFiles(TessDataPath) && Directory.Exists(localTessData))
            {
                CopyTessDataFiles(localTessData, TessDataPath);
            }
        }
    }

    private bool HasTrainedDataFiles(string path)
    {
        if (!Directory.Exists(path)) return false;
        var files = Directory.GetFiles(path, "*.traineddata");
        return files.Length > 0;
    }

    private void CopyTessDataFiles(string sourcePath, string destPath)
    {
        try
        {
            Directory.CreateDirectory(destPath);
            foreach (string file in Directory.GetFiles(sourcePath, "*.traineddata"))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destPath, fileName);
                if (!File.Exists(destFile))
                {
                    File.Copy(file, destFile, false);
                }
            }
        }
        catch (Exception ex)
        {
            // 复制失败不影响程序运行，后续会有错误提示
            System.Diagnostics.Debug.WriteLine($"复制 tessdata 文件失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 识别文本 - 接收屏幕像素坐标
    /// </summary>
    /// <param name="screenRect">屏幕像素坐标（已经考虑 DPI 缩放）</param>
    /// <param name="language">语言代码</param>
    public async Task<string> RecognizeTextAsync(System.Windows.Rect screenRect, string language)
    {
        if (screenRect.Width <= 0 || screenRect.Height <= 0) return string.Empty;

        using (var originalBitmap = CaptureScreenDirect(screenRect))
        {
            if (originalBitmap == null) return string.Empty;

            Bitmap bitmapToProcess = originalBitmap;

            if (ShouldInvert(originalBitmap))
            {
                bitmapToProcess = InvertBitmap(originalBitmap);
            }

            string result = await PerformOcr(bitmapToProcess, language);

            if (bitmapToProcess != originalBitmap)
            {
                bitmapToProcess.Dispose();
            }

            return result;
        }
    }

    private async Task<string> PerformOcr(Bitmap imageToProcess, string language)
    {
        string ocrLanguages;
        switch (language?.ToLower())
        {
            case "zh": ocrLanguages = "chi_sim+eng"; break;
            case "jp": ocrLanguages = "jpn+eng"; break;
            case "en": ocrLanguages = "eng"; break;
            case "kor": ocrLanguages = "kor"; break;
            case "auto": default: ocrLanguages = "chi_sim+eng+jpn+kor"; break;
        }

        return await Task.Run(() =>
        {
            try
            {
                using (var engine = new TesseractEngine(TessDataPath, ocrLanguages, EngineMode.Default))
                {
                    using (var ms = new MemoryStream())
                    {
                        imageToProcess.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        using (var pix = Pix.LoadFromMemory(ms.ToArray()))
                        {
                            using (var page = engine.Process(pix, PageSegMode.Auto))
                            {
                                return page.GetText().Trim();
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

    private bool ShouldInvert(Bitmap bmp)
    {
        if (bmp == null || bmp.Width < 10 || bmp.Height < 10) return false;

        BitmapData data = null;
        try
        {
            data = bmp.LockBits(
                new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadOnly,
                bmp.PixelFormat);

            int bytesPerPixel = Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
            if (bytesPerPixel < 3) return false;

            long totalBrightness = 0;
            int sampleCount = 0;
            int step = Math.Max(1, Math.Min(bmp.Width, bmp.Height) / 20);

            unsafe
            {
                byte* ptr = (byte*)data.Scan0;
                for (int y = 0; y < bmp.Height; y += step)
                {
                    for (int x = 0; x < bmp.Width; x += step)
                    {
                        int index = y * data.Stride + x * bytesPerPixel;
                        totalBrightness += ptr[index] + ptr[index + 1] + ptr[index + 2];
                        sampleCount++;
                    }
                }
            }

            if (sampleCount == 0) return false;
            double averageBrightness = (double)totalBrightness / (sampleCount * 3);
            return averageBrightness < 128;
        }
        finally
        {
            if (data != null)
            {
                bmp.UnlockBits(data);
            }
        }
    }

    private Bitmap InvertBitmap(Bitmap source)
    {
        Bitmap invertedImage = new Bitmap(source.Width, source.Height, source.PixelFormat);
        BitmapData sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
        BitmapData destData = invertedImage.LockBits(new Rectangle(0, 0, invertedImage.Width, invertedImage.Height), ImageLockMode.WriteOnly, invertedImage.PixelFormat);

        try
        {
            int bytesPerPixel = Image.GetPixelFormatSize(source.PixelFormat) / 8;
            if (bytesPerPixel < 3)
            {
                source.UnlockBits(sourceData);
                invertedImage.UnlockBits(destData);
                invertedImage.Dispose();
                return source;
            }
            int byteCount = sourceData.Stride * source.Height;
            byte[] pixels = new byte[byteCount];
            IntPtr ptrFirstPixel = sourceData.Scan0;
            Marshal.Copy(ptrFirstPixel, pixels, 0, byteCount);

            for (int i = 0; i < byteCount; i += bytesPerPixel)
            {
                pixels[i] = (byte)(255 - pixels[i]);
                pixels[i + 1] = (byte)(255 - pixels[i + 1]);
                pixels[i + 2] = (byte)(255 - pixels[i + 2]);
            }
            Marshal.Copy(pixels, 0, destData.Scan0, byteCount);
        }
        finally
        {
            source.UnlockBits(sourceData);
            invertedImage.UnlockBits(destData);
        }

        return invertedImage;
    }

    /// <summary>
    /// 直接截取屏幕 - 接收屏幕像素坐标，不进行 DPI 转换
    /// </summary>
    private Bitmap CaptureScreenDirect(System.Windows.Rect screenRect)
    {
        // 将坐标转换为整数像素
        int x = (int)Math.Round(screenRect.X);
        int y = (int)Math.Round(screenRect.Y);
        int width = (int)Math.Round(screenRect.Width);
        int height = (int)Math.Round(screenRect.Height);

        if (width <= 0 || height <= 0) 
        { 
            return new Bitmap(1, 1); 
        }

        var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            // 直接使用传入的屏幕坐标截图
            g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
        }
        return bmp;
    }
}