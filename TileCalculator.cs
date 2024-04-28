using System;
using Avalonia;
using Avalonia.Controls;

namespace GetStartedApp;

public class TileCalculator
{
    private const double EARTH_RAD = 6378137;
    private const double EARTH_PERIMETER = 2 * Math.PI * EARTH_RAD;
    private const int TILE_SIZE = 256;

    public static (int, int, int, int, int) CalculateTiles(double minLng, double minLat, double maxLng, double maxLat, double windowWidth, double windowHeight)
    {
        // 计算给定范围的经度和纬度跨度

        double[] leftTop = LngLatToMercator(minLng, maxLat);
        double[] rightBottom = LngLatToMercator(maxLng, minLat);
        double xSpan = rightBottom[0] - leftTop[0];
        double ySpan = leftTop[1] - rightBottom[1];

        // 根据经度和纬度跨度以及窗口大小，确定最佳的缩放级别
        int zoomLevel = CalculateZoomLevel(xSpan, ySpan, windowWidth, windowHeight);

        // 根据缩放级别计算瓦片范围
        int[] topLeft = GetTileRowAndCol(minLng, maxLat, zoomLevel);
        int[] bottomRight = GetTileRowAndCol(maxLng, minLat, zoomLevel);

        return (zoomLevel, topLeft[0], topLeft[1], bottomRight[0], bottomRight[1]);
    }

    public static (double left, double top) CalculateTilePosition(int row, int col, int zoom, double minLng, double maxLat)
    {
        var resolution = GetResolution(zoom);

        // 计算瓦片的像素坐标
        double tilePixelX = col * TILE_SIZE;
        double tilePixelY = row * TILE_SIZE;

        var WindowLeftTopPixel = getPxFromLngLat(minLng, maxLat, zoom);

        var left = tilePixelX - WindowLeftTopPixel.PxX; 
        var top = tilePixelY - WindowLeftTopPixel.PxY; 

        // 创建瓦片在窗口中的显示位置矩形
        return (left, top);
    }

    private static int CalculateZoomLevel(double lngSpan, double latSpan, double windowWidth, double windowHeight)
    {
        double resolutionHorizontal = lngSpan / windowWidth;
        double resolutionVertical = latSpan / windowHeight;

        double resolution = Math.Min(resolutionHorizontal, resolutionVertical);

        int zoomLevel = 0;
        double tileSize = TILE_SIZE;
        while ((EARTH_PERIMETER / tileSize / resolution) > 1)
        {
            tileSize *= 2;
            zoomLevel++;
        }

        return zoomLevel;
    }

    private static int[] GetTileRowAndCol(double lng, double lat, int z)
    {
        double[] xy = LngLatToMercator(lng, lat);
        double x = xy[0];
        double y = xy[1];
        // 调整原点为左上角
        x += EARTH_PERIMETER / 2;
        y = EARTH_PERIMETER / 2 - y;

        double resolution = GetResolution(z);

        int row = (int)Math.Floor(x / resolution / TILE_SIZE);
        int col = (int)Math.Floor(y / resolution / TILE_SIZE);

        return new int[] { row, col };
    }

    private static double[] LngLatToMercator(double lng, double lat)
    {
        double x = lng * (Math.PI / 180) * EARTH_RAD;
        double rad = lat * (Math.PI / 180);
        double sin = Math.Sin(rad);
        double y = (EARTH_RAD / 2) * Math.Log((1 + sin) / (1 - sin));
        return new double[] { x, y };
    }

    private static double GetResolution(int n)
    {
        double tileNums = Math.Pow(2, n);
        double tileTotalPx = tileNums * TILE_SIZE;
        return EARTH_PERIMETER / tileTotalPx;
    }

    private static double[] transformXY(double x, double y, string origin = "topLeft")
    {
        if (origin == "topLeft")
        {
            x += EARTH_PERIMETER / 2;
            y = EARTH_PERIMETER / 2 - y;
        }
        return new double[] { x, y };
    }

    // 计算4326经纬度对应的像素坐标
    public static (double PxX, double PxY) getPxFromLngLat(double lng, double lat, int z, string opt = "")
    {
        double[] xy = LngLatToMercator(lng, lat);
        double x = xy[0];
        double y = xy[1];
        // 调整原点为左上角
        x += EARTH_PERIMETER / 2;
        y = EARTH_PERIMETER / 2 - y;

        double resolution = GetResolution(z);

        x = Math.Floor(x / resolution);
        y = Math.Floor(y / resolution);
        return (x, y);
        // return new double[] {x, y};
    }
}
