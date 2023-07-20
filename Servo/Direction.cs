using Microsoft.Xna.Framework;

namespace Servo;

public static class Direction
{
    public static readonly Point[] Directions =
    {
        new(0, -1), // Up
        new(0, 1), // Down
        new(-1, 0), // Left
        new(1, 0) // Right
    };
}