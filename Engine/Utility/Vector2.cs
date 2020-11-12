using System;
using System.Collections.Generic;

struct Vector2
{
    public float X, Y;

    public static readonly Vector2 Zero = new Vector2(0, 0);

    /// <summary>
    /// Creates a new 2D vector.
    /// </summary>
    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public override string ToString()
    {
        return string.Format("({0}, {1})", X, Y);
    }

    /// <summary>
    /// Returns the length of this vector.
    /// </summary>
    public float Length()
    {
        return (float)Math.Sqrt(X * X + Y * Y);
    }

    /// <summary>
    /// Returns a copy of this vector rotated clockwise around the origin.
    /// </summary>
    /// <param name="degrees">The angle to rotate (in degrees).</param>
    public Vector2 Rotated(float degrees)
    {
        float radians = (float)(degrees * Math.PI / 180);
        float sin = (float)Math.Sin(radians);
        float cos = (float)Math.Cos(radians);
        return new Vector2(
            X * cos - Y * sin,
            X * sin + Y * cos);
    }

    /// <summary>
    /// Returns a copy of this vector normalized so that its length is one. If the length is zero it will be unchanged.
    /// </summary>
    public Vector2 Normalized()
    {
        float length = Length();
        if (length == 0)
        {
            return Vector2.Zero;
        }
        else
        {
            return this / length;
        }
    }

    /// <summary>
    /// Returns the dot product of two vectors.
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    public static float Dot(Vector2 a, Vector2 b)
    {
        return a.X * b.X + a.Y * b.Y;
    }

    /// <summary>
    /// Returns the Z component of the cross product of two vectors.
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    public static float Cross(Vector2 a, Vector2 b)
    {
        return a.X * b.Y - a.Y * b.X;
    }

    public static Vector2 operator -(Vector2 a)
    {
        return new Vector2(-a.X, -a.Y);
    }

    public static Vector2 operator +(Vector2 a, Vector2 b)
    {
        return new Vector2(a.X + b.X, a.Y + b.Y);
    }

    public static Vector2 operator -(Vector2 a, Vector2 b)
    {
        return new Vector2(a.X - b.X, a.Y - b.Y);
    }

    public static Vector2 operator *(float s, Vector2 v)
    {
        return new Vector2(s * v.X, s * v.Y);
    }

    public static Vector2 operator *(Vector2 v, float s)
    {
        return new Vector2(s * v.X, s * v.Y);
    }

    public static Vector2 operator *(int s, Vector2 v)
    {
        return new Vector2(s * v.X, s * v.Y);
    }

    public static Vector2 operator *(Vector2 v, int s)
    {
        return new Vector2(s * v.X, s * v.Y);
    }

    public static Vector2 operator /(Vector2 v, float s)
    {
        return new Vector2(v.X / s, v.Y / s);
    }

    public static Vector2 operator /(Vector2 v, int s)
    {
        return new Vector2(v.X / s, v.Y / s);
    }
}
