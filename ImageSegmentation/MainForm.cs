using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using ImageTemplate;
using System.Linq;
using System.Diagnostics;


namespace ImageTemplate
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        RGBPixel[,] ImageMatrix;

        private void btnOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //Open the browsed image and display it
                string OpenedFilePath = openFileDialog1.FileName;
                ImageMatrix = ImageOperations.OpenImage(OpenedFilePath);
                ImageOperations.DisplayImage(ImageMatrix, pictureBox1);
            }
            txtWidth.Text = ImageOperations.GetWidth(ImageMatrix).ToString();
            txtHeight.Text = ImageOperations.GetHeight(ImageMatrix).ToString();
        }

        private void btnGaussSmooth_Click(object sender, EventArgs e)
        {
            double sigma = double.Parse(txtGaussSigma.Text);
            int maskSize = (int)nudMaskSize.Value;
            ImageMatrix = ImageOperations.GaussianFilter1D(ImageMatrix, maskSize, sigma);
            ImageOperations.DisplayImage(ImageMatrix, pictureBox2);
        }

        private void btnSegmentRegions_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(txtKValue.Text, out int k))
            {
                MessageBox.Show("Please enter a valid number for k parameter.");
                return;
            }

            // Use segmentation with RGB channel intersection
            var result = RegionSegmentation.SegmentByChannels(ImageMatrix, k);

            textBox_time.Text = RegionSegmentation.Timeelapsed.ToString("0.00") + " ms";
            // Show the colored result
            ImageOperations.DisplayImage(result.ColoredRegions, pictureBox2);

            // Display info
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Number of regions: " + result.RegionCount);
            sb.AppendLine("Sizes (from largest to smallest):");
            foreach (var size in result.RegionSizes)
                sb.AppendLine(size.ToString());
            txtRegionsInfo.Text = sb.ToString();

            System.IO.File.WriteAllLines("SegmentResults.txt",
                new[] { result.RegionCount.ToString() }
                .Concat(result.RegionSizes.Select(s => s.ToString())));


        }

        private void MainForm_Load(object sender, EventArgs e)
        {
        }
    }
}