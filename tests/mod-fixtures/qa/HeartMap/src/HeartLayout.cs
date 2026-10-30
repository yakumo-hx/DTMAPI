using System;
using System.Collections.Generic;

namespace DTMAPI.Internal.HeartMap
{
    // Private experiment input, shared by native geometry, artwork and physics.
    // Pure data: no game objects, assets, filesystem or per-frame work.
    internal sealed class HeartLayout
    {
        internal const int Width = 48;
        internal const int Height = 40;
        internal const float CellSize = 1.5f;
        internal readonly bool[,] Solid = new bool[Width, Height];
        internal readonly bool[,] Platform = new bool[Width, Height];
        internal readonly List<Cell> Ground = new List<Cell>();
        internal readonly List<Cell> Obstacles = new List<Cell>();
        internal readonly List<Cell> Platforms = new List<Cell>();
        internal const int SpawnX = 24;
        internal const int SpawnY = 6;

        internal HeartLayout()
        {
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                double u = (x + .5 - Width / 2d) / 19d;
                double v = (y + .5 - 18d) / 14.5d;
                double a = u * u + v * v - 1d;
                Solid[x, y] = a * a * a - u * u * v * v * v > 0 ||
                    x == 0 || y == 0 || x == Width - 1 || y == Height - 1;
            }

            // A stable starting ledge; one-way rungs remain open to upward jumps.
            Ledge(21, 26, 5);
            Ledge(20, 27, 8);
            Ledge(16, 24, 11);
            Ledge(21, 30, 14);
            Ledge(25, 34, 17);
            Ledge(20, 29, 20);
            Ledge(14, 24, 23);
            Ledge(8, 39, 26);
            Ledge(8, 17, 29);
            Ledge(30, 39, 29);

            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                if (Solid[x, y]) Obstacles.Add(new Cell(x, y));
                if (Platform[x, y]) Platforms.Add(new Cell(x, y));
                if (!Solid[x, y] && y > 0 && (Solid[x, y - 1] || Platform[x, y - 1]))
                    Ground.Add(new Cell(x, y));
            }
            Validate();
        }

        private void Ledge(int left, int right, int y)
        {
            for (int x = left; x <= right; x++)
            {
                if (Solid[x, y] || Solid[x, y + 1] || Solid[x, y + 2])
                    throw new InvalidOperationException("Platform intersects the heart boundary: " + x + "," + y);
                Platform[x, y] = true;
            }
        }

        internal bool HasStandingClearance(int x, int y) =>
            x > 0 && x + 1 < Width && y > 0 && y + 1 < Height &&
            !Solid[x, y] && !Solid[x + 1, y] && !Solid[x, y + 1] && !Solid[x + 1, y + 1] &&
            (Solid[x, y - 1] || Platform[x, y - 1]) &&
            (Solid[x + 1, y - 1] || Platform[x + 1, y - 1]);

        internal void Validate()
        {
            if (!HasStandingClearance(SpawnX, SpawnY) ||
                !HasStandingClearance(12, 30) || !HasStandingClearance(34, 30))
                throw new InvalidOperationException("Spawn or lobe landing has insufficient clearance.");
            var seen = new bool[Width, Height];
            var queue = new Queue<Cell>();
            queue.Enqueue(new Cell(SpawnX, SpawnY));
            seen[SpawnX, SpawnY] = true;
            int count = 0;
            while (queue.Count > 0)
            {
                Cell cell = queue.Dequeue();
                count++;
                Visit(cell.X - 1, cell.Y); Visit(cell.X + 1, cell.Y);
                Visit(cell.X, cell.Y - 1); Visit(cell.X, cell.Y + 1);
            }
            if (count != Width * Height - Obstacles.Count)
                throw new InvalidOperationException("Heart cavity is disconnected.");
            void Visit(int x, int y)
            {
                if (x < 0 || y < 0 || x >= Width || y >= Height || Solid[x, y] || seen[x, y]) return;
                seen[x, y] = true;
                queue.Enqueue(new Cell(x, y));
            }
        }

        internal readonly struct Cell
        {
            internal Cell(int x, int y) { X = x; Y = y; }
            internal int X { get; }
            internal int Y { get; }
        }
    }
}
