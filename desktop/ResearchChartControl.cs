using System.Windows;
using System.Windows.Media;
using System.Globalization;
using BacktestEngine.Research.Contracts;
namespace BacktestEngine.Research.Desktop;
public static class ResearchChartPalette
{
    public static readonly Brush TaiwanRise = FrozenBrush(Color.FromRgb(239, 83, 80));
    public static readonly Brush TaiwanFall = FrozenBrush(Color.FromRgb(38, 166, 154));
    public static readonly Brush RawVolumeBrush = FrozenBrush(Color.FromRgb(70, 130, 180));
    public static readonly Brush Background = FrozenBrush(Color.FromRgb(13, 18, 24));
    public static readonly Pen RisePen = FrozenPen(TaiwanRise, 1);
    public static readonly Pen FallPen = FrozenPen(TaiwanFall, 1);
    public static readonly Pen CrosshairPen = FrozenPen(Brushes.LightGray, .7);
    public static readonly Brush Sma5Brush = FrozenBrush(Color.FromRgb(255, 214, 0));
    public static readonly Brush Sma10Brush = FrozenBrush(Color.FromRgb(33, 150, 243));
    public static readonly Brush Sma20Brush = FrozenBrush(Color.FromRgb(156, 106, 222));
    public static readonly Brush Sma60Brush = FrozenBrush(Color.FromRgb(235, 151, 78));
    public static readonly Brush DevelopingBrush = FrozenBrush(Color.FromRgb(137, 168, 184));
    public static readonly Pen DevelopingPen = FrozenPen(DevelopingBrush, 1.5);
    public static readonly Brush DevelopingEpisodeBrush = FrozenBrush(Color.FromRgb(255, 209, 102));
    public static readonly Brush SessionSeparatorBrush = FrozenBrush(Color.FromRgb(41, 69, 86));
    public static readonly Pen SessionSeparatorPen = FrozenPen(SessionSeparatorBrush, .8);
    public static readonly Brush VwapBrush = FrozenBrush(Colors.White);
    public static readonly Brush MeasurementReferenceRegionBrush = FrozenBrush(Color.FromRgb(47, 128, 237));
    public static readonly Brush MeasurementPriceRegionBrush = FrozenBrush(Color.FromRgb(242, 201, 76));
    // These presentation-only colors intentionally remain separate from the
    // indicator palette. The channel is a measurement region, not another MA
    // line, so a reviewer can identify it without consulting Inspector.
    public static readonly Brush MeasurementReferenceRegionStrokeBrush = FrozenBrush(Color.FromRgb(56, 189, 248));
    public static readonly Brush MeasurementPriceRegionStrokeBrush = FrozenBrush(Color.FromRgb(255, 183, 77));
    public static readonly Brush MeasurementReferenceRegionFillBrush = FrozenBrush(Color.FromArgb(112, 47, 128, 237));
    public static readonly Brush MeasurementPriceRegionFillBrush = FrozenBrush(Color.FromArgb(96, 242, 201, 76));
    public static readonly Brush MeasurementHeldBrush = FrozenBrush(Color.FromRgb(86, 204, 242));
    public static readonly Brush MeasurementBounceBrush = FrozenBrush(Color.FromRgb(39, 174, 96));
    public static readonly Brush MeasurementBreakBrush = FrozenBrush(Color.FromRgb(235, 87, 87));
    public static readonly Brush MeasurementReclaimBrush = FrozenBrush(Color.FromRgb(187, 107, 217));
    public static readonly Brush MeasurementDepartureBrush = FrozenBrush(Color.FromRgb(0, 191, 166));
    public static readonly Brush MeasurementAmbiguousBrush = FrozenBrush(Color.FromRgb(158, 158, 158));
    public static readonly Typeface Typeface = new("Microsoft JhengHei UI");

    public static Brush CandleBrushFor(ResearchBarRecord bar) =>
        bar.ClosePriceE4 >= bar.OpenPriceE4 ? TaiwanRise : TaiwanFall;

    public static Brush RawVolumeBrushFor(bool isDeveloping) =>
        isDeveloping ? WithOpacity(RawVolumeBrush, .48) : RawVolumeBrush;

    public static Brush FrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    public static Pen FrozenPen(Brush brush, double thickness)
    {
        var pen = new Pen(brush, thickness);
        pen.Freeze();
        return pen;
    }

    public static Brush WithOpacity(Brush brush, double opacity)
    {
        if (brush is not SolidColorBrush solid) return brush;
        var color = solid.Color;
        return FrozenBrush(Color.FromArgb(
            (byte)Math.Clamp(Math.Round(color.A * opacity), 0, 255),
            color.R, color.G, color.B));
    }

