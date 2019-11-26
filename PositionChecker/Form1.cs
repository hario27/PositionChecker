using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PositionChecker
{
    public partial class Form1 : Form
    {
        // 矩形が入ったイメージを保存
        public List<Mat> matList = new List<Mat>();
        public string imageFolder = "";

        // 矩形を保存
        public List<Rect> rectList = new List<Rect>();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            var filePath = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            var fileName = Path.GetFileName(filePath[0]);
            var ext = Path.GetExtension(filePath[0]);

            if(this.pictureBox1.Image == null)
            {
                if (ext == ".jpg")
                {
                    this.button1.Enabled = true;
                    this.pictureBox1.Image = CreateImage(filePath[0]);
                    Mat mat = BitmapConverter.ToMat(new Bitmap(this.pictureBox1.Image)).CvtColor(ColorConversionCodes.BGRA2BGR);
                    matList = new List<Mat>() { mat };
                    imageFolder = Path.GetDirectoryName(filePath[0]);
                }
            }
            else
            {
                Mat mat = BitmapConverter.ToMat(new Bitmap(CreateImage(filePath[0]))).CvtColor(ColorConversionCodes.BGRA2BGR);
                if(ext == ".jpg" && mat.Width == 960 && mat.Height == 540)
                {
                    Mat newMat = BitmapConverter.ToMat(new Bitmap(CreateImage(filePath[0]))).CvtColor(ColorConversionCodes.BGRA2BGR);
                    matList = new List<Mat>() { newMat };
                    Mat newMat1 = matList[0].Clone();
                    rectList.ForEach(item =>
                    {
                        Cv2.Rectangle(newMat1, item, new Scalar(0, 255, 0), 2);
                    });
                    Image image = BitmapConverter.ToBitmap(newMat1);
                    this.pictureBox1.Image = image;
                    imageFolder = Path.GetDirectoryName(filePath[0]);
                }
                else
                {
                    this.button2.Enabled = true;
                    this.button4.Enabled = true;
                    createCv2Rectangle(filePath[0]);
                }
            }
            
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if(this.textBox1.Text != "")
            {
                Clipboard.SetText(this.textBox1.Text);
            }
        }

        private void createCv2Rectangle(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var ext = Path.GetExtension(filePath);

            if ((ext == ".png" || ext == ".jpg") && this.pictureBox1.Image != null)
            {
                Mat tempMat = matList[matList.Count - 1].Clone();

                using (Mat mat = tempMat)
                using (Mat temp = BitmapConverter.ToMat(new Bitmap(filePath)).CvtColor(ColorConversionCodes.BGRA2BGR))
                using (Mat result = new Mat())
                {
                    if(temp.Width == 960 && temp.Height == 540)
                    {
                        return;
                    }
                    if(ext == ".jpg")
                    {
                        Cv2.Resize(temp, temp, new OpenCvSharp.Size(0.5 * temp.Width, 0.5 * temp.Height), 0, 0, InterpolationFlags.Lanczos4);
                    }
                    
                    // テンプレートマッチ
                    Cv2.MatchTemplate(mat, temp, result, TemplateMatchModes.CCoeffNormed);

                    // 類似度が最大/最小となる画素の位置を調べる
                    OpenCvSharp.Point minloc, maxloc;
                    double minval, maxval;
                    Cv2.MinMaxLoc(result, out minval, out maxval, out minloc, out maxloc);

                    // しきい値で判断
                    for (var i = 1.0; i > 0.3; i-=0.05)
                    {
                        if (maxval >= i)
                        {

                            // 最も見つかった場所に赤枠を表示
                            Rect rect = new Rect(maxloc.X, maxloc.Y, temp.Width, temp.Height);
                            rectList.Add(rect);
                            rectList.ForEach(item =>
                            {
                                Cv2.Rectangle(mat, item, new Scalar(0, 255, 0), 2);
                            });

                            // ウィンドウに矩形を表示
                            Image image = BitmapConverter.ToBitmap(mat);
                            this.pictureBox1.Image = image;

                            // CSSを表示
                            int minX = 960;
                            int maxX = 0;
                            int minY = 540;
                            int maxY = 0;

                            rectList.ForEach(item =>
                            {
                                if (item.X < minX)
                                {
                                    minX = item.X;
                                }
                                if (item.X + item.Width > maxX)
                                {
                                    maxX = item.X + item.Width;
                                }
                                if (item.Y < minY)
                                {
                                    minY = item.Y;
                                }
                                if (item.Y + item.Height > maxY)
                                {
                                    maxY = item.Y + item.Height;
                                }

                            });

                            this.textBox1.Text = string.Format(@"left:{0}px; top:{1}px; width:{2}px; height:{3}px;", minX, minY, maxX - minX, maxY - minY);

                            return;
                        }
                    }
                    // 見つからない
                    MessageBox.Show("見つかりませんでした");
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(rectList.Count == 0)
            {
                return;
            }
            int minX = 960;
            int maxX = 0;
            int minY = 540;
            int maxY = 0;

            rectList.ForEach(item =>
            {
                if(item.X < minX)
                {
                    minX = item.X;
                }
                if(item.X + item.Width > maxX)
                {
                    maxX = item.X + item.Width;
                }
                if(item.Y < minY)
                {
                    minY = item.Y;
                }
                if(item.Y + item.Height > maxY)
                {
                    maxY = item.Y + item.Height;
                }
                
            });
            /*
            //SaveFileDialogクラスのインスタンスを作成
            SaveFileDialog sfd = new SaveFileDialog();

            //はじめのファイル名を指定する
            //はじめに「ファイル名」で表示される文字列を指定する
            sfd.FileName = "bg.jpg";
            //はじめに表示されるフォルダを指定する
            sfd.InitialDirectory = @"C:\";
            //[ファイルの種類]に表示される選択肢を指定する
            //指定しない（空の文字列）の時は、現在のディレクトリが表示される
            sfd.Filter = "JPGファイル(*.jpg;*.JPG)|*.html;*.htm|すべてのファイル(*.*)|*.*";
            //[ファイルの種類]ではじめに選択されるものを指定する
            //2番目の「すべてのファイル」が選択されているようにする
            sfd.FilterIndex = 2;
            //タイトルを設定する
            sfd.Title = "保存先のファイルを選択してください";
            //ダイアログボックスを閉じる前に現在のディレクトリを復元するようにする
            sfd.RestoreDirectory = true;
            //既に存在するファイル名を指定したとき警告する
            //デフォルトでTrueなので指定する必要はない
            sfd.OverwritePrompt = true;
            //存在しないパスが指定されたとき警告を表示する
            //デフォルトでTrueなので指定する必要はない
            sfd.CheckPathExists = true;

            //ダイアログを表示する
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                //OKボタンがクリックされたとき、選択されたファイル名を表示する
                Console.WriteLine(sfd.FileName);
            }
            */
            System.IO.File.Delete(imageFolder + "\\temp.jpg");
            var mat2 = matList[0].Clone(new Rect(minX, minY, maxX - minX, maxY - minY));
            Cv2.ImWrite(imageFolder + "\\temp.jpg", mat2);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.button1.Enabled = false;
            this.button2.Enabled = false;
            this.button4.Enabled = false;
            this.textBox1.Text = "";
            this.pictureBox1.Image = null;
            rectList = new List<Rect>();
        }

        public static System.Drawing.Image CreateImage(string filename)
        {
            System.IO.FileStream fs = new System.IO.FileStream(
                filename,
                System.IO.FileMode.Open,
                System.IO.FileAccess.Read);
            System.Drawing.Image img = System.Drawing.Image.FromStream(fs);
            fs.Close();
            return img;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Location = Properties.Settings.Default.location;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.state = this.WindowState;
            if (this.WindowState == FormWindowState.Normal)
            {
                // ウインドウステートがNormalな場合には位置（location）とサイズ（size）を記憶する。
                Properties.Settings.Default.location = this.Location;
            }
            else
            {
                // もし最小化（minimized）や最大化（maximized）の場合には、RestoreBoundsを記憶する。
                Properties.Settings.Default.location = this.RestoreBounds.Location;
            }

            // ここで設定を保存する
            Properties.Settings.Default.Save();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if(this.pictureBox1.Image != null)
            {
                Mat mat = matList[0].Clone();
                rectList.ForEach(item =>
                {
                    Cv2.Rectangle(mat, item, new Scalar(0, 255, 0), 2);
                });
                this.pictureBox1.Image = BitmapConverter.ToBitmap(mat);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.button2.Enabled = false;
            this.button4.Enabled = false;
            this.textBox1.Text = "";
            rectList = new List<Rect>();
            if(this.pictureBox1.Image != null)
            {
                Mat mat = matList[0].Clone();
                this.pictureBox1.Image = BitmapConverter.ToBitmap(mat);
            }
        }

        private void 終了XToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
