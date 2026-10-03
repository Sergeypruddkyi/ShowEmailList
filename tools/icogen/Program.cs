using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;

namespace icogen
{
    // Converts a high-res source PNG into a multi-size ICO (PNG-compressed frames).
    // Args: <source.png> <out.ico>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("usage: icogen <source.png> <out.ico>");
                return 2;
            }
            string src = args[0];
            string dst = args[1];
            int[] sizes = { 16, 24, 32, 48, 64, 128, 256 };

            using (Image source = Image.FromFile(src))
            {
                List<byte[]> frames = new List<byte[]>();
                foreach (int s in sizes)
                {
                    using (Bitmap bmp = new Bitmap(s, s))
                    {
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.SmoothingMode = SmoothingMode.HighQuality;
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            g.Clear(Color.Transparent);
                            g.DrawImage(source, 0, 0, s, s);
                        }
                        using (MemoryStream ms = new MemoryStream())
                        {
                            bmp.Save(ms, ImageFormat.Png);
                            frames.Add(ms.ToArray());
                        }
                    }
                }

                using (FileStream fs = File.Create(dst))
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    bw.Write((ushort)0);                 // reserved
                    bw.Write((ushort)1);                 // type = icon
                    bw.Write((ushort)frames.Count);      // image count
                    int offset = 6 + 16 * frames.Count;
                    for (int i = 0; i < frames.Count; i++)
                    {
                        int s = sizes[i];
                        byte[] data = frames[i];
                        bw.Write((byte)(s >= 256 ? 0 : s)); // width (0 = 256)
                        bw.Write((byte)(s >= 256 ? 0 : s)); // height
                        bw.Write((byte)0);               // palette colors
                        bw.Write((byte)0);               // reserved
                        bw.Write((ushort)1);             // color planes
                        bw.Write((ushort)32);            // bits per pixel
                        bw.Write((uint)data.Length);     // data size
                        bw.Write((uint)offset);          // data offset
                        offset += data.Length;
                    }
                    foreach (byte[] data in frames) bw.Write(data);
                    bw.Flush();
                }
            }
            Console.WriteLine("Wrote " + dst + " (" + new FileInfo(dst).Length + " bytes)");
            return 0;
        }
    }
}
