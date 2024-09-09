using Microsoft.UI.Input;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ResizeableRectangle;

using XRect = Windows.Foundation.Rect;
using XPoint = Windows.Foundation.Point;
using XSize = Windows.Foundation.Size;

// Sample xaml
// <Canvas>
//     <local:ResizeableRectangle Left = "50" Top="50" Right="350" Bottom="350"/>
// </Canvas>

public sealed partial class ResizeableRectangle : UserControl
{
    public ResizeableRectangle()
    {
        this.InitializeComponent();
    }

    private void MainBorderLoaded(object sender, RoutedEventArgs e)
    {
        XRect RectInParent = new XRect(0, 0, Width, Height);
        UpdateGripperPosition(RectInParent);
        BeforeRect = XRect.Empty;

        string coodinate = string.Format(
                $"Rect3[{Left}, {Top}, {Right}, {Bottom}] [{Width}, {Height}] [{ParentActualWidth}, {ParentActualHeight}]\n" +
                $"LT[{Canvas.GetLeft(LTGripper)}, {Canvas.GetTop(LTGripper)}] RT[{Canvas.GetLeft(RTGripper)}, {Canvas.GetTop(RTGripper)}]\n" +
                $"LB[{Canvas.GetLeft(LBGripper)}, {Canvas.GetTop(LBGripper)}] RB[{Canvas.GetLeft(RBGripper)}, {Canvas.GetTop(RBGripper)}]\n");
        CoodinateText.Text = coodinate;
    }

    public double GripperSize { get; set; } = 13;

    private XRect BeforeRect { get; set; } = new XRect();

    private double ParentActualWidth => (double)Parent.GetValue(ActualWidthProperty);
    private double ParentActualHeight => (double)Parent.GetValue(ActualHeightProperty);

    public static readonly DependencyProperty LeftProperty
        = DependencyProperty.Register(nameof(Left), typeof(double), typeof(ResizeableRectangle), new PropertyMetadata(double.NaN));
    public static readonly DependencyProperty TopProperty
        = DependencyProperty.Register(nameof(Top), typeof(double), typeof(ResizeableRectangle), new PropertyMetadata(double.NaN));
    public static readonly DependencyProperty RightProperty
        = DependencyProperty.Register(nameof(Right), typeof(double), typeof(ResizeableRectangle), new PropertyMetadata(double.NaN));
    public static readonly DependencyProperty BottomProperty
        = DependencyProperty.Register(nameof(Bottom), typeof(double), typeof(ResizeableRectangle), new PropertyMetadata(double.NaN));

    public double Left
    {
        set
        {
            SetValue(LeftProperty, value);
            Canvas.SetLeft(this, value);
        }
        get
        {
            double v = (double)GetValue(LeftProperty);
            if (double.IsNaN(v))
                SetValue(LeftProperty, Canvas.GetLeft(this));

            return Canvas.GetLeft(this);
        }
    }
    public double Top
    {
        set
        {
            SetValue(TopProperty, value);
            Canvas.SetTop(this, value);
        }
        get
        {
            double v = (double)GetValue(TopProperty);
            if (double.IsNaN(v))
                SetValue(TopProperty, Canvas.GetTop(this));

            return Canvas.GetTop(this);
        }
    }
    public double Right
    {
        set
        {
            SetValue(RightProperty, value);
            Width = value - Left;
        }
        get
        {
            double v = (double)GetValue(RightProperty);
            if (double.IsNaN(v))
                SetValue(RightProperty, Left + Width);

            return Left + Width;
        }
    }
    public double Bottom
    {
        set
        {
            SetValue(BottomProperty, value);
            Height = value - Top;
        }
        get
        {
            double v = (double)GetValue(BottomProperty);
            if (double.IsNaN(v))
                SetValue(BottomProperty, Top + Height);

            return Top + Height;
        }
    }

