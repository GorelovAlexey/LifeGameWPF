using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows;

namespace LifeGame
{
    public class GraphicsController
    {
        Image _image;
        public Image Destination
        {
            get { return _image; }
            set
            {
                _image = value;
                _image.Source = WriteableBitmap;
            }
        }
        readonly Life Game;

        public WriteableBitmap WriteableBitmap { get; private set; }

        const double DpiX = 96;
        const double DpiY = 96;

        PixelFormat pixelFormat = PixelFormats.Bgr24;

        public byte[] DeadColor = new byte[] { 0, 0, 0 };
        public byte[] AliveColor = new byte[] { 0, 255, 0 };

        int x;
        int y;

        int _scale;
        public int Scale
        {
            get
            {
                return _scale;
            }
            set
            {
                if (_scale != value)
                {
                    _scale = value;
                    if (_scale < 1) _scale = 1;
                    if (_scale > 100) _scale = 100;
                    if (WriteableBitmap != null) Size = (width, height);
                }
            }
        }

        public (int x, int y) Position
        {
            get
            {
                return (x, y);
            }
            set
            {
                var (vx, vy) = value;
                if (vx + widthCells > Game.Width) vx = Game.Width - widthCells;
                if (vx < 0) vx = 0;
                if (vy + heightCells > Game.Height) vy = Game.Height - heightCells;
                if (vy < 0) vy = 0;

                if (vx != x || vy != y && WriteableBitmap != null)
                {
                    var dx = x - vx;
                    var dy = y - vy;
                    x = vx; y = vy;
                    MoveViewPoint(dx, dy);
                }
            }
        }

        double width;
        double height;

        public (double width, double height) Size
        {
            get
            {
                return (width, height);
            }
            set
            {
                width = value.width;
                height = value.height;

                widthCells = (int)width / Scale;
                if (width % Scale != 0) widthCells++;
                if (x + widthCells > Game.Width)
                {
                    x = Game.Width - widthCells;
                    if (x < 0)
                    {
                        x = 0;
                        widthCells = Game.Width;
                    }
                }

                heightCells = (int)height / Scale;
                if (height % Scale != 0) heightCells++;
                if (y + heightCells > Game.Height)
                {
                    y = Game.Height - heightCells;
                    if (y < 0)
                    {
                        y = 0;
                        heightCells = Game.Height;
                    }

                }

                WriteableBitmap = Resize(widthCells * Scale, heightCells * Scale);
                Destination.Source = WriteableBitmap;
                RedrawAllField();
            }
        }

        int widthCells;
        int heightCells;

        public (int x, int y) CoordConverter(double x, double y)
        {
            return ((int)x / Scale, (int)y / Scale);
        }



        private void Game_CellChanged(object sender, Life.CellChangedEventArgs e)
        {
            if (e.X >= (widthCells + x) || e.Y >= (widthCells + y) || e.X < x || e.Y < y) return;
            var color = (e.alive) ? AliveColor : DeadColor;
            Pixel(WriteableBitmap, e.X - x, e.Y - y, color);
        }

        public void UpdatePixel(int x, int y)
        {
            Game.Flip(this.x + x, this.y + y);
        }

        public void PaintPixel(int x, int y, bool paintAsAlive)
        {
            x += this.x;
            y += this.y;
            if (Game.Get(x, y) != paintAsAlive) Game.Flip(x, y);
        }


        public GraphicsController(Image destination, Life game, Size size, int scale = 1)
        {
            Game = game;
            Destination = destination;
            Game.CellChanged += Game_CellChanged;
            this.Scale = scale;
            Size = ((int)size.Width, (int)size.Height);
        }

        WriteableBitmap Resize(int width, int height)
        {
            var wbmap = new WriteableBitmap(
                    width, height,
                    DpiX, DpiY,
                   pixelFormat, null
                );
            return wbmap;
        }

        void RedrawAllField()
        {
            WriteableBitmap.Lock();
            UpdatePartOfField(0, 0, widthCells, heightCells);
            WriteableBitmap.AddDirtyRect(new Int32Rect(0, 0, widthCells * Scale, heightCells * Scale));
            WriteableBitmap.Unlock();
        }

