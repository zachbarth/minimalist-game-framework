using System;
using System.Collections.Generic;

class Game
{
    public static readonly string Title = "Minimalist Game Framework";
    public static readonly Vector2 Resolution = new Vector2(128, 128);

    // Define some constants controlling animation speed:
    static readonly float Framerate = 10;
    static readonly float WalkSpeed = 50;

    // Load some textures when the game starts:
    Texture texKnight = Engine.LoadTexture("knight.png");
    Texture texBackground = Engine.LoadTexture("background.png");

    // Keep track of the knight's state:
    Vector2 knightPosition = Resolution / 2;
    bool knightFaceLeft = false;
    float knightFrameIndex = 0;

    public Game()
    {
    }

    public void Update()
    {
        // Draw the background:
        Engine.DrawTexture(texBackground, Vector2.Zero);

        // Use the keyboard to control the knight:
        Vector2 moveOffset = Vector2.Zero;
        if (Engine.GetKeyHeld(Key.Left))
        {
            moveOffset.X -= 1;
            knightFaceLeft = true;
        }
        if (Engine.GetKeyHeld(Key.Right))
        {
            moveOffset.X += 1;
            knightFaceLeft = false;
        }
        if (Engine.GetKeyHeld(Key.Up))
        {
            moveOffset.Y -= 1;
        }
        if (Engine.GetKeyHeld(Key.Down))
        {
            moveOffset.Y += 1;
        }
        knightPosition += moveOffset * WalkSpeed * Engine.TimeDelta;

        // Advance through the knight's 6-frame animation and select the current frame:
        knightFrameIndex = (knightFrameIndex + Engine.TimeDelta * Framerate) % 6.0f;
        bool knightIdle = moveOffset.Length() == 0;
        Bounds2 knightFrameBounds = new Bounds2(((int)knightFrameIndex) * 16, knightIdle ? 0 : 16, 16, 16);

        // Draw the knight:
        Vector2 knightDrawPos = knightPosition + new Vector2(-8, -8);
        TextureMirror knightMirror = knightFaceLeft ? TextureMirror.Horizontal : TextureMirror.None;
        Engine.DrawTexture(texKnight, knightDrawPos, source: knightFrameBounds, mirror: knightMirror);
    }
}
