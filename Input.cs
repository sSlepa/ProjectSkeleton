using Silk.NET.SDL;

namespace TheAdventure;

public unsafe class Input
{
    private readonly Sdl _sdl;
    private readonly byte[] _previousKeyState = new byte[(int)KeyCode.Count];

    public EventHandler<(int x, int y)>? OnMouseClick;

    public Input(Sdl sdl)
    {
        _sdl = sdl;
    }

    public bool IsKeyPressed(KeyCode key)
    {
        ReadOnlySpan<byte> keyboardState = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);
        return keyboardState[(int)key] == 1;
    }

    public bool WasKeyJustPressed(KeyCode key)
    {
        ReadOnlySpan<byte> keyboardState = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);
        return keyboardState[(int)key] == 1 && _previousKeyState[(int)key] == 0;
    }

    public bool IsLeftPressed() => IsKeyPressed(KeyCode.Left) || IsKeyPressed(KeyCode.A);
    public bool IsRightPressed() => IsKeyPressed(KeyCode.Right) || IsKeyPressed(KeyCode.D);
    public bool IsUpPressed() => IsKeyPressed(KeyCode.Up) || IsKeyPressed(KeyCode.W);
    public bool IsDownPressed() => IsKeyPressed(KeyCode.Down) || IsKeyPressed(KeyCode.S);

    public bool ProcessInput()
    {
        // Snapshot keyboard state before pumping new events so WasKeyJustPressed
        // can compare end-of-previous-frame state to current state.
        var rawState = _sdl.GetKeyboardState(null);
        new ReadOnlySpan<byte>(rawState, (int)KeyCode.Count).CopyTo(_previousKeyState);

        Event ev = new Event();
        while (_sdl.PollEvent(ref ev) != 0)
        {
            if (ev.Type == (uint)EventType.Quit) return true;

            switch (ev.Type)
            {
                case (uint)EventType.Mousebuttondown:
                {
                    if (ev.Button.Button == (byte)MouseButton.Primary)
                    {
                        OnMouseClick?.Invoke(this, (ev.Button.X, ev.Button.Y));
                    }

                    break;
                }
                case (uint)EventType.Windowevent:
                    switch (ev.Window.Event)
                    {
                        case (byte)WindowEventID.TakeFocus:
                            _sdl.SetWindowInputFocus(_sdl.GetWindowFromID(ev.Window.WindowID));
                            break;
                    }
                    break;
            }
        }

        return false;
    }
}
