using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace TiradorDeDados
{
    public partial class TiradorDadosForm : Form
    {
        private bool activado = false;


        public TiradorDadosForm()
        {
            InitializeComponent();
        }

        private void startBtn_Click(object sender, EventArgs e)
        {
            this.activado = true;
            this.escanear();
        }

        private void stopBtn_Click(object sender, EventArgs e)
        {
            this.activado = false;
        }

        private void escanear()
        {
            bool encontrado = false;
            while (activado && !encontrado)
            {
                //Tirar dados
                int[] destino = { 680, 100};
                int[] original = this.obtenerPosicionMouse();   //guardar Posicion original
                this.clickear(destino);     //clickear en los dados
                this.mover(original);       //Devolver el mouse a posicion original

                this.Hide();
                Thread.Sleep(Int32.Parse(this.txtInterval.Text));
                //Sacar una screenShot
                Bitmap bt = this.capturar();
                //Evaluar patron
                if (this.evaluarAtributos(bt))
                {
                    encontrado = true;
                }
                this.Show();
                Thread.Sleep(Int32.Parse(this.txtInterval.Text));
            }
        }

        private bool evaluarAtributos(Bitmap bt)
        {
            /*Constitución:569,137 - Carisma: 569,114 - Inteligencia: 569,91 - Agilidad: 569,68 - Fuerza: 569,46 */
            bool encontrado = true;
            if (chkFuerza.Checked) encontrado &= this.esUnOcho(569, 46, bt);
            if (chkAgilidad.Checked) encontrado &= this.esUnOcho(569, 68, bt);
            if (chkInteligencia.Checked) encontrado &= this.esUnOcho(569, 91, bt);
            if (chkCarisma.Checked) encontrado &= this.esUnOcho(569, 114, bt);
            if (chkConstitucion.Checked) encontrado &= this.esUnOcho(569, 137, bt);
            return encontrado;
        }

        private bool esUnOcho(int x, int y, Bitmap bt)
        {
            int[,] blancos = {    //  Matríz de 8col x 10filas
              {0,0,0,1,1,0,0,0},    //---oo---
              {0,0,1,1,1,1,0,0},    //--oooo--
              {0,0,0,0,0,0,0,0},    //--------
              {0,0,0,0,0,0,0,0},    //--------
              {0,0,1,1,1,1,0,0},    //--oooo--
              {0,0,1,1,1,1,0,0},    //--oooo--
              {0,1,0,0,0,0,0,0},    //-o------
              {0,1,0,0,0,0,1,0},    //-o----o-
              {0,0,1,1,1,1,0,0},    //--oooo--
              {0,0,0,1,1,0,0,0}     //---oo---
            };
            for (int fila = 0; fila < 10; fila++)
                for (int columna = 0; columna < 8; columna++)
                    if (blancos[fila, columna] == 1 && !this.esBlanco(bt.GetPixel(x + columna, y + fila)))
                        return false;
            return true;
        }

        private bool esBlanco(Color color)
        {
            return color.R == 255 && color.G == 255 && color.B == 255;
        }

        /*RELACIONADO CON LOS SCREENSHOTS*/
        private Graphics screenShot;
        private Bitmap capturar()
        {
            Bitmap bt = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, PixelFormat.Format32bppArgb);
            screenShot = Graphics.FromImage(bt);
            screenShot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, 0, 0, Screen.PrimaryScreen.Bounds.Size, CopyPixelOperation.SourceCopy);
            return bt;
        }

        
        /*RELACIONADO CON EL MOUSE*/
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT { int dx; int dy; int mouseData; public int dwFlags; int time; IntPtr dwExtraInfo; }
        struct INPUT { public uint dwType; public MOUSEINPUT mi; }
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        private void clickear(int[] posicion) {
            Cursor.Position = new Point(posicion[0], posicion[1]);
            var input = new INPUT() { dwType = 0, mi = new MOUSEINPUT() { dwFlags = MOUSEEVENTF_LEFTDOWN } };
            SendInput(1, new INPUT[] { input }, Marshal.SizeOf(input));
            input = new INPUT() { dwType = 0, mi = new MOUSEINPUT() { dwFlags = MOUSEEVENTF_LEFTUP } };
            SendInput(1, new INPUT[] { input }, Marshal.SizeOf(input));
        }

        private void mover(int[] posicion) {
            Cursor.Position = new Point(posicion[0], posicion[1]);
        }

        private int[] obtenerPosicionMouse()
        {
            return new int[] { Cursor.Position.X, Cursor.Position.Y };
        }

        /*RELACIONADO CON HOOK DEL TECLADO PARA EL ESCAPE
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        public static void Main()
        {
            _hookID = SetHook(_proc);
            Application.Run();
            UnhookWindowsHookEx(_hookID);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc( int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback( int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Console.WriteLine((Keys)vkCode);
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
         */
    }
}