    private static InputCursor CursorSizeAll { get; } = InputSystemCursor.Create(InputSystemCursorShape.SizeAll);
    private static InputCursor CursorSizeNorthwestSoutheast { get; } = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthwestSoutheast);
    private static InputCursor CursorSizeNortheastSouthwest { get; } = InputSystemCursor.Create(InputSystemCursorShape.SizeNortheastSouthwest);

    private bool IsMainBorderPressed { get; set; } = false;
    private PointerPoint MBLastPoint { get; set; }

    private bool IsHeaderPressed { get; set; } = false;
    private PointerPoint HDLastPoint { get; set; }

    private bool IsLTGripperPressed { get; set; } = false;
    private PointerPoint LTLastPoint { get; set; }

    private bool IsRTGripperPressed { get; set; } = false;
    private PointerPoint RTLastPoint { get; set; }

    private bool IsLBGripperPressed { get; set; } = false;
    private PointerPoint LBLastPoint { get; set; }

    private bool IsRBGripperPressed { get; set; } = false;
    private PointerPoint RBLastPoint { get; set; }

    private bool CheckMinMaxWidth(XRect rect) => MinWidth <= rect.Width && rect.Width <= MaxWidth;
    private bool CheckMinMaxHeight(XRect rect) => MinHeight <= rect.Height && rect.Height <= MaxHeight;
    private bool CheckParentWidth(XRect rect) => rect.Right <= ParentActualWidth;
    private bool CheckParentHeight(XRect rect) => rect.Bottom <= ParentActualHeight;

    private void SetPoint(FrameworkElement gripper, double x, double y)
    {
        Canvas.SetLeft(gripper, x);
        Canvas.SetTop(gripper, y);
    }

    private bool UpdatePosition(XRect rect)
    {
        bool ret = false;

        if (0 <= rect.Left && CheckMinMaxWidth(rect) && CheckParentWidth(rect))
        {
            if (Left != rect.Left)
            {
                Left = rect.Left;
                Right = rect.Right;
                ret = true;
            }
            if (Right != rect.Right)
            {
                Right = rect.Right;
                ret = true;
            }
        }
        if (0 <= rect.Top && CheckMinMaxHeight(rect) && CheckParentHeight(rect))
        {
            if (Top != rect.Top)
            {
                Top = rect.Top;
                Bottom = rect.Bottom;
                ret = true;
            }
            if (Bottom != rect.Bottom)
            {
                Bottom = rect.Bottom;
                ret = true;
            }
        }

        return ret;
    }

    private void UpdateGripperPosition(XRect rect)
    {
        if (rect.Width < MinWidth || rect.Height < MinHeight)
            return;

        double gabLeft = GripperSize / 2.0 + MainBorder.BorderThickness.Left;
        double gabTop = GripperSize / 2.0 + MainBorder.BorderThickness.Top;
        double gabRight = GripperSize / 2.0 + MainBorder.BorderThickness.Right;
        double gabBottom = GripperSize / 2.0 + MainBorder.BorderThickness.Bottom;

        Header.Width = rect.Width - (MainBorder.BorderThickness.Left + MainBorder.BorderThickness.Right);

        SetPoint(LTGripper, rect.Left - gabLeft, rect.Top - gabTop);
        SetPoint(RTGripper, rect.Right - gabRight, rect.Top - gabTop);
        SetPoint(LBGripper, rect.Left - gabLeft, rect.Bottom - gabBottom);
        SetPoint(RBGripper, rect.Right - gabRight, rect.Bottom - gabBottom);
    }

    #region MainBorderPointer    
    public void MainBorderPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        MainBorder.BorderBrush = new SolidColorBrush(Colors.Black);
        ProtectedCursor = CursorSizeAll;
    }

    public void MainBorderPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (IsMainBorderPressed)
            return;

        MainBorder.BorderBrush = new SolidColorBrush(Colors.DarkGray);
        ProtectedCursor = null;
    }

    public void MainBorderPointerPressed(object sender, PointerRoutedEventArgs e)
    {

        MBLastPoint = e.GetCurrentPoint(null);
        IsMainBorderPressed = true;
        MainBorder.BorderBrush = new SolidColorBrush(Colors.Black);
        MainBorder.CapturePointer(e.Pointer);
    }

    public void MainBorderPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        IsMainBorderPressed = false;
        MainBorder.BorderBrush = new SolidColorBrush(Colors.DarkGray);
        MainBorder.ReleasePointerCapture(e.Pointer);
    }

    public void MainBorderPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var curPt = e.GetCurrentPoint(null);
        if (IsMainBorderPressed && curPt.FrameId != MBLastPoint.FrameId)
        {
            double diffX = curPt.Position.X - MBLastPoint.Position.X;
            double diffY = curPt.Position.Y - MBLastPoint.Position.Y;

            XRect beforeRectInParent = new XRect(Left, Top, Width, Height);
            XRect expectRectInParent = new XRect(Left + diffX, Top + diffY, Width, Height);

            if (UpdatePosition(expectRectInParent))
                UpdateGripperPosition(new XRect(0, 0, Width, Height));

            string coodinate = string.Format(
                $"Cur[{curPt.Position.X}, {curPt.Position.Y}] Last[{MBLastPoint.Position.X}, {MBLastPoint.Position.Y}]\n" +
                $"Rect[{beforeRectInParent.Left}, {beforeRectInParent.Top}, {beforeRectInParent.Right}, {beforeRectInParent.Bottom}] [{beforeRectInParent.Width}, {beforeRectInParent.Height}]\n" +
                $"Rect2[{expectRectInParent.Left}, {expectRectInParent.Top}, {expectRectInParent.Right}, {expectRectInParent.Bottom}] [{expectRectInParent.Width}, {expectRectInParent.Height}]\n" +
                $"Rect3[{Left}, {Top}, {Right}, {Bottom}] [{Width}, {Height}] [{ParentActualWidth}, {ParentActualHeight}]\n" +
                $"LT[{Canvas.GetLeft(LTGripper)}, {Canvas.GetTop(LTGripper)}] RT[{Canvas.GetLeft(RTGripper)}, {Canvas.GetTop(RTGripper)}]\n" +
                $"LB[{Canvas.GetLeft(LBGripper)}, {Canvas.GetTop(LBGripper)}] RB[{Canvas.GetLeft(RBGripper)}, {Canvas.GetTop(RBGripper)}]\n");
            CoodinateText.Text = coodinate;

            MBLastPoint = curPt;
        }
    }
    #endregion

    #region HeaderPointer    
    public void HeaderPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        Header.Fill = new SolidColorBrush(Colors.Black);
        ProtectedCursor = CursorSizeAll;
    }

    public void HeaderPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (IsHeaderPressed)
            return;

        Header.Fill = new SolidColorBrush(Colors.DarkGray);
        ProtectedCursor = null;
    }

    public void HeaderPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        HDLastPoint = e.GetCurrentPoint(null);
        IsHeaderPressed = true;
        Header.Fill = new SolidColorBrush(Colors.Black);
        Header.CapturePointer(e.Pointer);
    }

    public void HeaderPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        IsHeaderPressed = false;
        Header.Fill = new SolidColorBrush(Colors.DarkGray);
        Header.ReleasePointerCapture(e.Pointer);
    }

    public void HeaderPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var curPt = e.GetCurrentPoint(null);
        if (IsHeaderPressed && curPt.FrameId != HDLastPoint.FrameId)
        {
            double diffX = curPt.Position.X - HDLastPoint.Position.X;
            double diffY = curPt.Position.Y - HDLastPoint.Position.Y;

            XRect beforeRectInParent = new XRect(Left, Top, Width, Height);
            XRect expectRectInParent = new XRect(Left + diffX, Top + diffY, Width, Height);

            if (UpdatePosition(expectRectInParent))
                UpdateGripperPosition(new XRect(0, 0, Width, Height));

            string coodinate = string.Format(
                $"Cur[{curPt.Position.X}, {curPt.Position.Y}] Last[{HDLastPoint.Position.X}, {HDLastPoint.Position.Y}]\n" +
                $"Rect[{beforeRectInParent.Left}, {beforeRectInParent.Top}, {beforeRectInParent.Right}, {beforeRectInParent.Bottom}] [{beforeRectInParent.Width}, {beforeRectInParent.Height}]\n" +
                $"Rect2[{expectRectInParent.Left}, {expectRectInParent.Top}, {expectRectInParent.Right}, {expectRectInParent.Bottom}] [{expectRectInParent.Width}, {expectRectInParent.Height}]\n" +
                $"Rect3[{Left}, {Top}, {Right}, {Bottom}] [{Width}, {Height}] [{ParentActualWidth}, {ParentActualHeight}]\n" +
                $"LT[{Canvas.GetLeft(LTGripper)}, {Canvas.GetTop(LTGripper)}] RT[{Canvas.GetLeft(RTGripper)}, {Canvas.GetTop(RTGripper)}]\n" +
                $"LB[{Canvas.GetLeft(LBGripper)}, {Canvas.GetTop(LBGripper)}] RB[{Canvas.GetLeft(RBGripper)}, {Canvas.GetTop(RBGripper)}]\n");
            CoodinateText.Text = coodinate;

            HDLastPoint = curPt;
        }
    }
    #endregion
    #region LeftTopPointer
    public void LeftTopPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        LTGripper.Fill = new SolidColorBrush(Colors.Black);
        ProtectedCursor = CursorSizeNorthwestSoutheast;
    }

    public void LeftTopPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (IsLTGripperPressed)
            return;

        LTGripper.Fill = new SolidColorBrush(Colors.DarkGray);
        ProtectedCursor = null;
    }

    public void LeftTopPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        LTLastPoint = e.GetCurrentPoint(RBGripper);
        LTGripper.Fill = new SolidColorBrush(Colors.Black);
        IsLTGripperPressed = LTGripper.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    public void LeftTopPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        IsLTGripperPressed = false;
        LTGripper.Fill = new SolidColorBrush(Colors.DarkGray);
        LTGripper.ReleasePointerCapture(e.Pointer);
        e.Handled = true;
    }

    public void LeftTopPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var curPt = e.GetCurrentPoint(RBGripper);
        if (IsLTGripperPressed && curPt.FrameId != LTLastPoint.FrameId)
        {
            double diffX = curPt.Position.X - LTLastPoint.Position.X;
            double diffY = curPt.Position.Y - LTLastPoint.Position.Y;

            XRect beforeRectInParent = new XRect(Left, Top, Width, Height);
            XRect expectRectInParent = new XRect(Left + diffX, Top + diffY, Width - diffX, Height - diffY);

            if (UpdatePosition(expectRectInParent))
                UpdateGripperPosition(new XRect(0, 0, Width, Height));

            string coodinate = string.Format(
                $"Cur[{curPt.Position.X}, {curPt.Position.Y}] Last[{LTLastPoint.Position.X}, {LTLastPoint.Position.Y}]\n" +
                $"Rect[{beforeRectInParent.Left}, {beforeRectInParent.Top}, {beforeRectInParent.Right}, {beforeRectInParent.Bottom}] [{beforeRectInParent.Width}, {beforeRectInParent.Height}]\n" +
                $"Rect2[{expectRectInParent.Left}, {expectRectInParent.Top}, {expectRectInParent.Right}, {expectRectInParent.Bottom}] [{expectRectInParent.Width}, {expectRectInParent.Height}]\n" +
                $"Rect3[{Left}, {Top}, {Right}, {Bottom}] [{Width}, {Height}] [{ParentActualWidth}, {ParentActualHeight}]\n" +
                $"LT[{Canvas.GetLeft(LTGripper)}, {Canvas.GetTop(LTGripper)}] RT[{Canvas.GetLeft(RTGripper)}, {Canvas.GetTop(RTGripper)}]\n" +
                $"LB[{Canvas.GetLeft(LBGripper)}, {Canvas.GetTop(LBGripper)}] RB[{Canvas.GetLeft(RBGripper)}, {Canvas.GetTop(RBGripper)}]\n");
            CoodinateText.Text = coodinate;

            LTLastPoint = curPt;
        }
    }
    #endregion

    #region RightTopPointer
    public void RightTopPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        RTGripper.Fill = new SolidColorBrush(Colors.Black);
        ProtectedCursor = CursorSizeNortheastSouthwest;
    }

    public void RightTopPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (IsRTGripperPressed)
            return;

        RTGripper.Fill = new SolidColorBrush(Colors.DarkGray);
        ProtectedCursor = null;
    }

    public void RightTopPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        RTLastPoint = e.GetCurrentPoint(LBGripper);
        RTGripper.Fill = new SolidColorBrush(Colors.Black);
        IsRTGripperPressed = RTGripper.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    public void RightTopPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        IsRTGripperPressed = false;
        RTGripper.Fill = new SolidColorBrush(Colors.DarkGray);
        RTGripper.ReleasePointerCapture(e.Pointer);
        e.Handled = true;
    }

    public void RightTopPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var curPt = e.GetCurrentPoint(LBGripper);
        if (IsRTGripperPressed && curPt.FrameId != RTLastPoint.FrameId)
        {
            double diffX = curPt.Position.X - RTLastPoint.Position.X;
            double diffY = curPt.Position.Y - RTLastPoint.Position.Y;

            XRect beforeRectInParent = new XRect(Left, Top, Width, Height);
            XRect expectRectInParent = new XRect(Left, Top + diffY, Width + diffX, Height - diffY);

            if (UpdatePosition(expectRectInParent))
                UpdateGripperPosition(new XRect(0, 0, Width, Height));

            string coodinate = string.Format(
                $"Cur[{curPt.Position.X}, {curPt.Position.Y}] Last[{RTLastPoint.Position.X}, {RTLastPoint.Position.Y}]\n" +
                $"Rect[{beforeRectInParent.Left}, {beforeRectInParent.Top}, {beforeRectInParent.Right}, {beforeRectInParent.Bottom}] [{beforeRectInParent.Width}, {beforeRectInParent.Height}]\n" +
                $"Rect2[{expectRectInParent.Left}, {expectRectInParent.Top}, {expectRectInParent.Right}, {expectRectInParent.Bottom}] [{expectRectInParent.Width}, {expectRectInParent.Height}]\n" +
                $"Rect3[{Left}, {Top}, {Right}, {Bottom}] [{Width}, {Height}] [{ParentActualWidth}, {ParentActualHeight}]\n" +
                $"LT[{Canvas.GetLeft(LTGripper)}, {Canvas.GetTop(LTGripper)}] RT[{Canvas.GetLeft(RTGripper)}, {Canvas.GetTop(RTGripper)}]\n" +
                $"LB[{Canvas.GetLeft(LBGripper)}, {Canvas.GetTop(LBGripper)}] RB[{Canvas.GetLeft(RBGripper)}, {Canvas.GetTop(RBGripper)}]\n");
            CoodinateText.Text = coodinate;

            RTLastPoint = curPt;
        }
    }
    #endregion

    #region LeftBottomPointer
    public void LeftBottomPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        LBGripper.Fill = new SolidColorBrush(Colors.Black);
        ProtectedCursor = CursorSizeNortheastSouthwest;
    }

    public void LeftBottomPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (IsLBGripperPressed)
            return;

        LBGripper.Fill = new SolidColorBrush(Colors.DarkGray);
        ProtectedCursor = null;
    }

    public void LeftBottomPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        LBLastPoint = e.GetCurrentPoint(RTGripper);
        LBGripper.Fill = new SolidColorBrush(Colors.Black);
        IsLBGripperPressed = LBGripper.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    public void LeftBottomPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        IsLBGripperPressed = false;
        LBGripper.Fill = new SolidColorBrush(Colors.DarkGray);
        LBGripper.ReleasePointerCapture(e.Pointer);
        e.Handled = true;
    }

    public void LeftBottomPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var curPt = e.GetCurrentPoint(RTGripper);
        if (IsLBGripperPressed && curPt.FrameId != LBLastPoint.FrameId)
        {
            double diffX = curPt.Position.X - LBLastPoint.Position.X;
            double diffY = curPt.Position.Y - LBLastPoint.Position.Y;

            XRect beforeRectInParent = new XRect(Left, Top, Width, Height);
            XRect expectRectInParent = new XRect(Left + diffX, Top, Width - diffX, Height + diffY);

            if (UpdatePosition(expectRectInParent))
                UpdateGripperPosition(new XRect(0, 0, Width, Height));

            string coodinate = string.Format(
                $"Cur[{curPt.Position.X}, {curPt.Position.Y}] Last[{LBLastPoint.Position.X}, {LBLastPoint.Position.Y}]\n" +
                $"Rect[{beforeRectInParent.Left}, {beforeRectInParent.Top}, {beforeRectInParent.Right}, {beforeRectInParent.Bottom}] [{beforeRectInParent.Width}, {beforeRectInParent.Height}]\n" +
                $"Rect2[{expectRectInParent.Left}, {expectRectInParent.Top}, {expectRectInParent.Right}, {expectRectInParent.Bottom}] [{expectRectInParent.Width}, {expectRectInParent.Height}]\n" +
                $"Rect3[{Left}, {Top}, {Right}, {Bottom}] [{Width}, {Height}] [{ParentActualWidth}, {ParentActualHeight}]\n" +
                $"LT[{Canvas.GetLeft(LTGripper)}, {Canvas.GetTop(LTGripper)}] RT[{Canvas.GetLeft(RTGripper)}, {Canvas.GetTop(RTGripper)}]\n" +
                $"LB[{Canvas.GetLeft(LBGripper)}, {Canvas.GetTop(LBGripper)}] RB[{Canvas.GetLeft(RBGripper)}, {Canvas.GetTop(RBGripper)}]\n");
            CoodinateText.Text = coodinate;

            LBLastPoint = curPt;
        }
    }
    #endregion

    #region RightBottomPointer
    public void RightBottomPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        RBGripper.Fill = new SolidColorBrush(Colors.Black);
        ProtectedCursor = CursorSizeNorthwestSoutheast;
    }

    public void RightBottomPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (IsRBGripperPressed)
            return;

        RBGripper.Fill = new SolidColorBrush(Colors.DarkGray);
        ProtectedCursor = null;
    }

    public void RightBottomPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        RBLastPoint = e.GetCurrentPoint(LTGripper);
        RBGripper.Fill = new SolidColorBrush(Colors.Black);
        IsRBGripperPressed = RBGripper.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    public void RightBottomPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        IsRBGripperPressed = false;
        RBGripper.Fill = new SolidColorBrush(Colors.DarkGray);
        RBGripper.ReleasePointerCapture(e.Pointer);
        e.Handled = true;
    }

    public void RightBottomPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var curPt = e.GetCurrentPoint(LTGripper);
        if (IsRBGripperPressed && curPt.FrameId != RBLastPoint.FrameId)
        {
            double diffX = curPt.Position.X - RBLastPoint.Position.X;
            double diffY = curPt.Position.Y - RBLastPoint.Position.Y;

            XRect beforeRectInParent = new XRect(Left, Top, Width, Height);
            XRect expectRectInParent = new XRect(Left, Top, Width + diffX, Height + diffY);

            if (beforeRectInParent.Width < expectRectInParent.Width && curPt.Position.X <= beforeRectInParent.Width)
                expectRectInParent.Width = beforeRectInParent.Width;
            else if (beforeRectInParent.Width > expectRectInParent.Width && curPt.Position.X >= beforeRectInParent.Width)
                expectRectInParent.Width = beforeRectInParent.Width;

            if (beforeRectInParent.Height < expectRectInParent.Height && curPt.Position.Y <= beforeRectInParent.Height)
                expectRectInParent.Height = beforeRectInParent.Height;
            else if (beforeRectInParent.Height > expectRectInParent.Height && curPt.Position.Y >= beforeRectInParent.Height)
                expectRectInParent.Height = beforeRectInParent.Height;

            if (UpdatePosition(expectRectInParent))
                UpdateGripperPosition(new XRect(0, 0, Width, Height));

            string coodinate = string.Format(
                $"Cur[{curPt.Position.X}, {curPt.Position.Y}] Last[{RBLastPoint.Position.X}, {RBLastPoint.Position.Y}]\n" +
                $"Rect[{beforeRectInParent.Left}, {beforeRectInParent.Top}, {beforeRectInParent.Right}, {beforeRectInParent.Bottom}] [{beforeRectInParent.Width}, {beforeRectInParent.Height}]\n" +
                $"Rect2[{expectRectInParent.Left}, {expectRectInParent.Top}, {expectRectInParent.Right}, {expectRectInParent.Bottom}] [{expectRectInParent.Width}, {expectRectInParent.Height}]\n" +
                $"Rect3[{Left}, {Top}, {Right}, {Bottom}] [{Width}, {Height}] [{ParentActualWidth}, {ParentActualHeight}]\n" +
                $"LT[{Canvas.GetLeft(LTGripper)}, {Canvas.GetTop(LTGripper)}] RT[{Canvas.GetLeft(RTGripper)}, {Canvas.GetTop(RTGripper)}]\n" +
                $"LB[{Canvas.GetLeft(LBGripper)}, {Canvas.GetTop(LBGripper)}] RB[{Canvas.GetLeft(RBGripper)}, {Canvas.GetTop(RBGripper)}]\n");
            CoodinateText.Text = coodinate;

            RBLastPoint = curPt;
        }
    }

    private void Header_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (BeforeRect.IsEmpty)
        {
            BeforeRect = new XRect(Left, Top, Width, Height);

            Left = 0;
            Top = 0;
            Right = ParentActualWidth;
            Bottom = ParentActualHeight;
        }
        else
        {
            Left = BeforeRect.X;
            Top = BeforeRect.Top;
            Right = BeforeRect.Right;
            Bottom = BeforeRect.Bottom;

            BeforeRect = XRect.Empty;
        }

        UpdateGripperPosition(new XRect(0, 0, Width, Height));
    }
    #endregion
}
