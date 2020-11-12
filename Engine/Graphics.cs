using SDL2;
using System;
using System.Collections.Generic;

static partial class Engine
{
    private static IntPtr Renderer;
    private static Dictionary<Tuple<Font, string>, TextCacheEntry> TextCache = new Dictionary<Tuple<Font, string>, TextCacheEntry>();

    // ======================================================================================
    // Primitive drawing
    // ======================================================================================

    private static void DrawPrimitiveSetup(Color color)
    {
        SDL.SDL_SetRenderDrawColor(Renderer, color.R, color.G, color.B, color.A);
        SDL.SDL_SetRenderDrawBlendMode(Renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
    }

    /// <summary>
    /// Draws a line.
    /// </summary>
    /// <param name="start">The start of the line.</param>
    /// <param name="end">The end of the line.</param>
    /// <param name="color">The color of the line.</param>
    public static void DrawLine(Vector2 start, Vector2 end, Color color)
    {
        DrawPrimitiveSetup(color);

        SDL.SDL_RenderDrawLine(Renderer, (int)start.X, (int)start.Y, (int)end.X, (int)end.Y);
    }

    /// <summary>
    /// Draws an empty rectangle.
    /// </summary>
    /// <param name="bounds">The bounds of the rectangle.</param>
    /// <param name="color">The color of the rectangle.</param>
    public static void DrawRectEmpty(Bounds2 bounds, Color color)
    {
        DrawPrimitiveSetup(color);

        SDL.SDL_Rect rect = new SDL.SDL_Rect();
        rect.x = (int)bounds.Position.X;
        rect.y = (int)bounds.Position.Y;
        rect.w = (int)bounds.Size.X;
        rect.h = (int)bounds.Size.Y;
        SDL.SDL_RenderDrawRect(Renderer, ref rect);
    }

    /// <summary>
    /// Draws a solid rectangle.
    /// </summary>
    /// <param name="bounds">The bounds of the rectangle.</param>
    /// <param name="color">The color of the rectangle.</param>
    public static void DrawRectSolid(Bounds2 bounds, Color color)
    {
        DrawPrimitiveSetup(color);

        SDL.SDL_Rect rect = new SDL.SDL_Rect();
        rect.x = (int)bounds.Position.X;
        rect.y = (int)bounds.Position.Y;
        rect.w = (int)bounds.Size.X;
        rect.h = (int)bounds.Size.Y;
        SDL.SDL_RenderFillRect(Renderer, ref rect);
    }

    // ======================================================================================
    // Texture drawing
    // ======================================================================================

    private static void DrawTextureSetup(IntPtr textureHandle, Color? color, TextureBlendMode blendMode, TextureScaleMode scaleMode)
    {
        Color finalColor = color ?? Color.White;
        SDL.SDL_SetTextureColorMod(textureHandle, finalColor.R, finalColor.G, finalColor.B);
        SDL.SDL_SetTextureAlphaMod(textureHandle, finalColor.A);
        SDL.SDL_SetTextureBlendMode(textureHandle, (SDL.SDL_BlendMode)blendMode);
        SDL.SDL_SetTextureScaleMode(textureHandle, (SDL.SDL_ScaleMode)scaleMode);
    }

    /// <summary>
    /// Draws a texture.
    /// </summary>
    /// <param name="texture">The texture to draw.</param>
    /// <param name="position">The position where the texture will be drawn.</param>
    /// <param name="color">The color to multiply with the colors of the texture. If unspecified the colors of the texture will be unchanged.</param>
    /// <param name="size">The destination size of the texture. If unspecified the original texture size will be used.</param>
    /// <param name="rotation">The amount the texture will be rotated clockwise (in degrees). If unspecified the texture will not be rotated.</param>
    /// <param name="pivot">The offset from position to the pivot that the texture will be rotated about. If unspecified the center of the destination bounds will be used.</param>
    /// <param name="mirror">The mirroring to apply to the texture. If unspecified the texture will not be mirrored.</param>
    /// <param name="source">The source bounds of the texture to draw. If unspecified the entire texture will be drawn.</param>
    /// <param name="blendMode">The blend mode to use when drawing the texture. If unspecified the texture will be drawn using the standard alpha-based blend mode.</param>
    /// <param name="scaleMode">The scale mode to use when drawing the texture. If unspecified the texture will be linearly interpolated.</param>
    public static void DrawTexture(Texture texture, Vector2 position, Color? color = null, Vector2? size = null, float rotation = 0, Vector2? pivot = null, TextureMirror mirror = TextureMirror.None, Bounds2? source = null, TextureBlendMode blendMode = TextureBlendMode.Normal, TextureScaleMode scaleMode = TextureScaleMode.Linear)
    {
        DrawTextureSetup(texture.Handle, color, blendMode, scaleMode);

        SDL.SDL_Rect src;
        SDL.SDL_Rect dest;
        if (source.HasValue)
        {
            // Use the specified source coordinates:
            src.x = (int)source.Value.Position.X;
            src.y = (int)source.Value.Position.Y;
            src.w = (int)source.Value.Size.X;
            src.h = (int)source.Value.Size.Y;
            dest.x = (int)position.X;
            dest.y = (int)position.Y;
            dest.w = src.w;
            dest.h = src.h;
        }
        else
        {
            // Use the full texture as the source:
            src.x = 0;
            src.y = 0;
            src.w = texture.Width;
            src.h = texture.Height;
            dest.x = (int)position.X;
            dest.y = (int)position.Y;
            dest.w = texture.Width;
            dest.h = texture.Height;
        }

        // Apply the size override, if specified:
        if (size.HasValue)
        {
            dest.w = (int)size.Value.X;
            dest.h = (int)size.Value.Y;
        }

        // Apply the pivot override, if specified:
        SDL.SDL_Point center;
        if (pivot.HasValue)
        {
            center.x = (int)pivot.Value.X;
            center.y = (int)pivot.Value.Y;
        }
        else
        {
            center.x = dest.w / 2;
            center.y = dest.h / 2;
        }

        SDL.SDL_RenderCopyEx(Renderer, texture.Handle, ref src, ref dest, rotation, ref center, (SDL.SDL_RendererFlip)mirror);
    }

    /// <summary>
    /// Draws a resizable texture.
    /// See the documentation for an explanation of how resizable textures work.
    /// </summary>
    /// <param name="texture">The resizable texture to draw.</param>
    /// <param name="bounds">The bounds that the texture should be resized to.</param>
    /// <param name="color">The color to multiply with the colors of the texture. If unspecified the colors of the texture will be unchanged.</param>
    /// <param name="blendMode">The blend mode to use when drawing the texture. If unspecified the texture will be drawn using the standard alpha-based blend mode.</param>
    /// <param name="scaleMode">The scale mode to use when drawing the texture. If unspecified the texture will be linearly interpolated.</param>
    public static void DrawResizableTexture(ResizableTexture texture, Bounds2 bounds, Color? color = null, TextureBlendMode blendMode = TextureBlendMode.Normal, TextureScaleMode scaleMode = TextureScaleMode.Linear)
    {
        DrawTextureSetup(texture.Handle, color, blendMode, scaleMode);

        /*    
         *   0    bxmin     bxmax    txmax
         *   v    v             v    v
         *   +----|-------------|----+ < tymax
         *   | 1  |      5      | 2  |
         *   -----+-------------+----- < bymax
         *   |    |             |    |
         *   |    |             |    |
         *   | 6  |      9      | 7  |
         *   |    |             |    |
         *   |    |             |    |
         *   -----+-------------+----- < bymin
         *   | 3  |      8      | 4  |
         *   +----|-------------|----+ < 0
         */

        int bxmin = texture.LeftOffset;
        int bxmax = texture.RightOffset;
        int bymin = texture.TopOffset;
        int bymax = texture.BottomOffset;
        int txmax = texture.Width;
        int tymax = texture.Height;
        int px = (int)bounds.Position.X;
        int py = (int)bounds.Position.Y;
        
        // Don't let the overall size be so small that segment 9 has a negative size in either dimension:
        int sx = Math.Max((int)bounds.Size.X, txmax - bxmax + bxmin);
        int sy = Math.Max((int)bounds.Size.Y, tymax - bymax + bymin);

        // Draw each of the nine segments:
        DrawResizableTextureSegment(texture, 0, 0, bxmin, bymin, px, py, bxmin, bymin);
        DrawResizableTextureSegment(texture, bxmax, 0, txmax - bxmax, bymin, px + sx - (txmax - bxmax), py, txmax - bxmax, bymin);
        DrawResizableTextureSegment(texture, 0, bymax, bxmin, tymax - bymax, px, py + sy - (tymax - bymax), bxmin, tymax - bymax);
        DrawResizableTextureSegment(texture, bxmax, bymax, txmax - bxmax, tymax - bymax, px + sx - (txmax - bxmax), py + sy - (tymax - bymax), txmax - bxmax, tymax - bymax);
        DrawResizableTextureSegment(texture, bxmin, 0, bxmax - bxmin, bymin, px + bxmin, py, sx - bxmin - (txmax - bxmax), bymin);
        DrawResizableTextureSegment(texture, 0, bymin, bxmin, bymax - bymin, px, py + bymin, bxmin, sy - bymin - (tymax - bymax));
        DrawResizableTextureSegment(texture, bxmax, bymin, txmax - bxmax, bymax - bymin, px + sx - (txmax - bxmax), py + bymin, txmax - bxmax, sy - bymin - (tymax - bymax));
        DrawResizableTextureSegment(texture, bxmin, bymax, bxmax - bxmin, tymax - bymax, px + bxmin, py + sy - (tymax - bymax), sx - bxmin - (txmax - bxmax), tymax - bymax);
        DrawResizableTextureSegment(texture, bxmin, bymin, bxmax - bxmin, bymax - bymin, px + bxmin, py + bymin, sx - bxmin - (txmax - bxmax), sy - bymin - (tymax - bymax));
    }

    private static void DrawResizableTextureSegment(ResizableTexture texture, int subtextureX, int subtextureY, int subtextureW, int subtextureH, int destX, int destY, int destW, int destH)
    {
        // Don't draw invisible segments:
        if (subtextureW <= 0 || subtextureH <= 0)
        {
            return;
        }

        SDL.SDL_Rect src;
        src.x = subtextureX;
        src.y = subtextureY;
        src.w = subtextureW;
        src.h = subtextureH;

        SDL.SDL_Rect dest;
        dest.x = destX;
        dest.y = destY;
        dest.w = destW;
        dest.h = destH;

        SDL.SDL_RenderCopy(Renderer, texture.Handle, ref src, ref dest);
    }

    // ======================================================================================
    // Text drawing
    // ======================================================================================

    /// <summary>
    /// Draws a text string. Returns the bounds of the drawn text.
    /// </summary>
    /// <param name="text">The text to draw.</param>
    /// <param name="position">The position where the text will be drawn.</param>
    /// <param name="color">The color of the text.</param>
    /// <param name="font">The font to use to draw the text.</param>
    /// <param name="alignment">The alignment of the text relative to the position. If unspecified the text will be drawn left-aligned.</param>
    /// <param name="measureOnly">If true the text will only be measured and not actually drawn to the screen.</param>
    public static Bounds2 DrawString(string text, Vector2 position, Color color, Font font, TextAlignment alignment = TextAlignment.Left, bool measureOnly = false)
    {
        // Every time we draw a string we have to render it into a texture, which isn't the fastest thing in the world.
        // To make this faster we keep a cache of recently rendered strings and reuse them when possible.

        Tuple<Font, string> textCacheKey = Tuple.Create(font, text);
        TextCacheEntry entry;
        if (TextCache.TryGetValue(textCacheKey, out entry))
        {
            // We found the text in the cache, so use it and reset its age so that it doesn't get freed this frame:
            entry.Age = 0;
        }
        else
        {
            // We were unable to find the text in the cache, so render it to a texture and cache it for later:
            SDL.SDL_Color white = new SDL.SDL_Color() { r = 255, g = 255, b = 255, a = 255 };
            IntPtr surface = SDL_ttf.TTF_RenderText_Blended(font.Handle, text, white);
            IntPtr handle = SDL.SDL_CreateTextureFromSurface(Renderer, surface);
            SDL.SDL_FreeSurface(surface);
            entry = new TextCacheEntry(handle);
            TextCache[textCacheKey] = entry;
        }

        // Query the texture dimensions:
        uint format;
        int access, width, height;
        SDL.SDL_QueryTexture(entry.Handle, out format, out access, out width, out height);

        // Apply text alignment relative to the draw position:
        if (alignment == TextAlignment.Center)
        {
            position.X -= width / 2;
        }
        else if (alignment == TextAlignment.Right)
        {
            position.X -= width;
        }

        // If we're not only measuring the text, draw it:
        if (!measureOnly)
        {
            Texture texture = new Texture(entry.Handle, width, height);
            DrawTexture(texture, position, color);
        }

        // Return the bounds of the text:
        return new Bounds2(position, new Vector2(width, height));
    }

    private static void FreeUnusedTextCacheEntries()
    {
        // Cache entries are aggressively purged, and will essentially only stick around when the same text is drawn every frame.

        List<Tuple<Font, string>> expiredEntryKeys = new List<Tuple<Font, string>>();
        foreach (var keyAndEntry in TextCache)
        {
            keyAndEntry.Value.Age += 1;
            if (keyAndEntry.Value.Age > 2)
            {
                SDL.SDL_DestroyTexture(keyAndEntry.Value.Handle);
                expiredEntryKeys.Add(keyAndEntry.Key);
            }
        }

        foreach (Tuple<Font, string> key in expiredEntryKeys)
        {
            TextCache.Remove(key);
        }
    }

    private class TextCacheEntry
    {
        public readonly IntPtr Handle;
        public int Age;

        public TextCacheEntry(IntPtr handle)
        {
            Handle = handle;
            Age = 0;
        }
    }
}

enum TextAlignment
{
    /// <summary>
    /// Left-align the text.
    /// </summary>
    Left,

