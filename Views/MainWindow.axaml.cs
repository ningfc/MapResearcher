using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Input;
using System.IO;
using GetStartedApp;
using Avalonia;
using System.IO.Pipes;
using System.Collections.Generic;
// using Avalonia.Remote.Protocol.Input;
namespace GetStartedApp.Views;

using TileCache = System.Collections.Generic.Dictionary<string, Tile>;

public partial class MainWindow : Window
{
    private Canvas _canvas;
    private HttpClient _httpClient;
    private TileCache _tilesCache;
    public MainWindow()
    {
        InitializeComponent();
        _canvas = this.FindControl<Canvas>("canvas");
        _httpClient = new HttpClient();
        _tilesCache = new TileCache();
        this.KeyDown += MapViewerWindow_KeyDown;
        this.SizeChanged += MapViewerWindow_SizeChanged;
        this.PointerPressed += MapViewerWindow_PointerPressed;
        this.PointerMoved += MapViewerWindow_PointerMoved;
        this.PointerReleased += MapViewerWindow_PointerReleased;
        this.Width = 800;
        this.Height = 450;
        if (_canvas != null)
        {
            _canvas.Width = 800;
            _canvas.Height = 450;
        }
    }

    // 陕西省的经纬度范围（左下角和右上角的经纬度坐标）
    double minLng = 108; // 最小经度
    double minLat = 31; // 最小纬度
    double maxLng = 109; // 最大经度
    double maxLat = 32; // 最大纬度
    int zoomLevel = 9; // 缩放级别

    Rect _lastExtent;
    private Point? lastMousePosition;
    private void MapViewerWindow_PointerPressed(object sender, PointerEventArgs e)
    {
        if (e.GetCurrentPoint(_canvas).Properties.IsLeftButtonPressed)
        {
            lastMousePosition = e.GetPosition(_canvas);
            _lastExtent = new Rect(minLng, maxLat, maxLng - minLng,  minLat - maxLat);
            isCanceled = true;
        }
    }

    private bool isCanceled = false;

