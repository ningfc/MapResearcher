using System;
using Avalonia.Media.Imaging;

namespace GetStartedApp;

public class Tile
{
    public const int tileSize = 256;//tile.PixelWidth;
    public int x { get; set; }
    public int y { get; set; }
    public Bitmap bitmap;
    public bool ok { get; set; }
}
