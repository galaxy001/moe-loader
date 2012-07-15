using System;
using System.Collections.Generic;
using System.Configuration;
using System.Windows;

namespace MoeLoader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void PART_VerticalScrollBar_MouseDown_1(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                System.Windows.Controls.Primitives.ScrollBar sc = sender as System.Windows.Controls.Primitives.ScrollBar;
                if (sc != null)
                {
                    System.Windows.Controls.ScrollViewer sv = sc.TemplatedParent as System.Windows.Controls.ScrollViewer;
                    double pos = e.GetPosition(sc).Y;
                    if (pos > sc.Value / sv.ExtentHeight * sc.ActualHeight + sc.Track.Thumb.ActualHeight)
                        sv.PageDown();
                    else if (pos < sc.Value / sv.ExtentHeight * sc.ActualHeight)
                        sv.PageUp();
                    //sv.ScrollToVerticalOffset(e.GetPosition(sc).Y / sc.ActualHeight * sv.ExtentHeight);
                }
            }
        }

        private void PART_HorizontalScrollBar_MouseDown_1(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                System.Windows.Controls.Primitives.ScrollBar sc = sender as System.Windows.Controls.Primitives.ScrollBar;
                if (sc != null)
                {
                    System.Windows.Controls.ScrollViewer sv = sc.TemplatedParent as System.Windows.Controls.ScrollViewer;
                    //sv.ScrollToHorizontalOffset(e.GetPosition(sc).X / sc.ActualWidth * sv.ExtentWidth);
                    double pos = e.GetPosition(sc).X;
                    if (pos > sc.Value / sv.ExtentWidth * sc.ActualWidth + sc.Track.Thumb.ActualWidth)
                        sv.PageRight();
                    else if (pos < sc.Value / sv.ExtentWidth * sc.ActualWidth)
                        sv.PageLeft();
                }
            }
        }
    }
}
