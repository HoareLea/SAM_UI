using System.Drawing;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Forms
{
    public partial class TestForm : Form
    {
        public TestForm()
        {
            InitializeComponent();
        }

        private void TestForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics gObject = CreateGraphics();

            Brush red = new SolidBrush(Color.Red);
            Rectangle rectangle = new Rectangle(10, 10, 100, 100);
            gObject.FillRectangle(red, 10, 10, 100, 50);
            Pen pen = new Pen(red, 2);
            gObject.DrawLine(pen, 10, 10, 10, 10);

        }
    }
}
