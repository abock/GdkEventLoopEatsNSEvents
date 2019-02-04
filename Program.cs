using System;

using AppKit;
using CoreGraphics;

static class Program
{
    [System.Runtime.InteropServices.DllImport("libgtk-quartz-2.0")]
    static extern IntPtr gtk_ns_view_new(IntPtr nsview);

    const int WindowWidth = 600;
    const int WindowHeight = 400;
    const int Padding = 50;

    public static void Main(string[] args)
    {
        NSApplication.Init();
        Gtk.Application.Init();

        var gtkWindow = new Gtk.Window(Gtk.WindowType.Toplevel) {
            Title = "GTK Window",
            Gravity = Gdk.Gravity.Center,
            DefaultSize = new Gdk.Size(WindowWidth, WindowHeight),
            BorderWidth = Padding
        };

        gtkWindow.Add(new Gtk.Widget(gtk_ns_view_new(new NativeViewHost().Handle)));
        gtkWindow.ShowAll();

        var nativeWindow = new NSWindow(
            new CGRect(0, 0, WindowWidth, WindowHeight),
            NSWindowStyle.Titled | NSWindowStyle.Closable | NSWindowStyle.Resizable,
            NSBackingStore.Buffered,
            false)
        {
            Title = "Native Window",
            ContentView = new NativeViewHost()
        };

        nativeWindow.Center();
        nativeWindow.MakeKeyAndOrderFront(null);

        var menuBar = new NSMenu();
        var appMenuItem = new NSMenuItem();
        menuBar.AddItem(appMenuItem);

        var appMenu = new NSMenu();
        appMenu.AddItem(new NSMenuItem("Quit", new ObjCRuntime.Selector("terminate:"), "q"));
        appMenuItem.Submenu = appMenu;

        NSApplication.SharedApplication.Delegate = new NSApplicationDelegate();
        NSApplication.SharedApplication.MainMenu = menuBar;
        NSApplication.SharedApplication.ActivationPolicy = NSApplicationActivationPolicy.Regular;

        // Allow the native run loop to drive the app, and the GTK window
        // embedding a native view with a local event monitor will deliver
        // events as expected.
        // NSApplication.SharedApplication.Run();

        // Let GTK drive the app, dispatching to the native run loop as
        // necessary, and the native view with a local event monitor will
        // only deliver events when the mouse is within the view's frame.
        Gtk.Application.Run();
    }

    class NativeViewHost : NSView
    {
        public NativeViewHost()
        {
            Appearance = NSAppearance.GetAppearance(NSAppearance.NameVibrantLight);

            WantsLayer = true;
            Layer.BackgroundColor = NSColor.SystemOrangeColor.CGColor;

            var eventView = new NativeEventView();

            AddSubview(eventView);

            eventView.TranslatesAutoresizingMaskIntoConstraints = false;
            eventView.LeadingAnchor.ConstraintEqualToAnchor(LeadingAnchor).Active = true;
            eventView.TrailingAnchor.ConstraintEqualToAnchor(TrailingAnchor).Active = true;
            eventView.TopAnchor.ConstraintEqualToAnchor(TopAnchor).Active = true;
            eventView.BottomAnchor.ConstraintEqualToAnchor(BottomAnchor).Active = true;
        }
    }

    class NativeEventView : NSView
    {
        public NativeEventView()
        {
            var dragView = new NativeDragView();

            AddSubview(dragView);

            dragView.TranslatesAutoresizingMaskIntoConstraints = false;
            dragView.LeadingAnchor.ConstraintEqualToAnchor(LeadingAnchor, Padding).Active = true;
            dragView.TrailingAnchor.ConstraintEqualToAnchor(TrailingAnchor).Active = true;
            dragView.TopAnchor.ConstraintEqualToAnchor(TopAnchor, Padding).Active = true;
            dragView.BottomAnchor.ConstraintEqualToAnchor(BottomAnchor, -Padding).Active = true;
        }
    }

    class NativeDragView : NSView
    {
        readonly NSTextField field;
        bool isDragging;

        public NativeDragView()
        {
            WantsLayer = true;
            Layer.BackgroundColor = NSColor.SystemBlueColor.CGColor;

            field = new NSTextField
            {
                StringValue = "Click Me to Focus or Drag in Blue Rect",
                Alignment = NSTextAlignment.Center,
                Font = NSFont.UserFixedPitchFontOfSize(NSFont.SystemFontSizeForControlSize(NSControlSize.Regular)),
                Bordered = true,
                BezelStyle = NSTextFieldBezelStyle.Square,
                DrawsBackground = true,
                BackgroundColor = NSColor.TextBackground,
                TextColor = NSColor.ControlText,
                Editable = false
            };

            AddSubview(field);

            field.TranslatesAutoresizingMaskIntoConstraints = false;
            field.LeadingAnchor.ConstraintEqualToAnchor(LeadingAnchor, Padding).Active = true;
            field.TrailingAnchor.ConstraintEqualToAnchor(TrailingAnchor, -Padding).Active = true;
            field.CenterYAnchor.ConstraintEqualToAnchor(CenterYAnchor).Active = true;

            NSEvent.AddLocalMonitorForEventsMatchingMask(
                NSEventMask.LeftMouseDragged | NSEventMask.LeftMouseUp,
                e =>
                {
                    switch (e.Type)
                    {
                        case NSEventType.LeftMouseDragged when isDragging:
                            MouseDragged(e);
                            return null;
                        case NSEventType.LeftMouseUp:
                            isDragging = false;
                            break;
                    }

                    return e;
                });
        }

        public override void MouseDragged(NSEvent theEvent)
        {
            isDragging = true;
            field.StringValue = $"{theEvent.LocationInWindow.X:0000.00},{theEvent.LocationInWindow.Y:0000.00} t={theEvent.Timestamp:0.00}";
            base.MouseDragged(theEvent);
        }
    }
}