        void MoveViewPoint(int cellXOffset, int cellYOffset)
        {
            if (cellXOffset == 0 && cellYOffset == 0) return;
            if (Math.Abs(cellXOffset) >= widthCells || Math.Abs(cellYOffset) >= heightCells) { RedrawAllField(); return; }
            WriteableBitmap.Lock();

            var bytesPerPixel = WriteableBitmap.Format.BitsPerPixel / 8;
            if (bytesPerPixel < 1) bytesPerPixel++;

            int xOffset = cellXOffset * Scale * bytesPerPixel;
            int yOffset = cellYOffset * Scale;

            int bytesWidth = WriteableBitmap.BackBufferStride;

            IntPtr buff = WriteableBitmap.BackBuffer;

            unsafe
            {
                var pByte = (byte*)buff.ToPointer();

                int xStart, xEnd, xStep;
                int yStart, yEnd, yStep;

                if (xOffset > 0)
                {
                    xStart = WriteableBitmap.PixelWidth * bytesPerPixel - 1;
                    xEnd = xOffset;
                    xStep = -1;
                }
                else
                {
                    xStart = 0;
                    xEnd = WriteableBitmap.PixelWidth * bytesPerPixel + xOffset - 1;
                    xStep = 1;
                }

                if (yOffset > 0)
                {
                    yStart = WriteableBitmap.PixelHeight - 1;
                    yEnd = yOffset;
                    yStep = -1;
                }
                else
                {
                    yStart = 0;
                    yEnd = WriteableBitmap.PixelHeight + yOffset - 1;
                    yStep = 1;
                }
                

                if (yStart != yEnd)
                {
                    for (int iY = yStart; ; iY += yStep)
                    {
                        if (xStart != xEnd)
                        {
                            for (int iX = xStart; ; iX += xStep)
                            {
                                pByte[iX + iY * bytesWidth] = pByte[iX - xOffset + (iY - yOffset) * bytesWidth];
                                if (iX == xEnd) break;
                            }
                        }
                        if (iY == yEnd) break;
                    }
                }


                int XO = Math.Abs(cellXOffset);
                int YO = Math.Abs(cellYOffset);

                if (cellXOffset > 0)
                {
                    UpdatePartOfField(0, 0, XO, heightCells);
                    if (cellYOffset > 0)
                    {
                        UpdatePartOfField(XO, 0, widthCells - XO, YO);
                    }
                    else if (cellYOffset < 0)
                    {
                        UpdatePartOfField(XO, heightCells - YO, widthCells - XO, YO);
                    }
                }
                else if (cellXOffset < 0)
                {
                    UpdatePartOfField(widthCells - XO, 0, XO, heightCells);
                    if (cellYOffset > 0)
                    {
                        UpdatePartOfField(0, 0, widthCells - XO, YO);
                    }
                    else if (cellYOffset < 0)
                    {
                        UpdatePartOfField(0, heightCells - YO, widthCells - XO, YO);
                    }
                }
                else
                {
                    if (cellYOffset > 0)
                    {
                        UpdatePartOfField(0, 0, widthCells, YO);
                    }
                    else if (cellYOffset < 0)
                    {
                        UpdatePartOfField(0, heightCells - YO, widthCells, YO);
                    }
                }

            }

            WriteableBitmap.AddDirtyRect(new Int32Rect(0, 0, WriteableBitmap.PixelWidth, WriteableBitmap.PixelHeight));
            WriteableBitmap.Unlock();
        }


        unsafe void UpdatePartOfField(int x, int y, int width, int height)
        {
            for (int j = y; j < y + height; j++)
                for (int i = x; i <  x + width; i++)
                {
                    UpdateCell(i, j);
                }
        }

        unsafe void UpdateCell(int x, int y)
        {
            int scale = Scale;
            var bytesPerPixel = WriteableBitmap.Format.BitsPerPixel / 8;
            if (bytesPerPixel < 1) bytesPerPixel++;

            var alive = Game.Get(this.x + x, this.y + y);
            var color = (alive) ? AliveColor : DeadColor;

            var pBuff = (byte*)WriteableBitmap.BackBuffer.ToPointer();

            for (int j = y * scale; j < scale + y * scale; j++)
            {
                for (int i = x * scale; i < scale + x * scale; i++)
                {
                    var wBuff = pBuff + i * bytesPerPixel + j * WriteableBitmap.BackBufferStride;
                    for (int c = 0; c < color.Length; c++) wBuff[c] = color[c];
                }
            }
        }

        void Pixel(WriteableBitmap WB, int x, int y, byte[] Color)
        {
            var pixelX = x * Scale;
            var pixelY = y * Scale;

            //if (pixelX < 0 || pixelY < 0) return;
            //if (pixelX > WB.PixelWidth || pixelY > WB.PixelHeight) { MessageBox.Show("ha"); return; }

            var w = Scale;
            while (w + pixelX >= WB.PixelWidth) w--;

            var h = Scale;
            while (h + pixelY >= WB.PixelHeight) h--;

            int stride = w * WB.Format.BitsPerPixel / 8;
            if (stride < 1) stride = 1;

            var pixels = new List<byte>();
            for (int i = 0; i < w * h; i++) pixels.AddRange(Color);

            WB.WritePixels(new Int32Rect(x * Scale, y * Scale, w, h), pixels.ToArray(), stride, 0);
        }

        // 0 1 2 3 4    5
        // 0 1 2    3

        public (int x, int y) ToCell(double x, double y, double width, double height)
        {
            x = (x + 0.5) / width * widthCells;
            while (x >= widthCells) --x;
            y = (y + 0.5) / width * widthCells;
            while (y >= heightCells) --y;

            return ((int)x, (int)y);
        }

    }


}


