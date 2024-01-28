﻿using FoxTunes.Interfaces;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace FoxTunes
{
    public class SnappingAdapter : BaseComponent
    {
        public SnappingAdapter(IntPtr handle)
        {
            this.Handle = handle;
            this.Window = GetWindow(handle);
        }

        public IntPtr Handle { get; private set; }

        public global::System.Windows.Window Window { get; private set; }

        public string Id
        {
            get
            {
                if (!(this.Window is IUserInterfaceWindow window))
                {
                    return string.Empty;
                }
                return window.Id;
            }
        }

        public Rectangle Bounds
        {
            get
            {
                var rect = default(RECT);
                if (!GetWindowRect(this.Handle, out rect))
                {
                    return Rectangle.Empty;
                }
                return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            }
            set
            {
                SetWindowPos(this.Handle, IntPtr.Zero, value.X, value.Y, value.Width, value.Height, SWP_SHOWWINDOW);
            }
        }

        public bool IsVisible
        {
            get
            {
                var bounds = this.Bounds;
                var point = new POINT()
                {
                    X = bounds.X + (bounds.Width / 2),
                    Y = bounds.Y + (bounds.Height / 2)
                };
                var handle = WindowFromPoint(point);
                return this.Handle == handle;
            }
        }

        public bool Capture
        {
            get
            {
                return this.Window.IsMouseCaptured;
            }
            set
            {
                if (value)
                {
                    this.Window.CaptureMouse();
                }
                else
                {
                    this.Window.ReleaseMouseCapture();
                }
            }
        }

        public void AddHook(HwndSourceHook hook)
        {
            var source = HwndSource.FromHwnd(this.Handle);
            if (source == null)
            {
                return;
            }
            source.AddHook(hook);
        }

        public void RemoveHook(HwndSourceHook hook)
        {
            var source = HwndSource.FromHwnd(this.Handle);
            if (source == null)
            {
                return;
            }
            source.RemoveHook(hook);
        }

        public Point PointToScreen(Point point)
        {
            return PointConverter.ToDrawingPoint(
                PointConverter.PointToScreen(
                    this.Window,
                    PointConverter.ToWindowsPoint(
                        point
                    )
                )
            );
        }

        public void Activate()
        {
            this.Window.Activate();
        }

        public static global::System.Windows.Window GetWindow(IntPtr handle)
        {
            var windows = System.Windows.Application.Current.Windows;
            for (var a = 0; a < windows.Count; a++)
            {
                var window = windows[a];
                if (window.GetHandle() == handle)
                {
                    return window;
                }
            }
            return null;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT lpPoint);

        const uint SWP_SHOWWINDOW = 0x0040;
    }
}
