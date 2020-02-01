using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Test
{
    //System functions to work with console.
    //Descriptions can be found in Microsoft documentation
    //https://docs.microsoft.com/en-us/windows/console/console-reference

    static public class XConsole
    {
        //constants  ---------------------------------------------------------------------------- 

        //from wincon.h for CreateConsoleScreenBuffer function
        public const uint GENERIC_READ = 0x80000000;
        public const uint GENERIC_WRITE = 0x40000000;
        public const uint FILE_SHARE_READ = 0x00000001;
        public const uint FILE_SHARE_WRITE = 0x00000002;
        public const uint CONSOLE_TEXTMODE_BUFFER = 1;

        //Attributes byte in CHAR_INFO
        public const byte ATTR_FRAME_TOP = 0x4;
        public const byte ATTR_FRAME_LEFT = 0x8;
        public const byte ATTR_FRAME_RIGHT = 0x10;
        public const byte ATTR_FRAME_BOTTOM = 0x80; //is called underscore
        public const byte ATTR_INVERT = 0x40;

        //Palette size
        public const int PALETTE_SIZE = 0x10;

        //Default style: white on black
        public static Style DEFAULT_STYLE = new Style();

        //structures ----------------------------------------------------------------------------
        [StructLayout(LayoutKind.Explicit)]
        public struct CHAR_INFO
        {
            [FieldOffset(0)]
            public ushort UnicodeChar;//16bit chars. It's not UTF16! 0x00XX is ASCII
            [FieldOffset(1)]
            public byte ASCIIChar;//8bit chars ASCII
            [FieldOffset(2)]
            public byte Color; //BGRIBGRI 1-4 bg color, 5-8 text color,
            [FieldOffset(3)]
            public byte Attributes; //UUFFFFIU bits: 9 leadingbyte, 10trailingbyte, 11-13 frame, 14 undefined, 15 invert, 16 bottom frame


            public CHAR_INFO(ushort _char, byte _attr, byte _color) { ASCIIChar = 0; UnicodeChar = _char; Attributes = _attr; Color = _color; }

            //Unicode 16bits
            public CHAR_INFO(ushort _char, byte textcolor, byte bgcolor, byte attr)
            {
                ASCIIChar = 0;
                UnicodeChar = _char;
                Attributes = attr;
                Color = ColorConverter(bgcolor, textcolor);
            }

            //ASCII
            public CHAR_INFO(byte _char, byte textcolor, byte bgcolor, byte attr)
            {
                UnicodeChar = 0;
                ASCIIChar = _char;
                Attributes = attr;
                Color = ColorConverter(bgcolor, textcolor);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SMALL_COORD //not negative
        {
            public ushort x;
            public ushort y;
            public SMALL_COORD(int _x, int _y) { x = (ushort)_x; y = (ushort)_y; }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SMALL_RECT //not negative
        {
            public ushort Left;
            public ushort Top;
            public ushort Right;
            public ushort Bottom;
            public SMALL_RECT(int _Left, int _Top, int _Right, int _Bottom) { Left = (ushort)_Left; Top = (ushort)_Top; Right = (ushort)_Right; Bottom = (ushort)_Bottom; }
            public SMALL_RECT(SMALL_COORD LeftTop, SMALL_COORD RightBottom) { Left = LeftTop.x; Top = LeftTop.y; Right = RightBottom.x; Bottom = RightBottom.y; }

        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CONSOLE_SCREEN_BUFFER_INFO
        {
            public SMALL_COORD dwSize;
            public SMALL_COORD dwCursorPosition;
            public UInt16 wAttributes;
            public SMALL_RECT srWindow;
            public SMALL_COORD dwMaximumWindowSize;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct RGBX
        {
            public byte r, g, b, x;
            public RGBX(byte _r, byte _g, byte _b, byte _x = 0) { r = _r; g = _g; b = _b; x = _x; }
            public RGBX(int _r, int _g, int _b, int _x = 0) { r = (byte)_r; g = (byte)_g; b = (byte)_b; x = (byte)_x; }
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class PALETTE
        {
            public RGBX[] rgbx = new RGBX[16];
        };

        [StructLayout(LayoutKind.Explicit)]
        internal struct RGBXConverter
        {
            [FieldOffset(0)] public RGBX rgbx;
            [FieldOffset(0)] public uint UINT;
            public RGBXConverter(RGBX _rgbx) { UINT = 0; rgbx = _rgbx; }
            public RGBXConverter(uint _UINT) { rgbx = default; UINT = _UINT; }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct CONSOLE_SCREEN_BUFFER_INFO_EX
        {
            public int cbSize; //ulong in 32bits size must be 96bytes
            public SMALL_COORD dwSize;
            public SMALL_COORD dwCursorPosition;
            public UInt16 wAttributes;
            public SMALL_RECT srWindow;
            public SMALL_COORD dwMaximumWindowSize;
            public UInt16 wPopupAttributes;
            public UInt32 bFullscreenSupported; //not bool!
            public fixed uint Palette[16];
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CURSOR_INFO //the structure is 5 bytes. 
        {
            public SMALL_COORD dwSize; 
            public bool bVisible; //no info about it's length. May be there'd be safer to add 3 bytes to 8 bytes boundary
        }

        //functions ----------------------------------------------------------------------------

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateConsoleScreenBuffer(
            UInt32 dwDesiredAccess,
            UInt32 dwShareMode,
            IntPtr secutiryAttributes,
            UInt32 flags,
            IntPtr screenBufferData
            );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleActiveScreenBuffer(IntPtr hConsoleOutput);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteConsole(
                IntPtr hConsoleOutput, string lpBuffer,
                uint nNumberOfCharsToWrite, out uint lpNumberOfCharsWritten,
                IntPtr lpReserved);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetConsoleScreenBufferInfo(IntPtr handle, IntPtr lpScreenBufferInfo); //not tested

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetConsoleScreenBufferInfoEx(IntPtr handle, IntPtr lpScreenBufferInfo); //not tested

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleScreenBufferInfoEx(IntPtr handle, IntPtr lpScreenBufferInfo); //not tested

        [DllImport("kernel32.dll", SetLastError = true)]
        public static unsafe extern bool WriteConsoleOutput(
              IntPtr hConsoleOutput,
              CHAR_INFO* lpBuffer, //CHAR_INFO
              SMALL_COORD dwBufferSize,
              SMALL_COORD dwBufferCoord,
              SMALL_RECT* lpWriteRegion
              );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static unsafe extern bool SetConsoleCursorInfo(
            IntPtr hConsoleOutput,
            IntPtr lpConsoleCursorInfo
            );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static unsafe extern bool SetConsoleCursorPosition(
            IntPtr hConsoleOutput,
            SMALL_COORD dwPosition
            );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static unsafe extern bool SetConsoleTitle(string lpConsoleTitle);

        [DllImport("kernel32.dll")]
        public static unsafe extern int GetLastError(); //DWORD is int or uint? 0-0x4000 are used

        //Additional functions and converters-----------------------------------------------------------------

        public static unsafe bool SetCursor(IntPtr hConsoleOutput, bool visible)
        {
            CURSOR_INFO inf = new CURSOR_INFO();
            inf.dwSize.x=0;
            inf.dwSize.y=1;
            inf.bVisible = visible;
            IntPtr infptr = new IntPtr(&inf);
            return SetConsoleCursorInfo(hConsoleOutput, infptr);
        }

        public static int WriteLine(string sLine, IntPtr StdOut)
        {
            uint charsWritten = 0xFFFF; //-1
            WriteConsole(StdOut, sLine, (uint)sLine.Length, out charsWritten, IntPtr.Zero);
            return (int)charsWritten;
        }

        public static byte[] EncodeASCII(string text)
        {
            return Encoding.ASCII.GetBytes(text);
        }

        public static byte ColorConverter(byte bgcolor, byte textcolor)
        {
            return (byte)((bgcolor<<4) + (textcolor & 0xF));
        }

        public static byte ColorXor(byte high, byte low)
        {
            return (byte)((high & 0xF0) + (low & 0xF));
        }

        public static byte ColorShuffle(byte color)
        {
            return (byte)(((color & 0xF0) >> 4) + ((color & 0xF) << 4));
        }
    }

    //It's a region on the screen used to clip the content
    //transform has four coordinates of the region. 
    //0,0 is left top
    //The bottom coordinate sets the last USED line!
    //That means for one line TOP=BOTTOM
    public class Viewport
    {
        public XConsole.SMALL_RECT transform = new XConsole.SMALL_RECT(0, 0, 80, 25);
        public XConsole.SMALL_COORD scroll = new XConsole.SMALL_COORD(0, 0);

        public Viewport(XConsole.SMALL_RECT _transform)
        {
            transform = _transform;
        }
        public void Scroll(int x, int y) { scroll = new XConsole.SMALL_COORD((ushort)x, (ushort)y); }
    }

    

    //The console can have multiple pages.
    //Every page contains a handle for Microsoft API's functions
    //It's not a buffer and it's not a window. 
    //It has some properties like the cursor visibility.

    public class Page
    {
        public IntPtr Handle;
        private bool cursor = false;

        //initializes the page simply
        public unsafe void Initialize(bool _cursor = false)
        {
            Handle = XConsole.CreateConsoleScreenBuffer(
                            XConsole.GENERIC_READ | XConsole.GENERIC_WRITE, XConsole.FILE_SHARE_READ | XConsole.FILE_SHARE_WRITE,
                            IntPtr.Zero, XConsole.CONSOLE_TEXTMODE_BUFFER, IntPtr.Zero); 
            XConsole.SetCursor(Handle, _cursor);
            cursor = _cursor;
        }

        //initializes the page and sets the palette
        public unsafe void Initialize(XConsole.PALETTE palette, bool _cursor = false)
        {
            Initialize(_cursor);
            XConsole.CONSOLE_SCREEN_BUFFER_INFO_EX sbi = new XConsole.CONSOLE_SCREEN_BUFFER_INFO_EX();
            sbi.cbSize = sizeof(XConsole.CONSOLE_SCREEN_BUFFER_INFO_EX);
            IntPtr sbiptr = new IntPtr(&sbi);
            XConsole.GetConsoleScreenBufferInfoEx(Handle, sbiptr);
            for (int n = 0; n < XConsole.PALETTE_SIZE; n++) sbi.Palette[n] = new XConsole.RGBXConverter(palette.rgbx[n]).UINT;
            XConsole.SetConsoleScreenBufferInfoEx(Handle, sbiptr);
        }

        //draws a surface to the region marked as a viewport.
        //the viewport cuts the 
        public unsafe void Draw(Viewport vp, Surface surf)
        {   
            XConsole.SMALL_RECT temp = vp.transform;
            fixed (XConsole.CHAR_INFO* p = &surf.Buffer[0])
            {
                XConsole.WriteConsoleOutput(
                                  Handle,           // screen buffer to write to 
                                  p,                // buffer to copy from 
                                  surf.size,        // col-row size of Buffer 
                                  vp.scroll,        // top left src cell in Buffer 
                                  &temp);            // dest. screen buffer rectangle 
                //return XConsole.GetLastError();
            };
        }
     
        //Every page must be active before writing to it. 
        //Unfirtunately avtive=visible so there is no pageflipping
        public int Active()
        {
            XConsole.SetConsoleActiveScreenBuffer(Handle);
            return XConsole.GetLastError();
        }
    }

    //Every letter has attributes and color
    //It's uncomfortable to set it to every character
    //and there are no such cases
    //so you can set a style for writing a text
    //color contains 4 bits of background color and 4 bits of foreground color
    //0bBBBBFFFF
    //the frame contains attributes to draw a frame
    public class Style
    {
        public byte color;
        public bool frame;

        public Style()
        {
            color = 7;
            frame = false;
        }
        public Style(byte bgcolor, byte textcolor, bool _frame = false)
        {
            color = XConsole.ColorConverter(bgcolor, textcolor);
            frame = _frame;
        }
    }

    //It's a buffer of CHAR_INFO
    public class Surface
    {
        public XConsole.SMALL_COORD size;
        public XConsole.CHAR_INFO[] Buffer;
        private readonly int length;

        //virtual cursor if you want to write from the last position
        private int cursor_x;
        private int cursor_y;

        //the Buffer must be allocated
        private Surface(){}

        public unsafe Surface(XConsole.SMALL_COORD _size)
        {
            size = _size;
            length = size.x * size.y;
            cursor_x = cursor_y = 0;
            Buffer = new XConsole.CHAR_INFO[length];
        }

        //sets the virtual cursor to position
        public void MoveTo(int _x = 0, int _y = 0) { cursor_x = _x; cursor_y = _y; }

        //clears the buffer
        public void Clear(Style style, byte letter = 32)
        {
            for (int n = 0; n < length; n++) Buffer[n] = new XConsole.CHAR_INFO(letter, 0, style.color);
            if (style.frame)
            {
                for (int n = 0; n < size.x; n++)
                {
                    Buffer[n].Attributes += XConsole.ATTR_FRAME_TOP;
                    Buffer[length - n - 1].Attributes += XConsole.ATTR_FRAME_BOTTOM;
                }
                for (int n = 0; n < size.y; n++)
                {
                    Buffer[n * size.x].Attributes += XConsole.ATTR_FRAME_LEFT;
                    Buffer[(n + 1) * size.x - 1].Attributes += XConsole.ATTR_FRAME_RIGHT;
                }
            }
            MoveTo();//reset virtual cursor
        }

        //Scroll left one letter
        public void ScrollLeftAround(int chars)
        {
            XConsole.CHAR_INFO temp = Buffer[0];
            for (int n = 0; n < length - chars - 1; n++) Buffer[n] = Buffer[n + chars];
            Buffer[length - 1] = temp;
        }

        //Scroll line up
        public void ScrollUp(int lines)
        {
            int distance = size.x * lines;
            for (int n = 0; n < length - distance; n++) Buffer[n] = Buffer[n + distance];
            for (int n = length - distance; n < length; n++) Buffer[n] = new XConsole.CHAR_INFO();
        }

        //writes a text line. The next line starts from cursor_x
        //if the text is longer than the buffer the text trims
        //if the line is the last the buffer scrolls up
        public void WriteLine(string text, Style style) { WriteLine(XConsole.EncodeASCII(text), style); }
        public void WriteLine(byte[] chars)
        {
            if (cursor_y >= size.y) { ScrollUp(cursor_y - size.y + 1); cursor_y = size.y - 1; }
            int maxlength = size.x - cursor_x;
            int strlength = chars.Length;
            if (maxlength < strlength) strlength = maxlength;
            int offset = cursor_y * size.x + cursor_x;
            for (int n = 0; n < strlength; n++) Buffer[offset + n].UnicodeChar = chars[n];
            cursor_y++;
        }

        //writes a styled line
        public void WriteLine(byte[] chars, Style style)
        {
            if (cursor_y >= size.y) { ScrollUp(cursor_y - size.y + 1); cursor_y = size.y - 1; }
            int maxlength = size.x - cursor_x;
            int strlength = chars.Length;
            if (maxlength < strlength) strlength = maxlength;
            byte[] attr = new byte[strlength--];
            if (style.frame)
            {
                attr[0] = XConsole.ATTR_FRAME_TOP + XConsole.ATTR_FRAME_LEFT + XConsole.ATTR_FRAME_BOTTOM;
                for (int n = 1; n < strlength; n++) attr[n] = XConsole.ATTR_FRAME_TOP + XConsole.ATTR_FRAME_BOTTOM;
                attr[strlength] = XConsole.ATTR_FRAME_TOP + XConsole.ATTR_FRAME_RIGHT + XConsole.ATTR_FRAME_BOTTOM;
            }
            else for (int n = 0; n <= strlength; n++) attr[n] = 0;
            int offset = cursor_y * size.x + cursor_x;
            for (int n = 0; n < strlength; n++) Buffer[offset + n] = new XConsole.CHAR_INFO(chars[n], attr[n], style.color);
            cursor_y++;
        }

        //returns the virtual cursor
        public void NewLine()
        {
            cursor_y++;
            if (cursor_y >= size.y) { ScrollUp(cursor_y - size.y + 1); cursor_y = size.y - 1; }
            cursor_x = 0;
        }

        //writes a text. If the text is longer than a buffer it trims.
        //no scroll. The cursor position is at the end of the line or out of the limits
        public void Write(string text, Style style) { Write(XConsole.EncodeASCII(text), style); }

        public void Write(byte[] chars)
        {
            if (cursor_y >= size.y) return;
            int offset = cursor_y * size.x + cursor_x;
            int maxlength = length - offset;
            int strlength = chars.Length;
            if (maxlength < strlength) strlength = maxlength;
            for (int n = 0; n < strlength; n++) Buffer[offset + n].UnicodeChar = chars[n];
            cursor_x += strlength;
            cursor_y += cursor_x / size.x;
            cursor_x %= size.x;
        }

        //writes a styled text
        public void Write(byte[] chars, Style style)
        {
            if (cursor_y >= size.y) return;
            int offset = cursor_y * size.x + cursor_x;
            int maxlength = length - offset;
            int strlength = chars.Length;
            if (maxlength < strlength) strlength = maxlength;
            byte[] attr = new byte[strlength--];
            if (style.frame)
            {
                attr[0] = XConsole.ATTR_FRAME_TOP + XConsole.ATTR_FRAME_LEFT + XConsole.ATTR_FRAME_BOTTOM;
                for (int n = 1; n < strlength; n++) attr[n] = XConsole.ATTR_FRAME_TOP + XConsole.ATTR_FRAME_BOTTOM;
                attr[strlength] = XConsole.ATTR_FRAME_TOP + XConsole.ATTR_FRAME_RIGHT + XConsole.ATTR_FRAME_BOTTOM;
            }
            else for (int n = 0; n <= strlength; n++) attr[n] = 0;
            for (int n = 0; n <= strlength; n++) Buffer[offset + n] = new XConsole.CHAR_INFO(chars[n], attr[n], style.color);
            cursor_x += strlength + 1;
            cursor_y += cursor_x / size.x;
            cursor_x %= size.x;
        }
    }

    //Window is a basic structure to serve the console's window properly.
    //Unfortunately there is no possibility to get the events of this window
    //When the console's window resizes the artefacts appear
    //You can't get it's dimentions - the system functions work unproperly.
    //We need to have a clear surface blit to force the screen be good 
    //It slows down but you can also reduse it's execution
    static class Window
    {
        //the components are hidden so you don't need to know about the machanics
        private static Page page = new Page();
        private static Surface bgroundsurface;
        private static Viewport bgroundviewport;

        public static int Initialize(int width, int height, string lpConsoleTitle = "XConsole", XConsole.PALETTE palette = null, bool cursor = false)
        {
            XConsole.SetConsoleTitle(lpConsoleTitle);
            Console.SetWindowSize(width, height);
            Console.SetBufferSize(width, height);
            if (palette != null) page.Initialize(palette, cursor);
            else page.Initialize(cursor);
            bgroundsurface = new Surface(new XConsole.SMALL_COORD(width, height));
            bgroundsurface.Clear(XConsole.DEFAULT_STYLE);
            page.Active();
            bgroundviewport = new Viewport(new XConsole.SMALL_RECT(0, 0, width, height));
            return XConsole.GetLastError();
        }


        //refreshes the window if it's resized
        public static void Clear()
        {
            page.Draw(bgroundviewport, bgroundsurface);
        }

        //draws an element. The element must have two functions to be passed to page.Draw  
        public static void Draw<T>(T t) where T : IHaveAViewport, IHaveASurface
        {
            page.Draw(t.viewport, t.surface);
        }

        //draws anything anywhere
        public static void Draw(Viewport v, Surface s)
        {
            page.Draw(v, s);
        }
    }

    //There is no base object for elements but you need to pass them to page.Draw function
    //The interfaces simply guarantee the compatibility of your custom elements
    public interface IHaveAViewport
    {
        Viewport viewport { get; }
    }

    public interface IHaveASurface
    {
        Surface surface { get; }
    }

    // custom elements ---------------------------------------------------------------------------------------------

    //percentage scale
    public class PercentScale : IHaveASurface, IHaveAViewport
    {
        private Viewport view;
        private Surface surf;
        private int length;

        private PercentScale(){}

        public PercentScale(int position_x, int position_y, int length)
        {
            surf = new Surface(new XConsole.SMALL_COORD((ushort)length, 1));
            view = new Viewport(new XConsole.SMALL_RECT(position_x, position_y, position_x + length, position_y));//bottom==top!
            this.length = length;
        }

        //if you have a percent
        public void Update(float percent, Style s)
        {
            surf.Clear(s);
            int intp = (int)(length * percent / 100);
            byte[] number = Encoding.ASCII.GetBytes(new string(" %" + percent.ToString("F1")));
            int offset = (length - number.Length) / 2;
            for (int n = 0; n < number.Length; n++) surf.Buffer[n + offset].UnicodeChar = number[n];
            for (int n = 0; n < intp; n++) surf.Buffer[n].Attributes += XConsole.ATTR_INVERT;
        }

        //if you have two numbers
        public void Update(int quantity, int maxquantity, Style s)
        {
            surf.Clear(s);
            int intp = (int)((float)length * quantity / maxquantity);
            byte[] number = Encoding.ASCII.GetBytes(new string(quantity.ToString("F0") + "/" + maxquantity.ToString("F0")));
            int offset = (length - number.Length) / 2;
            for (int n = 0; n < number.Length; n++) surf.Buffer[n + offset].UnicodeChar = number[n];
            for (int n = 0; n < intp; n++) surf.Buffer[n].Attributes += XConsole.ATTR_INVERT;
        }

        //if you want to show a progress
        //unknown is a number outside the function 
        public void Update(ref int unknown, Style s)
        {
            surf.Clear(s);
            int marker = (int)((float)(length) / 20); //1/20 is a good length
            if (unknown++ == length + marker) unknown = -marker;
            int intp1 = unknown - marker;
            int intp2 = unknown + marker;
            if (intp1 < 0) intp1 = 0;
            if (intp2 > length) intp2 = length;
            for (int n = intp1; n < intp2; n++) surf.Buffer[n].Attributes += XConsole.ATTR_INVERT;
        }

        public Viewport viewport { get { return view; } private set { } }
        public Surface surface { get { return surf; } private set { } }
    }

    //It's a bitmap with frame.
    //The height of the bitmap is longer then it's viewport heigth
    //Viewport's Scroll is used to show the correct sprite
    //Every letter contains ASCII #220 '▄' and you only set it's color then.
    //The top half contains bground color and the bottom - foreground;
    //0bBBBBFFFF turns vertical: the high part is at the top
    public class Sprite : IHaveASurface, IHaveAViewport
    {
        protected Viewport view;
        protected Surface surf;
        protected int length;
        protected readonly int frames;
        protected readonly int width;
        protected readonly int height;

        private Sprite(){}

        public Sprite(int position_x, int position_y, int _width, int _height, int _frames = 1)
        {
            surf = new Surface(new XConsole.SMALL_COORD(_width, _height * _frames));
            view = new Viewport(new XConsole.SMALL_RECT(position_x, position_y, position_x + _width, position_y + _height-1));// -1!
            frames = _frames;
            width = _width;
            height = _height;
            length = _width * _height;
            Clear();
        }

        //use viewport scrolling to show the frame
        public void SetFrame(int frame)
        {
            if (frame < frames) view.Scroll(0, frame * height);
        }

        //y2 is in bitmap's coordinates.
        //for example y2=13 means 6 row bground color  
        public void SetPixel(int x, int y2, byte color16)
        {
            int y = y2 >> 1;
            if (x < 0 || y < 0 || x > surf.size.x - 1 || y > surf.size.y-1) return;
            if ((y2 & 1) == 1) surf.Buffer[y * surf.size.x + x].Color = XConsole.ColorXor(surf.Buffer[y * surf.size.x + x].Color, color16);
            else surf.Buffer[y * surf.size.x + x].Color = XConsole.ColorXor((byte)(color16 << 4), surf.Buffer[y * surf.size.x + x].Color);
        }

        //returns -1 if out of buffer.
        //otherwise the color is in lower 8 bits 
        public short GetPixel(int x, int y2) 
        {
            int y = y2 >> 1;
            if (x < 0 || y2 < 0 || x > surf.size.x - 1 || y > surf.size.y - 1) return -1;
            if ((y2 & 1) == 1) return (byte)(surf.Buffer[y * surf.size.x + x].Color >> 4);
            else return (byte)(surf.Buffer[y * surf.size.x + x].Color & 0xF);
        }

        //scrolls it up one pixel (0.5 lines)
        public void ScrollUp()
        {
            int length2 = length - surf.size.x;
            for (int n = 0; n < length2; n++) surf.Buffer[n].Color = (byte)((surf.Buffer[n].Color << 4) + ((surf.Buffer[n + surf.size.x].Color & 0xF0) >> 4));
            for (int n = 0; n < surf.size.x; n++) surf.Buffer[length2 + n].Color = (byte)((surf.Buffer[length2 + n].Color & 0xF) << 4);
        }

        //clears with 220 ASCII char
        public void Clear(byte color16 = 0) { Style s = new Style(color16, color16); surf.Clear(s, 220); }

        public Viewport viewport { get { return view; } private set { } }
        public Surface surface { get { return surf; } private set { } }
    }

    //The example of 'fire' animation.
    //It shows the speed.
    public class Fire : Sprite
    {
        Random r = new Random();

        public Fire(ushort position_x, ushort position_y, ushort _width, ushort _height):base(position_x,position_y,_width,_height){
            Reset();
            }

        public void Reset()
        {
            Clear(0);
            for (int n = 0; n < surf.size.x - 4; n++) if ((n & 8) > 0) SetPixel(n + 2, surf.size.y * 2 - 1, 12);
        }
        public void Update(int iterations)
        {
            int n = iterations;
            do
            {
                int position_x = r.Next(2, surf.size.x - 2);
                int position_y = r.Next(0, surf.size.y * 2 - 1);
                int pixel = (byte)(GetPixel(position_x, position_y) << 1);
                int pixelbelow = (byte)GetPixel(position_x, position_y + 1);
                int pixelleft = (byte)GetPixel(position_x - 1, position_y);
                int pixelright = (byte)GetPixel(position_x + 1, position_y);
                int pixelleft2 = (byte)(GetPixel(position_x - 2, position_y) >> 1);
                int pixelright2 = (byte)(GetPixel(position_x + 2, position_y) >> 1);
                int pixelleft3 = (byte)(GetPixel(position_x - 1, position_y + 1) >> 1);
                int pixelright3 = (byte)(GetPixel(position_x + 1, position_y + 1) >> 1);
                float average = (pixel + pixelbelow + pixelleft + pixelright + pixelleft2 + pixelright2 + pixelleft3 + pixelright3) / 6.666f;
                //if (average==0) average=4; 
                SetPixel(position_x, position_y, (byte)((int)average));
            } while (n-- > 0);
            ScrollUp();
            for (int m = 0; m < surf.size.x - 4; m++) if ((m & 8) > 0) SetPixel(m + 2, surf.size.y * 2 - 1, 12);
        }
    }

    //The test
    class Test_core
    {
        static unsafe void Main(string[] args)
        {
            //create custom palette 
            XConsole.PALETTE pal = new XConsole.PALETTE();
            pal.rgbx[1] = new XConsole.RGBX(35, 1, 5);
            pal.rgbx[2] = new XConsole.RGBX(60, 7, 3);
            pal.rgbx[3] = new XConsole.RGBX(100, 22, 2);
            pal.rgbx[4] = new XConsole.RGBX(143, 44, 0);
            pal.rgbx[5] = new XConsole.RGBX(181, 61, 1);
            pal.rgbx[6] = new XConsole.RGBX(224, 60, 0);
            pal.rgbx[7] = new XConsole.RGBX(200, 60, 80);
            pal.rgbx[8] = new XConsole.RGBX(170, 60, 130);
            pal.rgbx[9] = new XConsole.RGBX(150, 78, 155);
            pal.rgbx[10] = new XConsole.RGBX(118, 95, 188);
            pal.rgbx[11] = new XConsole.RGBX(100, 170, 200);
            pal.rgbx[12] = new XConsole.RGBX(157, 227, 236);
            pal.rgbx[13] = new XConsole.RGBX(0x7F, 0x7F, 0x7F);
            pal.rgbx[14] = new XConsole.RGBX(0x3F, 0x0, 0x0);
            pal.rgbx[15] = new XConsole.RGBX(0x0, 0xFF, 0x0);

            //create a crutch and set the console title and console's window size
            Window.Initialize(129,59,"XConsole",pal);

            //a pair of styles
            Style s = new Style(1, 4, true);
            Style s2 = new Style(0, 13, true);
 
            //separate surface ans viewport
            Surface simpletext = new Surface(new XConsole.SMALL_COORD(64, 40));
            Viewport simpletextvp = new Viewport(new XConsole.SMALL_RECT(80, 27, 115, 45));

            //write text till the surface is filled
            //the text wraps and has different colors
            for (int n = 0; n < 100; n++)
            {
                s.color = (byte)(n);
                simpletext.Write(XConsole.EncodeASCII(" HELLO WORLD " + n.ToString()), s);
            }

            //three percentage scales
            PercentScale ps = new PercentScale(10, 50, 100);
            PercentScale ps2 = new PercentScale(10, 52, 100);
            PercentScale ps3 = new PercentScale(10, 54, 100);
            
            //fire effect
            Fire fire = new Fire(5, 0, 72, 48);

            //sprite with 4 frames
            Sprite bm = new Sprite(80, 3, 35, 20, 4);
            for (int l=0;l<4;l++)
                for (int n = 0; n < 40; n++) 
                    for (int i = 0; i < bm.surface.size.x; i++) {
                        bm.SetPixel(i, n+l*40, (byte)((l*3)+6));
                        bm.SetPixel(0,0+l*40, 13);
                        bm.SetPixel(34, 39 + l * 40, 13);
                    }
    
            int unknown=0;
            int frame=0;
            float k=0;
    
            //the cycle
            do
            {
                Window.Clear(); //clear the screen with black
                Window.Draw(ps);
                Window.Draw(ps2);
                Window.Draw(ps3);
                Window.Draw(fire);
                bm.SetFrame(frame>>4); //sets a frame
                Window.Draw(bm);
                Window.Draw(simpletextvp,simpletext);

                Thread.Sleep(10);  //relaxes the system, prevent the screen flashing

                //updates the elements
                ps.Update(k, s2);
                ps2.Update((int)(100-k),100, s2);
                ps3.Update(ref unknown, s2);
                fire.Update(2000); 
                if (k < 100) k += 0.1f; else k = 0;
                if (frame++==64) frame=0;
                
            } while (!Console.KeyAvailable);
            Console.ReadKey();
        }
    }
}
