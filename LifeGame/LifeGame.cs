using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LifeGame
{
    public class Life
    {
        readonly bool[][] field;


        public readonly bool Wrapping;

        public bool Get(int x, int y)
        {           
            return field[y][x];
        }

        public int Height { get; private set; }
        public int Width { get; private set; }

        public Life(int width, int height, bool wrap = false)
        {
            field = new bool[height][];
            for (int i = 0; i < field.Length; i++) field[i] = new bool[width];
            Height = height;
            Width = width;
            Wrapping = wrap;
        }

        public void Flip(int x, int y)
        {
            field[y][x] = !field[y][x];
            CellChanged(this, new CellChangedEventArgs() { X = x, Y = y, alive = field[y][x] });
        }       

        public void Tick()
        {
            var changes = new List<(int x, int y)>();
            object l = new object();

            Parallel.For(1, Height - 1, (y) =>
            {
                for (int x = 1; x < Width - 1; x++)
                {
                    int neighbours = CountAliveNeighbours(x, y);
                    if (field[y][x])
                    {
                        if (neighbours < 2 || neighbours > 3) lock (l) { changes.Add((x, y)); }
                    }
                    else if (neighbours == 3) lock (l) { changes.Add((x, y)); }
                }
            });

            for (int x = 0; x < Width; x++)
            {
                int y = 0;
                int neighbours = CountAliveNeighboursBorder(x, y);
                if (field[y][x])
                {
                    if (neighbours < 2 || neighbours > 3) changes.Add((x, y));
                }
                else if (neighbours == 3) changes.Add((x, y));

                y = Height - 1;
                neighbours = CountAliveNeighboursBorder(x, y);
                if (field[y][x])
                {
                    if (neighbours < 2 || neighbours > 3) changes.Add((x, y));
                }
                else if (neighbours == 3) changes.Add((x, y));
            }

            for (int y = 1; y < Height - 1; y++)
            {
                int x = 0;
                int neighbours = CountAliveNeighboursBorder(x, y);
                if (field[y][x])
                {
                    if (neighbours < 2 || neighbours > 3) changes.Add((x, y));
                }
                else if (neighbours == 3) changes.Add((x, y));

                x = Width - 1;
                neighbours = CountAliveNeighboursBorder(x, y);
                if (field[y][x])
                {
                    if (neighbours < 2 || neighbours > 3) changes.Add((x, y));
                }
                else if (neighbours == 3) changes.Add((x, y));
            }

            foreach (var (x, y) in changes) Flip(x, y);
        }

        int CountAliveNeighbours (int x, int y)
        {
            int neighbours = 0;
            for (int nx = x - 1; nx <= x + 1; nx++)
                for (int ny = y - 1; ny <= y + 1; ny++)
                {
                    if (nx == x && ny == y) continue;
                    if (field[ny][nx])
                        if (++neighbours > 3) break;
                }
            return neighbours;
        }

        int CountAliveNeighboursBorder(int x, int y)
        {
            int neighbours = 0;
            if (!Wrapping)
            {
                for (int iX = x - 1; iX <= x + 1; iX++)
                    for (int iY = y - 1; iY <= y + 1; iY++)
                    {
                        if (iX < 0 || iY < 0) continue;
                        if (iX >= Width || iY >= Height) continue;
                        if (iX == x && iY == y) continue;
                        if (field[iY][iX])
                            if (++neighbours > 3) break;
                    }
            }
            else
            {
                for (int iX = x - 1; iX <= x + 1; iX++)
                    for (int iY = y - 1; iY <= y + 1; iY++)
                    {
                        if (iX == x && iY == y) continue;

                        int pX = iX;
                        int pY = iY;

                        if (pX == -1) pX += Width;
                        else if (pX == Width) pX -= Width;

                        if (pY == -1) pY += Height;
                        else if (pY == Height) pY -= Height;

                        if (field[pY][pX])
                            if (++neighbours > 3) break;
                    }
            }

            return neighbours;
        }


        public class CellChangedEventArgs : EventArgs
        {
            public int X;
            public int Y;
            public bool alive;
        }

        public event EventHandler<CellChangedEventArgs> CellChanged = (o,e) => { }; 

    }
}
