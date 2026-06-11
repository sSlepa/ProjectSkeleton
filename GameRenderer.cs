using Silk.NET.Maths;
using Silk.NET.SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TheAdventure.Models;

namespace TheAdventure;

public unsafe class GameRenderer : IDisposable
{
    private readonly Sdl _sdl;
    private Renderer* _renderer;
    private readonly GameWindow _window;
    private readonly Camera _camera;

    private readonly Dictionary<int, IntPtr> _texturePointers = new();
    private readonly Dictionary<int, TextureData> _textureData = new();
    private readonly Dictionary<string, int> _textureCacheByPath = new();
    private int _textureId;
    private bool _disposed;

    public (int Width, int Height) WindowSize => _window.Size;

    public GameRenderer(Sdl sdl, GameWindow window)
    {
        _sdl = sdl;

        _renderer = (Renderer*)window.CreateRenderer();
        _sdl.SetRenderDrawBlendMode(_renderer, BlendMode.Blend);

        _window = window;
        var windowSize = window.Size;
        _camera = new Camera(windowSize.Width, windowSize.Height);
    }

    public int LoadTexture(string fileName, out TextureData textureInfo)
    {
        if (_textureCacheByPath.TryGetValue(fileName, out var cachedId))
        {
            textureInfo = _textureData[cachedId];
            return cachedId;
        }

        using (var fStream = new FileStream(fileName, FileMode.Open))
        {
            var image = Image.Load<Rgba32>(fStream);
            textureInfo = new TextureData()
            {
                Width = image.Width,
                Height = image.Height
            };
            var imageRAWData = new byte[textureInfo.Width * textureInfo.Height * 4];
            image.CopyPixelDataTo(imageRAWData.AsSpan());
            fixed (byte* data = imageRAWData)
            {
                var imageSurface = _sdl.CreateRGBSurfaceWithFormatFrom(data, textureInfo.Width,
                    textureInfo.Height, 8, textureInfo.Width * 4, (uint)PixelFormatEnum.Rgba32);
                if (imageSurface == null)
                {
                    throw new Exception("Failed to create surface from image data.");
                }

                var imageTexture = _sdl.CreateTextureFromSurface(_renderer, imageSurface);
                if (imageTexture == null)
                {
                    _sdl.FreeSurface(imageSurface);
                    throw new Exception("Failed to create texture from surface.");
                }

                _sdl.FreeSurface(imageSurface);

                _textureData[_textureId] = textureInfo;
                _texturePointers[_textureId] = (IntPtr)imageTexture;
                _textureCacheByPath[fileName] = _textureId;
            }
        }

        return _textureId++;
    }

    public void RenderTexture(int textureId, Rectangle<int> src, Rectangle<int> dst,
        RendererFlip flip = RendererFlip.None, double angle = 0.0, Silk.NET.SDL.Point center = default)
    {
        if (_texturePointers.TryGetValue(textureId, out var imageTexture))
        {
            var translatedDst = _camera.ToScreenCoordinates(dst);
            _sdl.RenderCopyEx(_renderer, (Texture*)imageTexture, in src,
                in translatedDst,
                angle,
                in center, flip);
        }
    }

    public void RenderTextureRaw(int textureId, Rectangle<int> src, Rectangle<int> dst,
        RendererFlip flip = RendererFlip.None)
    {
        if (_texturePointers.TryGetValue(textureId, out var imageTexture))
        {
            Silk.NET.SDL.Point center = default;
            _sdl.RenderCopyEx(_renderer, (Texture*)imageTexture, in src, in dst, 0.0, in center, flip);
        }
    }

    public void FillRect(byte r, byte g, byte b, byte a, Rectangle<int> rect)
    {
        _sdl.SetRenderDrawColor(_renderer, r, g, b, a);
        _sdl.RenderFillRect(_renderer, in rect);
    }

    public void DrawRect(byte r, byte g, byte b, byte a, Rectangle<int> rect)
    {
        _sdl.SetRenderDrawColor(_renderer, r, g, b, a);
        _sdl.RenderDrawRect(_renderer, in rect);
    }

    public void SetDrawColor(byte r, byte g, byte b, byte a) => _sdl.SetRenderDrawColor(_renderer, r, g, b, a);
    public void ClearScreen() => _sdl.RenderClear(_renderer);
    public void PresentFrame() => _sdl.RenderPresent(_renderer);
    public void SetWorldBounds(Rectangle<int> bounds) => _camera.SetWorldBounds(bounds);
    public void CameraLookAt(int x, int y) => _camera.LookAt(x, y);
    public Vector2D<int> ToWorldCoordinates(int x, int y) => _camera.ToWorldCoordinates(new Vector2D<int>(x, y));

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var ptr in _texturePointers.Values)
        {
            if (ptr != IntPtr.Zero)
            {
                _sdl.DestroyTexture((Texture*)ptr);
            }
        }
        _texturePointers.Clear();
        _textureData.Clear();

        if (_renderer != null)
        {
            _sdl.DestroyRenderer(_renderer);
            _renderer = null;
        }

        GC.SuppressFinalize(this);
    }
}
