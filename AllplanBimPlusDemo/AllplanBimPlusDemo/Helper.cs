using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BimPlus.Sdk.Data.CSG;
using BimPlus.Sdk.Data.DbCore;
using BimPlus.Sdk.Data.DbCore.Building;
using BimPlus.Sdk.Data.Geometry;
using Newtonsoft.Json.Linq;
using Path = System.IO.Path;

namespace BimPlusDemo
{
    internal static class Helper
    {
        public static void Execute(byte[] data, string name)
        {
            if (data == null)
                return;

            string path = @"c:\temp"; //\MyTest.glb";
            DirectoryInfo info = new DirectoryInfo(path);
            if (!info.Exists)
                info.Create();

            string filepath = Path.Combine(path, name);
            File.WriteAllBytes(filepath, data);

            Process.Start(filepath);
        }

        //public static string ConvertToBitmapSource(UIElement element)
        //{
        //    PresentationSource source = PresentationSource.FromVisual(element);
        //    RenderTargetBitmap rtb = new RenderTargetBitmap((int)element.RenderSize.Width,
        //        (int)element.RenderSize.Height, 96, 96, PixelFormats.Default);

        //    element.UpdateLayout();
        //    VisualBrush sourceBrush = new VisualBrush(element);
        //    DrawingVisual drawingVisual = new DrawingVisual();
        //    DrawingContext drawingContext = drawingVisual.RenderOpen();
        //    using (drawingContext)
        //    {
        //        drawingContext.DrawRectangle(sourceBrush, null, new Rect(new Point(0, 0),
        //            new Point(element.RenderSize.Width, element.RenderSize.Height)));
        //    }
        //    rtb.Render(drawingVisual);

        //    PngBitmapEncoder encoder = new PngBitmapEncoder();
        //    BitmapFrame outputFrame = BitmapFrame.Create(rtb);
        //    encoder.Frames.Add(outputFrame);
        //    using (FileStream file = File.OpenWrite("TestImage.png"))
        //    {
        //        encoder.Save(file);
        //        return file.Name;
        //    }
        //    return null;
        //}

        public static FileStream WriteToPng(UIElement element, string filename)
        {
            var rect = new Rect(element.RenderSize);
            var visual = new DrawingVisual();

            using (var dc = visual.RenderOpen())
            {
                dc.DrawRectangle(new VisualBrush(element), null, rect);
            }

            var bitmap = new RenderTargetBitmap(
                (int) rect.Width, (int) rect.Height, 96, 96, PixelFormats.Default);
            bitmap.Render(visual);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (var file = File.OpenWrite(filename))
            {
                encoder.Save(file);
                return file;
            }
        }

        /// <summary>
        /// Copies a UI element to the clipboard as an image.
        /// </summary>
        /// <param name="element">The element to copy.</param>
        // ReSharper disable once InconsistentNaming
        public static void CopyUIElementToClipboard(FrameworkElement element)
        {
            double width = element.ActualWidth;
            double height = element.ActualHeight;
            RenderTargetBitmap bmpCopied = new RenderTargetBitmap((int) Math.Round(width), (int) Math.Round(height), 96,
                96, PixelFormats.Default);
            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(element);
                dc.DrawRectangle(vb, null, new Rect(new Point(), new Size(width, height)));
            }

            bmpCopied.Render(dv);
            Clipboard.SetImage(bmpCopied);
        }

        public static bool AddGeometry(DtObject dtObject, JObject geometryResource)
        {
            try
            {
                var geo = geometryResource.Children().FirstOrDefault();
                if (geo == null)
                    return false;

                switch (geo.Path)
                {
                    case "csg":
                    {
                        var csgTree = geo.First.ToObject<DtoCsgTree>();
                        if (dtObject is RootElement element)
                            element.CsgTree = csgTree;
                        else
                            // TODO: 'ReinforcingBar' has missing relation to RootElement.
                            dtObject.AddProperty(TableNames.tabAttribPicture, "csg", csgTree);
                        return true;
                    }
                    case "mesh":
                    {
                        var mesh = geo.First.ToObject<DbGeometry>();
                        if (dtObject is RootElement element)
                            element.Mesh = mesh;
                        else
                            dtObject.AddProperty(TableNames.tabAttribPicture, "mesh", mesh);
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }

            return true;
        }
    }
}
