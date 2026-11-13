using System;

namespace DTMAPI.DebugConsole
{
    internal enum DebugConsoleBreakpoint
    {
        Wide,
        Tabs,
        Compact
    }

    internal enum DebugConsoleTab
    {
        Catalog,
        World,
        Advanced
    }

    internal sealed class DebugConsoleLayout
    {
        internal const double ReferenceWidth = 1920d;
        internal const double ReferenceHeight = 1080d;
        internal const double ReferenceMatch = 0.5d;
        internal const double LogicalMargin = 24d;
        internal const double MinimumPhysicalTarget = 44d;
        internal const double MaximumWideLogicalWidth = 1500d;
        internal const double MaximumWideLogicalHeight = 820d;
        internal const double CatalogCellSize = 84d;
        internal const double CatalogCellGap = 10d;
        internal const double CatalogCellPitch =
            CatalogCellSize + CatalogCellGap;
        internal const double CatalogPagerBottomInset = 110d;

        internal double ScreenWidth { get; private set; }
        internal double ScreenHeight { get; private set; }
        internal double SafeX { get; private set; }
        internal double SafeY { get; private set; }
        internal double SafeWidth { get; private set; }
        internal double SafeHeight { get; private set; }
        internal double ScaleFactor { get; private set; }
        internal double LogicalWidth { get; private set; }
        internal double LogicalHeight { get; private set; }
        internal double MinimumClickSize { get; private set; }
        internal int CatalogColumns { get; private set; }
        internal int CatalogRows { get; private set; }
        internal int CatalogPageSize => CatalogColumns * CatalogRows;
        internal DebugConsoleBreakpoint Breakpoint { get; private set; }
        internal float AnchorMinX { get; private set; }
        internal float AnchorMinY { get; private set; }
        internal float AnchorMaxX { get; private set; }
        internal float AnchorMaxY { get; private set; }

        internal static DebugConsoleLayout Create(
            double screenWidth,
            double screenHeight,
            double safeX,
            double safeY,
            double safeWidth,
            double safeHeight)
        {
            screenWidth = Math.Max(1d, screenWidth);
            screenHeight = Math.Max(1d, screenHeight);
            safeWidth = safeWidth > 0d ? safeWidth : screenWidth;
            safeHeight = safeHeight > 0d ? safeHeight : screenHeight;
            safeX = Math.Max(0d, Math.Min(safeX, screenWidth - 1d));
            safeY = Math.Max(0d, Math.Min(safeY, screenHeight - 1d));
            safeWidth = Math.Min(safeWidth, screenWidth - safeX);
            safeHeight = Math.Min(safeHeight, screenHeight - safeY);

            double widthScale = screenWidth / ReferenceWidth;
            double heightScale = screenHeight / ReferenceHeight;
            double scale = Math.Sqrt(Math.Max(0.0001d, widthScale * heightScale));
            double physicalInset = LogicalMargin * scale;
            double innerSafeWidth = Math.Max(1d, safeWidth - physicalInset * 2d);
            double innerSafeHeight = Math.Max(1d, safeHeight - physicalInset * 2d);
            double availableLogicalWidth = innerSafeWidth / scale;
            double availableLogicalHeight = innerSafeHeight / scale;

            // A low-resolution display must not be promoted back into the
            // three-column layout merely because CanvasScaler expands its
            // coordinate space.  The usable breakpoint width is therefore
            // bounded by both safe physical pixels and scaled UI units.
            double breakpointWidth = Math.Min(
                availableLogicalWidth,
                innerSafeWidth);
            DebugConsoleBreakpoint breakpoint = breakpointWidth >= 1500d
                ? DebugConsoleBreakpoint.Wide
                : breakpointWidth >= 1050d
                    ? DebugConsoleBreakpoint.Tabs
                    : DebugConsoleBreakpoint.Compact;
            double logicalWidth = breakpoint == DebugConsoleBreakpoint.Wide
                ? Math.Min(availableLogicalWidth, MaximumWideLogicalWidth)
                : availableLogicalWidth;
            double logicalHeight = breakpoint == DebugConsoleBreakpoint.Wide
                ? Math.Min(availableLogicalHeight, MaximumWideLogicalHeight)
                : availableLogicalHeight;
            double panelPhysicalWidth = logicalWidth * scale;
            double panelPhysicalHeight = logicalHeight * scale;
            double panelPhysicalX = safeX + physicalInset +
                (innerSafeWidth - panelPhysicalWidth) * 0.5d;
            double panelPhysicalY = safeY + physicalInset +
                (innerSafeHeight - panelPhysicalHeight) * 0.5d;
            double catalogWidth = breakpoint == DebugConsoleBreakpoint.Wide
                ? Math.Max(560d, logicalWidth - 540d)
                : logicalWidth;
            const double gridLeft = 388d;
            int columns = Clamp(
                (int)Math.Floor(
                    Math.Max(
                        CatalogCellPitch * 3d,
                        catalogWidth - gridLeft) /
                    CatalogCellPitch),
                3,
                8);
            double verticalReserve = breakpoint == DebugConsoleBreakpoint.Wide
                ? 278d
                : 324d;
            int rows = Clamp(
                (int)Math.Floor(
                    Math.Max(
                        CatalogCellPitch * 3d,
                        logicalHeight - verticalReserve) /
                    CatalogCellPitch),
                3,
                6);

            return new DebugConsoleLayout
            {
                ScreenWidth = screenWidth,
                ScreenHeight = screenHeight,
                SafeX = safeX,
                SafeY = safeY,
                SafeWidth = safeWidth,
                SafeHeight = safeHeight,
                ScaleFactor = scale,
                LogicalWidth = logicalWidth,
                LogicalHeight = logicalHeight,
                MinimumClickSize = MinimumPhysicalTarget / scale,
                CatalogColumns = columns,
                CatalogRows = rows,
                Breakpoint = breakpoint,
                AnchorMinX = (float)(panelPhysicalX / screenWidth),
                AnchorMinY = (float)(panelPhysicalY / screenHeight),
                AnchorMaxX = (float)((panelPhysicalX + panelPhysicalWidth) / screenWidth),
                AnchorMaxY = (float)((panelPhysicalY + panelPhysicalHeight) / screenHeight)
            };
        }

        internal bool SameGeometry(DebugConsoleLayout? other) =>
            other != null &&
            Math.Abs(ScreenWidth - other.ScreenWidth) < 0.5d &&
            Math.Abs(ScreenHeight - other.ScreenHeight) < 0.5d &&
            Math.Abs(SafeX - other.SafeX) < 0.5d &&
            Math.Abs(SafeY - other.SafeY) < 0.5d &&
            Math.Abs(SafeWidth - other.SafeWidth) < 0.5d &&
            Math.Abs(SafeHeight - other.SafeHeight) < 0.5d &&
            Breakpoint == other.Breakpoint;

        private static int Clamp(int value, int minimum, int maximum) =>
            Math.Max(minimum, Math.Min(maximum, value));
    }
}
