using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
class MakeIcon {
    static void Main(string[] args) {
        int[] sizes = {16,20,24,32,40,48,64,128,256};
        var frames = new List<byte[]>();
        foreach (int size in sizes) {
            using (var large = new Bitmap(size * 4, size * 4, PixelFormat.Format32bppArgb)) {
                using (var g = Graphics.FromImage(large)) {
                    g.Clear(Color.Transparent); g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.ScaleTransform(size * 4F / 64, size * 4F / 64);
                    Color navy = Color.FromArgb(30,40,68);
                    using(var bg = new SolidBrush(navy)) g.FillEllipse(bg, 1,1,62,62);
                    using(var moon = new SolidBrush(Color.FromArgb(255,220,135))) {
                        using(var shape = new GraphicsPath()) {
                            shape.AddEllipse(12,12,40,40);
                            using (var region = new Region(shape)) {
                                using (var cut = new GraphicsPath()) { cut.AddEllipse(26,5,36,36); region.Exclude(cut); }
                                g.FillRegion(moon, region);
                            }
                        }
                    }
                    using (var star = new SolidBrush(Color.FromArgb(228,238,255))) {
                        g.FillPolygon(star, new PointF[] {new PointF(43,13),new PointF(45,19),new PointF(51,21),new PointF(45,23),new PointF(43,29),new PointF(41,23),new PointF(35,21),new PointF(41,19)});
                        if(size >=24) g.FillEllipse(star,51,33,4,4);
                    }
                }
                using(var bitmap = new Bitmap(size,size,PixelFormat.Format32bppArgb)) {
                    using(var g = Graphics.FromImage(bitmap)) { g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.DrawImage(large,0,0,size,size); }
                    if(size==128) bitmap.Save(Path.Combine(args[0],"icon-preview.png"),ImageFormat.Png);
                    using(var stream = new MemoryStream()) { bitmap.Save(stream,ImageFormat.Png); frames.Add(stream.ToArray()); }
                }
            }
        }
        using(var file=File.Create(Path.Combine(args[0],"NightScreenGuard.ico"))) using(var writer=new BinaryWriter(file)) {
            writer.Write((ushort)0); writer.Write((ushort)1); writer.Write((ushort)sizes.Length);
            int offset=6+16*sizes.Length;
            for(int i=0;i<sizes.Length;i++) { writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)0);writer.Write((byte)0);writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(frames[i].Length);writer.Write(offset);offset+=frames[i].Length; }
            foreach(var bytes in frames) writer.Write(bytes);
        }
        using(var ico=new Icon(Path.Combine(args[0],"NightScreenGuard.ico"),16,16)) using(var bmp=ico.ToBitmap()) bmp.Save(Path.Combine(args[0],"icon-16.png"),ImageFormat.Png);
    }
}
