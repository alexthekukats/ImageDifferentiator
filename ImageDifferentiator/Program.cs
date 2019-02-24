using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ImageDifferentiator
{
    class Program //TODO: multi thread shit
    {
        static void Main(string[] args)
        {
            try
            {
                //Bitmap rightImg = null; //init type pointer
                List<ImgDataType> imageList = new List<ImgDataType>(); //init list where we store its location, width and height
                string[] files = Directory.GetFiles(@"D:\Alex\Pictures\megumin"); //load in megumin pics D:\Alex\Pictures\megumin

                if (File.Exists(ImgDataType.ExportLocation)) //load the created list if it exist
                {
                    int numberOfAdded;
                    Console.WriteLine("Loading db.");
                    imageList = ImgDataType.Load(); // load in db
                    Console.WriteLine("db Loaded.");
                    Console.WriteLine("Finding new Images in folder.");
                    FindNewImages(files, imageList, out numberOfAdded); //find new pictures in folder
                    Console.WriteLine("{0} Number of new Images are added to the list.", numberOfAdded);
                    int numberOfDeletedFiles = ImgDataType.DeleteRedundant(imageList); //delete redundant files set in db, and return with number of deleted files
                    Console.WriteLine("{0} Images were deleted.", numberOfDeletedFiles);
                }
                else //otherwise lets create 'em
                {
                    Bitmap leftImg = null;
                    Console.WriteLine("Creating db.");
                    foreach (string file in files) //in all the megumin pics
                    {
                        string ext = getExt(file); //not foolproof function
                        if (!(ext == ""))
                        {
                            leftImg = (Bitmap)Image.FromFile(file); //load one img into the memory
                            imageList.Add(new ImgDataType(file, leftImg.Width, leftImg.Height, ext, new FileInfo(file).Length)); //save picture location, its width and height and other data to our db
                            Console.WriteLine("{0} Added to db.", file);
                        }
                        //GC.Collect();
                    } //end of adding all the file location and its width and height
                    Console.WriteLine("List creating done.");
                }
                ImgDataType.Save(imageList); //save it
                Console.WriteLine("Saved.");


                Task.Factory.StartNew(() =>
                {
                    // Whatever code you want in your thread
                });
                CheckImagesForCollision(imageList, 0, 16);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadKey();
            }
        }


        public static void CheckImagesForCollision(List<ImgDataType> imageList, int startAt, int endAt, double pixelErrorRate = .15, double picturePassingRate = .9)
        {
            Bitmap leftImg = null; //init type pointer
            for (int i = 0; i < imageList.Count; i++) //in all the megumin pics from the list
            {
                leftImg = (Bitmap)Image.FromFile(imageList.ElementAt(i).fileLocation); //load one img into the memory
                Console.WriteLine("------------------------------------");
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("Testing for: {0}\n", imageList.ElementAt(i).fileLocation);
                Console.ResetColor();
                for (int j = i + 1; j < imageList.Count; j++)//foreach (ImgDataType imageList.ElementAt(i) in imageList) // start interating in the list once again
                {
                    //GC.Collect(); // no need to gc?!
                    if (!checkFileIfSame(imageList.ElementAt(i).fileLocation, imageList.ElementAt(j).fileLocation)) // is this the same picture?
                    {
                        if (imageList.ElementAt(i).ratio == imageList.ElementAt(j).ratio) // is the size ratio the same, meaning can we compare these?
                        {
                            Bitmap rightImg = (Bitmap)Image.FromFile(imageList.ElementAt(j).fileLocation); //load in the pic we want to compare with
                            rightImg = ResizeImage(rightImg, imageList.ElementAt(i).picWidth, imageList.ElementAt(i).picHeight); //resize it so the size equals with the other picture we compare with

                            double sumAvgDiff = 0;
                            int badPixel = 0;
                            int AllPixelCount = imageList.ElementAt(i).picWidth * imageList.ElementAt(i).picHeight;
                            for (int k = 0; k < imageList.ElementAt(i).picWidth; k++) // we suppose they have the same height and width
                            {
                                for (int l = 0; l < imageList.ElementAt(i).picHeight; l++)
                                {
                                    Color leftColor = leftImg.GetPixel(k, l); //get comparer color
                                    Color rightColor = rightImg.GetPixel(k, l); //get comparee color

                                    double redDiff = Math.Abs(leftColor.R - rightColor.R) / 255.0; //get the difference for all colors in percentage
                                    double greenDiff = Math.Abs(leftColor.G - rightColor.G) / 255.0;
                                    double blueDiff = Math.Abs(leftColor.B - rightColor.B) / 255.0;
                                    double avgDiff = (redDiff + greenDiff + blueDiff) / 3.0;

                                    if (avgDiff > pixelErrorRate) // test pixel if the difference is bigger than x%
                                    {
                                        badPixel++; //if not, increase the number of good pixels for match
                                    }
                                }
                                if (badPixel > (1.0 - picturePassingRate) * AllPixelCount)
                                {
                                    break;
                                }
                            }
                            sumAvgDiff = (double)badPixel / ((double)AllPixelCount); //get in percentage the good pixels on the comparee picture

                            if (sumAvgDiff > picturePassingRate)
                            {
                                Console.WriteLine("{0}\nMatch: {1}\n\n\n", imageList.ElementAt(j).fileLocation, sumAvgDiff);
                                if (sumAvgDiff == 1)
                                {
                                    imageList.ElementAt(j).markedForDeletion = true;
                                }
                                else
                                {
                                    using (StreamWriter sr = new StreamWriter("listofmatches.txt", true))
                                    {
                                        sr.WriteLine("{0}", imageList.ElementAt(i).fileLocation);
                                        sr.WriteLine("{0}\r\nMatch: {1}\r\n", imageList.ElementAt(j).fileLocation, sumAvgDiff);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        imageList.ElementAt(j).markedForDeletion = true;
                    }
                }
            }
        }

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public static List<ImgDataType> FindNewImages(string[] files, List<ImgDataType> imageList, out int numberOfAdded)
        {
            numberOfAdded = 0;
            int filesCount = files.Length;
            int imageListCount = imageList.Count;
            if (imageListCount == filesCount)
            {
                return imageList;
            }
            else if (filesCount > imageListCount)
            {
                int countDiff = filesCount - imageListCount;
                numberOfAdded = countDiff;
                bool isInTheList = false;
                for (int i = 0; i < filesCount; i++)
                {
                    for (int j = 0; j < imageListCount; j++)
                    {
                        if (files[i] == imageList.ElementAt(j).fileLocation)
                        {
                            isInTheList = true;
                            break;
                        }
                    }

                    if (!isInTheList)
                    {
                        Bitmap leftImg = (Bitmap)Image.FromFile(files[i]);
                        imageList.Add(new ImgDataType(files[i], leftImg.Width, leftImg.Height, getExt(files[i]), new FileInfo(files[i]).Length));
                        countDiff--;
                    }

                    if (countDiff == 0)
                    {
                        break;
                    }

                    isInTheList = false;
                }
            }
            return imageList;
        }

        static string getExt(string filename)
        {
            string lowerCaseFileLocation = filename.ToLower();
            string ext = "";
            if ((lowerCaseFileLocation.Contains(".jpg")) || (lowerCaseFileLocation.Contains(".jpeg")))
            {
                ext = ".jpg";
            }
            if (lowerCaseFileLocation.Contains(".png"))
            {
                ext = ".png";
            }
            if (lowerCaseFileLocation.Contains(".bmp"))
            {
                ext = ".bmp";
            }
            if (lowerCaseFileLocation.Contains(".tiff"))
            {
                ext = ".tiff";
            }
            return ext;
        }

        public static bool checkFileIfSame(string leftFile, string rightFile)
        {
            using (FileStream fs = new FileStream("file.txt", FileMode.Open))
            {
                using (var ms = new MemoryStream())
                {
                    fs.CopyTo(ms);
                    using (FileStream fs2 = new FileStream("file2.txt", FileMode.Open))
                    {
                        using (var ms2 = new MemoryStream())
                        {
                            fs2.CopyTo(ms2);
                            if (ms2.ToString() == ms.ToString())
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
