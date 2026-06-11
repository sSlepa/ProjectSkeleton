using Silk.NET.SDL;

namespace TheAdventure;

public static class Program
{
    public static void Main()
    {
        var sdl = new Sdl(new SdlContext());

        var sdlInitResult = sdl.Init(Sdl.InitVideo | Sdl.InitAudio | Sdl.InitEvents | Sdl.InitTimer |
                                     Sdl.InitGamecontroller | Sdl.InitJoystick);
        if (sdlInitResult < 0)
        {
            throw new InvalidOperationException("Failed to initialize SDL.");
        }

        using (var gameWindow = new GameWindow(sdl))
        using (var gameRenderer = new GameRenderer(sdl, gameWindow))
        {
            var input = new Input(sdl);
            var engine = new Engine(gameRenderer, input);

            // The SDL window must be created and pumped on the same (main) thread, so the
            // game loop stays synchronous. The engine's async setup/shutdown do a one-off
            // blocking wait here rather than turning Main into an async method that would
            // resume the loop on a thread-pool thread and leave the window unresponsive.
            engine.SetupWorldAsync().GetAwaiter().GetResult();

            bool quit = false;
            while (!quit)
            {
                quit = input.ProcessInput();
                if (quit) break;

                if (input.IsKeyPressed(KeyCode.Escape))
                {
                    quit = true;
                    break;
                }

                engine.ProcessFrame();
                engine.RenderFrame();

                System.Threading.Thread.Sleep(13);
            }

            engine.ShutdownAsync().GetAwaiter().GetResult();
        }

        sdl.Quit();
    }
}
