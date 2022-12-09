using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using AvaloniaEdit.Editing;
using SkiaSharp;

namespace Microcharts.Avalonia
{
    public class ChartDrawOperation : ICustomDrawOperation
    {
        public ChartView Parent { get; }

        public ChartDrawOperation(ChartView parent)
        {
            Parent = parent;
        }

        public void Dispose() {
            mBackCanvas?.Dispose();
            mBackBuffer?.Dispose();
            mBackCanvas = null;
            mBackBuffer = null;
        }
        public bool HitTest(Point p) => Parent.Bounds.Contains(p);
        public bool Equals(ICustomDrawOperation ?other) => (other!=null) ? this == other : false;

        SKBitmap ?mBackBuffer = null;
        SKCanvas ?mBackCanvas = null;

        public void Render(IDrawingContextImpl context)
        {
            if (mBackBuffer == null || mBackBuffer.Width != Parent.Bounds.Width || mBackBuffer.Height != Parent.Bounds.Height)
            {
                mBackCanvas?.Dispose();
                mBackBuffer?.Dispose();
                mBackBuffer = new SKBitmap((int)Parent.Bounds.Width, (int)Parent.Bounds.Height, false);
                mBackCanvas = new SKCanvas(mBackBuffer);
                mBackCanvas.Save();
            }

            try
            {
                if (Parent.Chart != null)
                {
                    lock (Parent.Chart.Entries)
                        Parent.Chart.Draw(mBackCanvas, (int)Parent.Bounds.Width, (int)Parent.Bounds.Height);
                }

                if (context is ISkiaDrawingContextImpl skia)
                {
                    skia.SkCanvas.DrawBitmap(mBackBuffer, 0, 0);
                }
            } 
            catch (Exception ex)
            { 
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        public Rect Bounds => Parent.Bounds;
    }
}