    private void MoveExtent(Point currentMousePosition)
    {
        // 计算鼠标移动的距离
        double deltaX = currentMousePosition.X - lastMousePosition.Value.X;
        double deltaY = currentMousePosition.Y - lastMousePosition.Value.Y;
        // deltaX /= 2f;
        // deltaY /= 2f;

        // 更新范围
        // 这里可以根据鼠标移动的距离更新范围，例如根据地图的比例进行缩放或平移
        var left = _lastExtent.Left - deltaX * (_lastExtent.Width) / _canvas.Width;
        var right = _lastExtent.Right - deltaX * (_lastExtent.Width) / _canvas.Width;
        var bottom = _lastExtent.Bottom - deltaY * (_lastExtent.Height) / _canvas.Height;
        var top = _lastExtent.Top - deltaY * (_lastExtent.Height) / _canvas.Height;

        minLng = left;
        maxLng = right;
        minLat = bottom;
        maxLat = top;
    }
    private async void MapViewerWindow_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // if(e.GetCurrentPoint(_canvas).Properties.IsLeftButtonPressed)
        {
            // 获取当前鼠标位置
            Point currentMousePosition = e.GetPosition(_canvas);
            if (lastMousePosition == null) return;

            MoveExtent(currentMousePosition);
            // 重新绘制地图
            isCanceled = false;
            await LoadMapAsync();
        }
    }
    private void MapViewerWindow_PointerMoved(object sender, PointerEventArgs e)
    {
        if (e.GetCurrentPoint(_canvas).Properties.IsLeftButtonPressed)
        {
            // 获取当前鼠标位置
            Point currentMousePosition = e.GetPosition(_canvas);
            if (lastMousePosition == null) return;
            
            MoveExtent(currentMousePosition);

        }
        _canvas.Children.Clear();
        foreach (var (_, tile) in _tilesCache)
        {
            DrawTile(tile.bitmap, tile.x, tile.y, zoomLevel);
        }
    }
    private void MapViewerWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _canvas.Width = this.Width;
        _canvas.Height = this.Height;
    }
    private async void MapViewerWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            Console.WriteLine("loadTiles");
            await LoadMapAsync();
        }
    }
    private async Task LoadMapAsync()
    {
        if (loading) return;
        int left, top, right, bottom;
        (zoomLevel, left, top, right, bottom) =
        TileCalculator.CalculateTiles(minLng, minLat, maxLng, maxLat, _canvas.Width, _canvas.Height);

        _canvas.Children.Clear();
        // 加载地图瓦片
        await LoadTiles(left, top, right, bottom, zoomLevel);
    }

    private (int, int) CalculateTileRowAndColumn(double lng, double lat, int zoomLevel)
    {
        double x = (lng + 180) / 360 * (1 << zoomLevel);
        double y = (1 - Math.Log(Math.Tan(lat * Math.PI / 180) + 1 / Math.Cos(lat * Math.PI / 180)) / Math.PI) / 2 * (1 << zoomLevel);

        // Ensure the values are within the valid range for tile coordinates
        x = Math.Max(0, Math.Min((1 << zoomLevel) - 1, x));
        y = Math.Max(0, Math.Min((1 << zoomLevel) - 1, y));

        return ((int)x, (int)y);
    }

    private bool loading = false;
    
    private async Task LoadTiles(int left, int top, double right, int bottom, int zoomLevel)
    {
        loading = true;
        List<Task> drawTasks = new List<Task>();
        for (int y = top; y <= bottom && !isCanceled; y++) // Adjust the step size according to your needs
        {
            for (int x = left; x <= right && !isCanceled; x++) // Adjust the step size according to your needs
            {
                // zoomLevel = 6;
                // int x, y;
                // (x, y) = CalculateTileRowAndColumn(lat, lat, zoomLevel);
                Console.WriteLine($"{x},{y}");
                string tileKey = $"{zoomLevel}_{x}_{y}";

                if (!_tilesCache.ContainsKey(tileKey))
                {
                    // Download tile asynchronously
                    Bitmap bmp = await DownloadTileAsync(x, y, zoomLevel);
                    _tilesCache.Add(tileKey, new Tile{x = x, y = y, bitmap=bmp});
                    // SaveTileToLocalAsync(bmp, x, y, zoomLevel);
                }
                // Draw tile on canvas
//                DrawTile(_tilesCache[tileKey].bitmap, x, y, zoomLevel);
                drawTasks.Add(Task.Run(() => DrawTile(_tilesCache[tileKey].bitmap, x, y, zoomLevel)));
            }
        }
        loading = false;
    }

    private async Task<Bitmap> DownloadTileAsync(int x, int y, int zoom)
    {
        string url = GetTileUrl(x, y, zoom);
        // Console.WriteLine(url);
        byte[] imageData = await _httpClient.GetByteArrayAsync(url);
        return new Bitmap(new MemoryStream(imageData));
    }

    private async Task SaveTileToLocalAsync(Bitmap tile, int x, int y, int zoom)
    {
        // 确保目录存在，如果不存在则创建
        string directoryPath = $"Tiles/{zoom}/{x}";
        Directory.CreateDirectory(directoryPath);

        // 构造文件路径
        string filePath = Path.Combine(directoryPath, $"{y}.png");

        // 保存瓦片到文件
        using (FileStream stream = File.Create(filePath))
        {
            // 将图像编码为 PNG 并保存到文件流中
            tile.Save(stream);
        }
    }
    
    private async Task DrawTile(Bitmap tile, int x, int y, int zoom)
    {
        // 计算瓦片在Canvas上的位置
        // double canvasX = x * Tile.tileSize;
        // double canvasY = y * Tile.tileSize;
        var r = TileCalculator.CalculateTilePosition(y, x, zoom, minLng, maxLat);

        // 创建Image控件并设置图像源
        Image image = new Image();
        image.Source = tile;

        // 设置Image在Canvas上的位置
        Canvas.SetLeft(image, r.left);
        Canvas.SetTop(image, r.top);

        // 将Image添加到Canvas中
        if (_canvas != null)
            _canvas.Children.Add(image);
    }

    private string GetTileUrl(int x, int y, int zoom)
    {
        Random random = new Random();
        int domainIndex = random.Next(1, 5); // 随机生成 1 到 4 之间的一个数
        // 使用字符串插值构建瓦片URL
        return $"https://webrd0{domainIndex}.is.autonavi.com/appmaptile?x={x}&y={y}&z={zoom}&lang=zh_cn&size=1&scale=1&style=8";
    }

}