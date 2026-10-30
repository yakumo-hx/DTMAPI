using System;
using System.Collections.Generic;

namespace DTMAPI.Doloc25D
{
    internal readonly struct Cell : IEquatable<Cell>
    {
        public readonly int X, Y;
        public Cell(int x, int y) { X = x; Y = y; }
        public bool Equals(Cell c) => X == c.X && Y == c.Y;
        public override bool Equals(object? o) => o is Cell c && Equals(c);
        public override int GetHashCode() => unchecked(X * 397 ^ Y);
    }
    internal readonly struct Edge
    {
        public readonly int X0, Y0, X1, Y1;
        public Edge(int x0, int y0, int x1, int y1) { X0 = x0; Y0 = y0; X1 = x1; Y1 = y1; }
    }
    internal static class Footprint
    {
        // Counter-clockwise perimeter only: adjacent cells never create internal walls.
        public static List<Edge> Boundary(HashSet<Cell> cells)
        {
            var edges = new List<Edge>();
            foreach (var c in cells)
            {
                if (!cells.Contains(new Cell(c.X, c.Y - 1))) edges.Add(new Edge(c.X, c.Y, c.X + 1, c.Y));
                if (!cells.Contains(new Cell(c.X + 1, c.Y))) edges.Add(new Edge(c.X + 1, c.Y, c.X + 1, c.Y + 1));
                if (!cells.Contains(new Cell(c.X, c.Y + 1))) edges.Add(new Edge(c.X + 1, c.Y + 1, c.X, c.Y + 1));
                if (!cells.Contains(new Cell(c.X - 1, c.Y))) edges.Add(new Edge(c.X, c.Y + 1, c.X, c.Y));
            }
            return edges;
        }
    }
    internal readonly struct Point3
    {
        public readonly double X, Y, Z;
        public Point3(double x, double y, double z) { X = x; Y = y; Z = z; }
        public static Point3 operator +(Point3 a, Point3 b) => new Point3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Point3 operator -(Point3 a, Point3 b) => new Point3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Point3 operator *(Point3 a, double b) => new Point3(a.X * b, a.Y * b, a.Z * b);
        public static double Dot(Point3 a, Point3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    }
    internal sealed class ViewProjection
    {
        public Point3 Position, Right, Up, Forward;
        public double X, Y, Width, Height, TanHalf;
        public void Configure(double pivotX, double pivotY, double size, double yaw, double pitch,
            double scale, double x, double y, double width, double height)
        {
            double a = yaw * Math.PI / 180, b = pitch * Math.PI / 180;
            Right = new Point3(Math.Cos(a), 0, -Math.Sin(a));
            Up = new Point3(Math.Sin(a) * Math.Sin(b), Math.Cos(b), Math.Cos(a) * Math.Sin(b));
            Forward = new Point3(Math.Sin(a) * Math.Cos(b), -Math.Sin(b), Math.Cos(a) * Math.Cos(b));
            TanHalf = Math.Tan(35 * Math.PI / 360);
            Position = new Point3(pivotX, pivotY, 0) - Forward * (size * scale / TanHalf);
            X = x; Y = y; Width = width; Height = height;
        }
        public Point3 Project(Point3 world)
        {
            Point3 relative = world - Position;
            double z = Point3.Dot(relative, Forward);
            return new Point3(X + Width * .5 * (1 + Point3.Dot(relative, Right) / (z * TanHalf * Width / Height)),
                Y + Height * .5 * (1 + Point3.Dot(relative, Up) / (z * TanHalf)), z);
        }
        public Point3 Unproject(double x, double y)
        {
            Point3 direction = Forward + Right * ((2 * (x - X) / Width - 1) * TanHalf * Width / Height)
                + Up * ((2 * (y - Y) / Height - 1) * TanHalf);
            if (Math.Abs(direction.Z) < 1e-8) throw new InvalidOperationException("View ray is parallel to the gameplay plane.");
            return Position + direction * (-Position.Z / direction.Z);
        }
    }
}
