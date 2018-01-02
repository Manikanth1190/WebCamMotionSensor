using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MotionSensorAForge
{
    public partial class ReferenceImage : Form
    {
        Image referenceImage;
        public ReferenceImage()
        {
            InitializeComponent();
        }
        public ReferenceImage(Image image)
        {
            InitializeComponent();
            referenceImage = image;
        }
        private void ReferenceImage_Load(object sender, EventArgs e)
        {
            pictureBox1.Image = referenceImage;
        }
    }
}
