using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Threading;

namespace ClientSystem
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        static byte[] EncryptStringToBytes(string plainText, byte[] Key, byte[] IV)
        {
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted; 
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }
            return encrypted;
        }

        static string DecryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV)
        { 
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            string plaintext = null;

            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
            return plaintext;
        }

        void Info(Color[,] PixelData, int X, int Y, Bitmap Image)
        {
            PixelData[X, Y] = Image.GetPixel(X, Y);
        }

        void MainProcess(string original, List<string> list)
        {
            try
            {
                using (RijndaelManaged myRijndael = new RijndaelManaged())
                {
                    myRijndael.GenerateKey();
                    myRijndael.GenerateIV();
                    byte[] encrypted = EncryptStringToBytes(original, myRijndael.Key, myRijndael.IV); 
                    string roundtrip = DecryptStringFromBytes(encrypted, myRijndael.Key, myRijndael.IV);
                    list.Add(roundtrip);
                }
            }
            catch (Exception ex){ MessageBox.Show(ex.ToString()); }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (!String.IsNullOrEmpty(textBox1.Text)) folderpath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\\{textBox1.Text}";
                else folderpath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\\NewFolder1";
                Directory.CreateDirectory(folderpath);
                button1.Visible = true; button1.Enabled = true;
            }
            catch { MessageBox.Show("Virhe"); }
        }

        public string[] imagePath;

        void ImageOperations(int index)
        {
            List<string> list = new List<string>();
            int counter = 0; list.Clear();

            using (var fileStream = new FileStream(imagePath[index], FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (Bitmap image = new Bitmap(imagePath[index]))
                {

                    Color[,] pixelData = new Color[image.Width, image.Height];
                    Color[,] newpixelData = new Color[image.Width, image.Height];

                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            Info(pixelData, x, y, image);
                            string currentPixel = Convert.ToString(pixelData[x, y].ToArgb());
                            MainProcess(currentPixel, list);
                            newpixelData[x, y] = Color.FromArgb(Convert.ToInt32(list[counter]));
                            if (counter >= image.Width * image.Height) break;
                            else counter++;
                        }
                    }

                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            image.SetPixel(x, y, newpixelData[x, y]);
                        }
                    }
                    image.Save($@"{folderpath}\new{index}.jpeg");
                }
            }
        }
        void TaskRunner(int indexer)
        {
           list.Add( Task.Run(() =>
            {
                ImageOperations(indexer);
            }));
        }
        static string filepath;
        static string folderpath;

        List<Task> list = new List<Task>();

        private async void button1_Click_1Async(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(textBox2.Text))
            {
                filepath = textBox2.Text;
                imagePath = Directory.GetFiles($@"{filepath}", "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".jpeg") || s.EndsWith(".png") || s.EndsWith(".JPG")).ToArray();
                if (imagePath.Length > 0)
                {
                    var timer = new Stopwatch();
                    timer.Start();
                    progressBar2.Maximum = imagePath.Length;

                    int indexer = 0;
                    while (indexer < imagePath.Length)
                    {
                        TaskRunner(indexer);
                        indexer++;
                    }

                    while (list.Any())
                    {
                        Task finishedTask = await Task.WhenAny(list);
                        list.Remove(finishedTask);
                        progressBar2.Value++;
                        if (list.Count == 0)
                        {
                            timer.Stop();
                            TimeSpan timeTaken = timer.Elapsed;
                            label3.Text = "Time taken: " + timeTaken.ToString(@"m\:ss\.fff");
                        }
                    }
                }
                else { MessageBox.Show("Folder is empty"); }
            }
            else { MessageBox.Show("Path is empty"); }
        }
    }
}
