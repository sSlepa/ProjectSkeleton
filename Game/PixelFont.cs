using Silk.NET.Maths;

namespace TheAdventure.Game;

// Tiny 3x5 bitmap font built on top of FillRect. Avoids pulling in SDL_ttf for a few status labels.
public static class PixelFont
{
    private const int GlyphWidth = 3;
    private const int GlyphHeight = 5;

    private static readonly Dictionary<char, string[]> Glyphs = new()
    {
        ['0'] = new[] { "###", "# #", "# #", "# #", "###" },
        ['1'] = new[] { " # ", "## ", " # ", " # ", "###" },
        ['2'] = new[] { "###", "  #", "###", "#  ", "###" },
        ['3'] = new[] { "###", "  #", "###", "  #", "###" },
        ['4'] = new[] { "# #", "# #", "###", "  #", "  #" },
        ['5'] = new[] { "###", "#  ", "###", "  #", "###" },
        ['6'] = new[] { "###", "#  ", "###", "# #", "###" },
        ['7'] = new[] { "###", "  #", " # ", " # ", " # " },
        ['8'] = new[] { "###", "# #", "###", "# #", "###" },
        ['9'] = new[] { "###", "# #", "###", "  #", "###" },
        ['A'] = new[] { "###", "# #", "###", "# #", "# #" },
        ['B'] = new[] { "## ", "# #", "## ", "# #", "## " },
        ['C'] = new[] { "###", "#  ", "#  ", "#  ", "###" },
        ['D'] = new[] { "## ", "# #", "# #", "# #", "## " },
        ['E'] = new[] { "###", "#  ", "###", "#  ", "###" },
        ['F'] = new[] { "###", "#  ", "###", "#  ", "#  " },
        ['G'] = new[] { "###", "#  ", "# #", "# #", "###" },
        ['H'] = new[] { "# #", "# #", "###", "# #", "# #" },
        ['I'] = new[] { "###", " # ", " # ", " # ", "###" },
        ['J'] = new[] { "###", "  #", "  #", "# #", "###" },
        ['K'] = new[] { "# #", "## ", "#  ", "## ", "# #" },
        ['L'] = new[] { "#  ", "#  ", "#  ", "#  ", "###" },
        ['M'] = new[] { "# #", "###", "###", "# #", "# #" },
        ['N'] = new[] { "# #", "###", "###", "###", "# #" },
        ['O'] = new[] { "###", "# #", "# #", "# #", "###" },
        ['P'] = new[] { "###", "# #", "###", "#  ", "#  " },
        ['Q'] = new[] { "###", "# #", "# #", "###", "  #" },
        ['R'] = new[] { "###", "# #", "## ", "# #", "# #" },
        ['S'] = new[] { "###", "#  ", "###", "  #", "###" },
        ['T'] = new[] { "###", " # ", " # ", " # ", " # " },
        ['U'] = new[] { "# #", "# #", "# #", "# #", "###" },
        ['V'] = new[] { "# #", "# #", "# #", "# #", " # " },
        ['W'] = new[] { "# #", "# #", "###", "###", "# #" },
        ['X'] = new[] { "# #", "# #", " # ", "# #", "# #" },
        ['Y'] = new[] { "# #", "# #", " # ", " # ", " # " },
        ['Z'] = new[] { "###", "  #", " # ", "#  ", "###" },
        [':'] = new[] { "   ", " # ", "   ", " # ", "   " },
        ['-'] = new[] { "   ", "   ", "###", "   ", "   " },
        ['!'] = new[] { " # ", " # ", " # ", "   ", " # " },
        [' '] = new[] { "   ", "   ", "   ", "   ", "   " },
    };

    public static int Measure(string text, int scale, int spacing = 1)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        return text.Length * GlyphWidth * scale + (text.Length - 1) * spacing * scale;
    }

    public static int Height(int scale) => GlyphHeight * scale;

    public static void Draw(GameRenderer renderer, string text, int x, int y, int scale,
        byte r, byte g, byte b, byte a = 255, int spacing = 1)
    {
        if (string.IsNullOrEmpty(text)) return;
        int cursor = x;
        foreach (var ch in text.ToUpperInvariant())
        {
            if (Glyphs.TryGetValue(ch, out var rows))
            {
                for (int row = 0; row < GlyphHeight; row++)
                {
                    var line = rows[row];
                    for (int col = 0; col < GlyphWidth; col++)
                    {
                        if (line[col] == '#')
                        {
                            renderer.FillRect(r, g, b, a,
                                new Rectangle<int>(cursor + col * scale, y + row * scale, scale, scale));
                        }
                    }
                }
            }
            cursor += GlyphWidth * scale + spacing * scale;
        }
    }
}