    /// <summary>
    /// Center the text.
    /// </summary>
    Center,

    /// <summary>
    /// Right-align the text.
    /// </summary>
    Right,
}

[Flags]
enum TextureMirror
{
    /// <summary>
    /// Do not mirror the texture.
    /// </summary>
    None = SDL.SDL_RendererFlip.SDL_FLIP_NONE,

    /// <summary>
    /// Mirror the texture horizontally.
    /// </summary>
    Horizontal = SDL.SDL_RendererFlip.SDL_FLIP_HORIZONTAL,

    /// <summary>
    /// Mirror the texture vertically.
    /// </summary>
    Vertical = SDL.SDL_RendererFlip.SDL_FLIP_VERTICAL,

    /// <summary>
    /// Mirror the texture horizontally and vertically. Equivalent to rotating it 180 degrees.
    /// </summary>
    Both = Horizontal | Vertical,
}

enum TextureBlendMode
{
    /// <summary>
    /// Use a normal blend mode where new colors replace old colors based on the alpha of the new color.
    /// </summary>
    Normal = SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND,

    /// <summary>
    /// Use an additive blend mode where new colors are simply added to old colors and get brighter.
    /// </summary>
    Additive = SDL.SDL_BlendMode.SDL_BLENDMODE_ADD,
}

enum TextureScaleMode
{
    /// <summary>
    /// Use a linear scale mode that interpolates between pixels for a smooth but blurry image.
    /// </summary>
    Nearest = SDL.SDL_ScaleMode.SDL_ScaleModeNearest,

    /// <summary>
    /// Use a nearest neighbor scale mode that interpolates between pixels for a sharp but pixelated image.
    /// </summary>
    Linear = SDL.SDL_ScaleMode.SDL_ScaleModeLinear,
}
