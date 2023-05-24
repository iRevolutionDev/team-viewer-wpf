using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Client.Services;

public class Screen
{
    public delegate void OnFrameEvent(byte[] data);

    public event OnFrameEvent? OnFrame;

    public void Start()
    {
        while (OnFrame != null)
            try
            {
                Thread.Sleep(1000 / 60);
                OnFrame(TakeScreenshot());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
    }

    public static byte[] TakeScreenshot()
    {
        //screen bounds
        var left = (int)SystemParameters.VirtualScreenLeft;
        var top = (int)SystemParameters.VirtualScreenTop;
        var width = (int)SystemParameters.VirtualScreenWidth;
        var height = (int)SystemParameters.VirtualScreenHeight;


        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        bitmap.SetResolution(96, 96);

        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(left, top, 0, 0, bitmap.Size);

        using var stream = new MemoryStream();

        bitmap.Save(stream, ImageFormat.Jpeg);

        return stream.ToArray();
    }

    public static ImageSource FromBytes(byte[] bytes)
    {
        try
        {
            var image = new BitmapImage();
            using var stream = new MemoryStream(bytes);
            stream.Position = 0;
            image.BeginInit();
            image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
            throw;
        }
    }
}