﻿using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using static ModernFlyouts.Core.Interop.NativeMethods;

namespace ModernFlyouts.Core.Interop
{
    public enum ZBandID : int
    {
        Default = 0x0,
        Desktop = 0x1,
        UIAccess = 0x2,
        ImmersiveIHM = 0x3,
        ImmersiveNotification = 0x4,
        ImmersiveAppChrome = 0x5,
        ImmersiveMogo = 0x6,
        ImmersiveEDGY = 0x7,
        ImmersiveInActiveMOBODY = 0x8,
        ImmersiveInActiveDock = 0x9,
        ImmersiveActiveMOBODY = 0xA,
        ImmersiveActiveDock = 0xB,
        ImmersiveBackground = 0xC,
        ImmersiveSEARCH = 0xD,
        GenuineWindows = 0xE,
        ImmersiveRestricted = 0xF,
        SystemTools = 0x10,
        Lock = 0x11,
        AboveLockUX = 0x12,
    };

    public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    public class BandWindow : ContentControl
    {
        private readonly WndProc delegWndProc;

        private HwndSource hwndSource;
        private double dpiScale = 1.0;
        private WndProcHookManager hookManager;
        private bool _isSizeChanging;
        private bool _isVisibilityChanging;

        #region Properties

        protected double DpiScale => dpiScale;

        protected HwndSource HwndSource => hwndSource;

        #region Activatable

        public static readonly DependencyProperty ActivatableProperty =
            DependencyProperty.Register(
                nameof(Activatable),
                typeof(bool),
                typeof(BandWindow),
                new PropertyMetadata(false, OnActivatablePropertyChanged));

        public bool Activatable
        {
            get => (bool)GetValue(ActivatableProperty);
            set => SetValue(ActivatableProperty, value);
        }

        private static void OnActivatablePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BandWindow bandWindow)
            {
                if (bandWindow.IsSourceCreated)
                {
                    if ((bool)e.NewValue)
                    {
                        ApplyWindowStyles(bandWindow.Handle, wsEXToRemove: ExtendedWindowStyles.WS_EX_NOACTIVATE);
                    }
                    else
                    {
                        ApplyWindowStyles(bandWindow.Handle, wsEXToAdd: ExtendedWindowStyles.WS_EX_NOACTIVATE);
                    }
                }
            }
        }

        #endregion

        #region ActualLeft

        private static readonly DependencyPropertyKey ActualLeftPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualLeft),
                typeof(double),
                typeof(BandWindow),
                new PropertyMetadata(0.0));

        public static readonly DependencyProperty ActualLeftProperty = ActualLeftPropertyKey.DependencyProperty;

        public double ActualLeft
        {
            get => (double)GetValue(ActualLeftProperty);
            private set => SetValue(ActualLeftPropertyKey, value);
        }

        #endregion

        #region ActualTop

        private static readonly DependencyPropertyKey ActualTopPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualTop),
                typeof(double),
                typeof(BandWindow),
                new PropertyMetadata(0.0));

        public static readonly DependencyProperty ActualTopProperty = ActualTopPropertyKey.DependencyProperty;

        public double ActualTop
        {
            get => (double)GetValue(ActualTopProperty);
            private set => SetValue(ActualTopPropertyKey, value);
        }

        #endregion

        #region Handle

        private static readonly DependencyPropertyKey HandlePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(Handle),
                typeof(IntPtr),
                typeof(BandWindow),
                new PropertyMetadata(IntPtr.Zero));

        public static readonly DependencyProperty HandleProperty = HandlePropertyKey.DependencyProperty;

        public IntPtr Handle
        {
            get => (IntPtr)GetValue(HandleProperty);
            private set => SetValue(HandlePropertyKey, value);
        }

        #endregion

        #region IsActive

        private static readonly DependencyPropertyKey IsActivePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsActive),
                typeof(bool),
                typeof(BandWindow),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsActiveProperty = IsActivePropertyKey.DependencyProperty;

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            private set => SetValue(IsActivePropertyKey, value);
        }

        #endregion

        #region IsDragMoving

        private static readonly DependencyPropertyKey IsDragMovingPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsDragMoving),
                typeof(bool),
                typeof(BandWindow),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsDragMovingProperty = IsDragMovingPropertyKey.DependencyProperty;

        public bool IsDragMoving
        {
            get => (bool)GetValue(IsDragMovingProperty);
            private set => SetValue(IsDragMovingPropertyKey, value);
        }

        #endregion

        #region IsSourceCreated

        private static readonly DependencyPropertyKey IsSourceCreatedPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsSourceCreated),
                typeof(bool),
                typeof(BandWindow),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsSourceCreatedProperty = IsSourceCreatedPropertyKey.DependencyProperty;

        public bool IsSourceCreated
        {
            get => (bool)GetValue(IsSourceCreatedProperty);
            private set => SetValue(IsSourceCreatedPropertyKey, value);
        }

        #endregion

        #region TopMost

        public static readonly DependencyProperty TopMostProperty =
            DependencyProperty.Register(
                nameof(TopMost),
                typeof(bool),
                typeof(BandWindow),
                new PropertyMetadata(true, OnTopMostPropertyChanged));

        public bool TopMost
        {
            get => (bool)GetValue(TopMostProperty);
            set => SetValue(TopMostProperty, value);
        }

        private static void OnTopMostPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BandWindow bandWindow)
            {
                if (bandWindow.IsSourceCreated)
                {
                    if ((bool)e.NewValue)
                    {
                        ApplyWindowStyles(bandWindow.Handle, wsEXToAdd: ExtendedWindowStyles.WS_EX_TOPMOST);
                        ShowWindow(bandWindow.Handle, (int)ShowWindowCommands.ShowNoActivate);
                    }
                    else
                    {
                        ApplyWindowStyles(bandWindow.Handle, wsEXToRemove: ExtendedWindowStyles.WS_EX_TOPMOST);
                    }
                }
            }
        }

        #endregion

        #region ZBandID

        public static readonly DependencyProperty ZBandIDProperty =
            DependencyProperty.Register(
                nameof(ZBandID),
                typeof(ZBandID),
                typeof(BandWindow),
                new PropertyMetadata(ZBandID.Default, OnZBandIDPropertyChanged));

        public ZBandID ZBandID
        {
            get => (ZBandID)GetValue(ZBandIDProperty);
            set => SetValue(ZBandIDProperty, value);
        }

        private static void OnZBandIDPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BandWindow bandWindow && bandWindow.IsSourceCreated)
                throw new AccessViolationException("ZBandID should not be changed after the window creation");
        }

        #endregion

        #endregion

        static BandWindow()
        {
            VisibilityProperty.OverrideMetadata(typeof(BandWindow), new FrameworkPropertyMetadata(Visibility.Hidden, OnVisibilityPropertyChanged));
        }

        private static void OnVisibilityPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BandWindow bandWindow)
            {
                if (e.NewValue is Visibility visibility)
                {
                    if (visibility == Visibility.Visible)
                    {
                        bandWindow.Show();
                    }
                    else
                    {
                        bandWindow.Hide();
                    }
                }
            }
        }

        public BandWindow()
        {
            delegWndProc = myWndProc;
            SizeChanged += BandWindow_SizeChanged;

            hookManager = WndProcHookManager.RegisterForBandWindow(this);
        }

        private void BandWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateSize();
        }

        #region Actual window handling

        public void CreateWindow()
        {
            if (IsSourceCreated)
                return;

            WNDCLASSEX wind_class = new()
            {
                cbSize = Marshal.SizeOf(typeof(WNDCLASSEX)),
                hbrBackground = (IntPtr)1 + 1,
                cbClsExtra = 0,
                cbWndExtra = 0,
                hInstance = Marshal.GetHINSTANCE(typeof(BandWindow).Module),
                hIcon = IntPtr.Zero,
                lpszMenuName = string.Empty,
                lpszClassName = "WIB_" + Guid.NewGuid(),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(delegWndProc),
                hIconSm = IntPtr.Zero
            };
            ushort regResult = RegisterClassEx(ref wind_class);

            if (regResult == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            IntPtr hWnd = CreateWindowInBand(
                    (int)(ExtendedWindowStyles.WS_EX_TOOLWINDOW
                    | (Activatable ? 0 : ExtendedWindowStyles.WS_EX_NOACTIVATE)
                    | (TopMost ? ExtendedWindowStyles.WS_EX_TOPMOST : 0)),
                    regResult,
                    string.Empty,
                    (uint)WindowStyles.WS_POPUP & ~(uint)WindowStyles.WS_SYSMENU,
                    0,
                    0,
                    0,
                    0,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    wind_class.hInstance,
                    IntPtr.Zero,
                    (int)ZBandID);

            if (hWnd == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            Handle = hWnd;

            hookManager.OnHwndCreated(hWnd);

            WindowCompositionHelper.MakeWindowTransparent(hWnd);

            HwndSourceParameters param = new()
            {
                WindowStyle = (int)(WindowStyles.WS_VISIBLE | WindowStyles.WS_CHILD | WindowStyles.WS_CLIPCHILDREN),
                ParentWindow = hWnd,
                UsesPerPixelTransparency = true,
            };

            hwndSource = new(param)
            {
                SizeToContent = SizeToContent.WidthAndHeight,
                RootVisual = this
            };

            hwndSource.CompositionTarget.BackgroundColor = Colors.Transparent;
            hwndSource.ContentRendered += HwndSource_ContentRendered;

            UpdateWindow(hWnd);

            IsSourceCreated = true;
        }

        private void HwndSource_ContentRendered(object sender, EventArgs e)
        {
            OnDpiChanged(GetDpiForWindow(Handle) / 96.0);
            UpdateSize(true);
        }

        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            base.OnDpiChanged(oldDpi, newDpi);

            OnDpiChanged(newDpi.DpiScaleX);
            UpdateSize(true);
        }

        private IntPtr myWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM_ACTIVATE:
                    HandleWindowActivation(wParam);
                    break;

                case WM_EXITSIZEMOVE:
                    OnDragMoved();
                    break;

                case WM_DESTROY:
                    DestroyWindow(hWnd);
                    break;

                case WM_DPICHANGED:
                    OnDpiChanged((wParam.ToInt32() & 0xFFFF) / 96.0);
                    UpdateSize(true);
                    break;

                default:
                    break;
            }

            hookManager.TryHandleWindowMessage(hWnd, msg, wParam, lParam);

            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private void HandleWindowActivation(IntPtr wParam)
        {
            bool isActive = wParam.ToInt32() != 0;
            IsActive = isActive;

            if (isActive)
            {
                OnActivated();
            }
            else
            {
                OnDeactivated();
            }
        }

        private void UpdateSize(bool sizeToContent = false)
        {
            if (_isSizeChanging)
                return;

            _isSizeChanging = true;

            double actualWidth = 0.0;
            double actualHeight = 0.0;

            if (sizeToContent)
            {
                if (Content is UIElement content)
                {
                    actualWidth = content.RenderSize.Width;
                    actualHeight = content.RenderSize.Height;
                }
            }
            else
            {
                actualWidth = ActualWidth;
                actualHeight = ActualHeight;
            }

            var width = (int)Math.Round(actualWidth * dpiScale);
            var height = (int)Math.Round(actualHeight * dpiScale);
            SetWindowPos(Handle, IntPtr.Zero, 0, 0,
                width,
                height,
                SWP.NOZORDER | SWP.NOMOVE | SWP.NOACTIVATE);

            UpdateWindow(Handle);

            _isSizeChanging = false;
        }

        #endregion

        #region Protected Methods

        protected void SetPosition(double x, double y)
        {
            if (!IsSourceCreated)
                return;

            SetWindowPos(Handle, IntPtr.Zero,
                (int)Math.Round(x),
                (int)Math.Round(y),
                0, 0, SWP.NOZORDER | SWP.NOSIZE | SWP.NOACTIVATE);

            UpdateWindow(Handle);

            ActualLeft = x;
            ActualTop = y;
        }

        protected virtual void OnActivated()
        {
            Activated?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnDeactivated()
        {
            Deactivated?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnDpiChanged(double newDpiScale)
        {
            dpiScale = newDpiScale;
            DpiChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnDragMoved()
        {
            IsDragMoving = false;

            GetWindowRect(Handle, out var rect);
            ActualLeft = rect.Left;
            ActualTop = rect.Top;

            DragMoved?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnShown()
        {
            Shown?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Public Methods

        public void DragMove()
        {
            if (!IsSourceCreated)
                return;

            SendMessage(Handle, WM_SYSCOMMAND, SC_MOUSEMOVE, 0);
            SendMessage(Handle, WM_LBUTTONUP, 0, 0);

            IsDragMoving = true;
        }

        public void Activate()
        {
            if (!IsSourceCreated)
                return;

            SetForegroundWindow(Handle);
        }

        public void Show()
        {
            if (!IsSourceCreated)
                CreateWindow();

            if (_isVisibilityChanging)
                return;

            _isVisibilityChanging = true;
            Visibility = Visibility.Visible;
            _isVisibilityChanging = false;

            if (TopMost)
            {
                SetWindowPos(Handle, new IntPtr(-1),
                    0, 0, 0, 0,
                    (Activatable ? 0 : SWP.NOACTIVATE) | SWP.NOMOVE | SWP.NOSIZE | SWP.NOOWNERZORDER | SWP.SHOWWINDOW);
            }
            else
            {
                ShowWindowAsync(Handle, Activatable ?
                (int)ShowWindowCommands.Show : (int)ShowWindowCommands.ShowNoActivate);
            }

            if (Activatable) SetForegroundWindow(Handle);

            OnShown();
        }

        public void Hide()
        {
            if (!IsSourceCreated || _isVisibilityChanging)
                return;

            ShowWindowAsync(Handle, (int)ShowWindowCommands.Hide);

            _isVisibilityChanging = true;
            Visibility = Visibility.Hidden;
            _isVisibilityChanging = false;
        }

        #endregion

        public event EventHandler Activated;

        public event EventHandler Deactivated;

        public event EventHandler DpiChanged;

        public event EventHandler DragMoved;

        public event EventHandler Shown;
    }
}
