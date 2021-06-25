using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace IOTLinkAgent.Agent.Commands
{
    class Screenshot : ICommand
    {
        private const string COMMAND_LINE = "screenshot";

        public string GetCommandLine()
        {
            return COMMAND_LINE;
        }

        public int ExecuteCommand(string[] args)
        {
            if (!Environment.UserInteractive)
                return -1;

            string filename = string.Join(" ", args);

            using (Bitmap bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    int screenwidth = Screen.GetBounds(new Point(0, 0)).Width;
                    int screenheight = Screen.GetBounds(new Point(0, 0)).Height;
                    g.CopyFromScreen(0, 0, 0, 0, new Size(screenwidth, screenheight));
                    bmp.Save(filename, ImageFormat.Png);
                }
            }
            return 0;
        }
    }
}
