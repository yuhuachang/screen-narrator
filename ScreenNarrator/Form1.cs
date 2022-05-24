using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Speech.Synthesis;
using Patagames.Ocr;
using System.Drawing;
using System.Threading;
using Patagames.Ocr.Exceptions;

namespace ScreenNarrator
{
    public partial class Form1 : Form
    {
        private Bitmap bitmap;
        private delegate void OnMouseMoved(int x, int y);
        private OnMouseMoved onMouseMoved;
        private int startX;
        private int startY;
        private int stopX;
        private int stopY;

        public Form1()
        {
            InitializeComponent();

            onMouseMoved = new OnMouseMoved(UpdateMousePosition);
            backgroundWorker1.RunWorkerAsync();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer();
            speechSynthesizer.Volume = 50;
            speechSynthesizer.Speak("What will you learn?");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int left = 0;
            int top = 0;
            int width = 0;
            int height = 0;
            foreach (var screen in Screen.AllScreens)
            {
                int w = screen.Bounds.X + screen.Bounds.Width;
                int h = screen.Bounds.Y + screen.Bounds.Height;
                if (screen.Bounds.X < left) left = screen.Bounds.X;
                if (screen.Bounds.Y < top) top = screen.Bounds.Y;
                if (w > width) width = w;
                if (h > height) height = h;
            }

            Form f = new Form();
            Panel p = new Panel();
            p.Parent = f;
            p.Location = new Point(0, 0);
            p.Size = new Size(f.Width, f.Height);
            p.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom | AnchorStyles.Left;
            p.BackColor = Color.White;
            p.MouseDown += (sender1, e1) =>
            {
                startX = MousePosition.X;
                startY = MousePosition.Y;
            };
            p.MouseUp += (sender1, e1) =>
            {
                stopX = MousePosition.X;
                stopY = MousePosition.Y;
                f.Close();
            };
            p.MouseMove += (sender1, e1) =>
            {
                if (MouseButtons == MouseButtons.Left)
                {
                    using (Graphics g = p.CreateGraphics())
                    {
                        g.Clear(Color.White);
                        Brush brush = new SolidBrush(Color.Red);
                        Pen pen = new Pen(brush, 2);
                        g.DrawRectangle(pen, startX, startY, MousePosition.X - startX, MousePosition.Y - startY);
                    }
                }
            };

            f.FormBorderStyle = FormBorderStyle.None;
            f.Left = left;
            f.Top = top;
            f.Width = width;
            f.Height = height;
            f.FormClosed += (sender1, e1) =>
            {
                int captureWidth = stopX - startX;
                int captureHeight = stopY - startY;
                if (captureWidth > 0 && captureHeight > 0)
                {
                    bitmap = new Bitmap(captureWidth, captureHeight);
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(startX, startY, 0, 0, new Size(captureWidth, captureHeight));
                    }
                    pictureBox1.Image = bitmap;

                    try
                    {
                        using (var ocr = OcrApi.Create())
                        {
                            ocr.Init(Patagames.Ocr.Enums.Languages.English);
                            var text = ocr.GetTextFromImage(bitmap);
                            Console.WriteLine(text);

                            SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer();
                            speechSynthesizer.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Senior);
                            speechSynthesizer.Volume = 100;
                            speechSynthesizer.Rate = 2;
                            speechSynthesizer.Speak(text);
                        }
                    }
                    catch (NoLicenseException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            };
            f.Opacity = 0.3f;
            f.ShowDialog();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (var ocr = OcrApi.Create())
            {
                ocr.Init(Patagames.Ocr.Enums.Languages.English);
                var text = ocr.GetTextFromImage(bitmap);
                Console.WriteLine(text);
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            label1.Text = "x: " + e.X + " y: " + e.Y;
        }

        private void UpdateMousePosition(int x, int y)
        {
            label2.Text = "x: " + x + " y: " + y;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                if (label2.InvokeRequired)
                {
                    label2.Invoke(onMouseMoved, MousePosition.X, MousePosition.Y);
                }
                Thread.Sleep(50);
            }
        }
    }
}