    public static Pen DevelopingPenFor(Brush seriesBrush) =>
        DashedPen(WithOpacity(seriesBrush, .72), 1.15);

    public static Pen DashedPen(Brush brush, double thickness)
    {
        var pen = new Pen(brush, thickness)
        {
            DashStyle = DashStyles.Dash
        };
        pen.Freeze();
        return pen;
    }
}
// Adapted original DrawMain candle/volume renderer; full overlays/qualification controls excluded.
public sealed class ResearchChartControl : FrameworkElement {
 ResearchBarRecord[] _bars=[];
 int _visibleStart=0;
 int _visibleCount=>_bars.Length;
 public void SetData(IReadOnlyList<ResearchBarRecord> bars){_bars=bars.ToArray();InvalidateVisual();}
 protected override void OnRender(DrawingContext dc){base.OnRender(dc);dc.DrawRectangle(ResearchChartPalette.Background,null,new Rect(RenderSize));DrawMain(dc);}
private void DrawMain(DrawingContext dc)
    {
        var count = _visibleCount;
        if (count <= 0) return;
        var pricePane = new Rect(50, 12, Math.Max(1, ActualWidth - 62), Math.Max(1, ActualHeight * .72));
        var volumePane = new Rect(50, pricePane.Bottom + 6, pricePane.Width, Math.Max(1, ActualHeight - pricePane.Bottom - 30));
        var low = int.MaxValue;
        var high = int.MinValue;
        long maxVolume = 1;
        for (var i = _visibleStart; i < _visibleStart + count; i++)
        {
            var bar = _bars[i];
            low = Math.Min(low, bar.LowPriceE4);
            high = Math.Max(high, bar.HighPriceE4);
            maxVolume = Math.Max(maxVolume, bar.Volume);
        }
        var range = Math.Max(1, high - low);
        var slot = pricePane.Width / count;
        var first = _bars[_visibleStart].OpenTimestampUs;
        var last = _bars[_visibleStart + count - 1].CloseTimestampUs;
        DrawGridAndAxes(dc, pricePane, volumePane, low, range, first, last);

        for (var offset = 0; offset < count; offset++)
        {
            var bar = _bars[_visibleStart + offset];
            var x = pricePane.Left + slot * (offset + .5);
            double Y(int price) => pricePane.Bottom - (price - low) * pricePane.Height / range;
            var rise = bar.ClosePriceE4 >= bar.OpenPriceE4;
            var brush = rise ? ResearchChartPalette.TaiwanRise : ResearchChartPalette.TaiwanFall;
            var pen = rise ? ResearchChartPalette.RisePen : ResearchChartPalette.FallPen;
            var developingPen = bar.IsDeveloping ? ResearchChartPalette.DevelopingPen : null;
            if (true)
            {
                dc.DrawLine(developingPen ?? pen, new(x, Y(bar.HighPriceE4)), new(x, Y(bar.LowPriceE4)));
                dc.DrawRectangle(bar.IsDeveloping ? Brushes.Transparent : brush, developingPen,
                    new(x - Math.Max(1, slot * .28), Math.Min(Y(bar.OpenPriceE4), Y(bar.ClosePriceE4)), Math.Max(2, slot * .56), Math.Max(1, Math.Abs(Y(bar.OpenPriceE4) - Y(bar.ClosePriceE4)))));
            }
            if (true)
            {
                var height = bar.Volume * volumePane.Height / maxVolume;
                dc.DrawRectangle(Brushes.SteelBlue, null, new(x - Math.Max(1, slot * .28), volumePane.Bottom - height, Math.Max(2, slot * .56), height));
            }
        }

    }
 void DrawGridAndAxes(DrawingContext dc,Rect price,Rect volume,int low,int range,long first,long last){
  for(var i=0;i<=5;i++){var y=price.Top+price.Height*i/5;dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(35,49,63)),1),new Point(price.Left,y),new Point(price.Right,y));
   var value=(low+range*(5-i)/5)/10000.0;var text=new FormattedText(value.ToString("0.00",CultureInfo.InvariantCulture),CultureInfo.InvariantCulture,FlowDirection.LeftToRight,ResearchChartPalette.Typeface,12,Brushes.LightSteelBlue,1);dc.DrawText(text,new Point(0,y-6));}
  var label=new FormattedText("價格（Price） / 成交量（Volume） · 1m · Synthetic",CultureInfo.InvariantCulture,FlowDirection.LeftToRight,ResearchChartPalette.Typeface,14,Brushes.LightSteelBlue,1);dc.DrawText(label,new Point(52,ActualHeight-20));
 }
}
