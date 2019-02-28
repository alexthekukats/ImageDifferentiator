using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading;

namespace ImageDifferentiator
{
    class Program
    {
        public static List<string> findings = new List<string>();

        static void Main(string[] args)
        {
            try
            {
                //Bitmap rightImg = null; //init type pointer
                List<ImgDataType> imageList = new List<ImgDataType>(); //init list where we store the DataType for the Imgs
                string[] files = Directory.GetFiles(@"D:\Alex\Pictures\megumin", "*.*", SearchOption.AllDirectories); //load in megumin pics D:\Alex\Pictures\megumin
                
                if (File.Exists(ImgDataType.ExportLocation)) //load the created list if it exist
                {
                    int numberOfAdded;
                    Console.WriteLine("Loading db.");
                    imageList = ImgDataType.Load(); // load in db
                    Console.WriteLine("db Loaded.");
                    Console.WriteLine("Finding new Images in folders.");
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
                    } //end of adding all the file location and its width and height
                    Console.WriteLine("List creating done.");
                }
                ImgDataType.Save(imageList); //save it
                Console.WriteLine("Saved.");

                MultiThreadingSearch(imageList, 8);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadKey();
            }
        }

        public static void MultiThreadingSearch(List<ImgDataType> imageList, int divider)
        {
            List<Task> taskList = new List<Task>();
            int num = imageList.Count / divider;
            for (int i = 0; i < divider; i++)
            {
                int startAt = i * num;
                int endAt = i * num + num;
                taskList.Add
                (
                    Task.Factory.StartNew
                    (() =>
                        {
                            CheckImagesForMatch(imageList, startAt, endAt);
                        }
                    )
                );
            }
            Task.WaitAll(taskList.ToArray());
            writeMatchesToFile();
        }


        public static void CheckImagesForMatch(List<ImgDataType> imageList, int startAt, int endAt, double pixelErrorRate = .15, double picturePassingRate = .9)
        {
            Bitmap leftImg = null; //init type pointer
            
            for (int i = startAt; i < endAt; i++) //in all the megumin pics from the list
            {
                SpinWait.SpinUntil(delegate
                {
                    try
                    {
                        leftImg = (Bitmap)Image.FromFile(imageList.ElementAt(i).fileLocation); //load one img into the memory
                    }
                    catch
                    {
                        return false;
                    }
                    return true;
                });
                
                Console.WriteLine("------------------------------------");
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("Testing for: {0}\n", imageList.ElementAt(i).fileLocation);
                Console.ResetColor();
                for (int j = 0; j < imageList.Count; j++)//foreach (ImgDataType imageList.ElementAt(i) in imageList) // start interating in the list once again
                {
                    Bitmap rightImg = null;
                    if (!checkFileIfSame(imageList.ElementAt(i).fileLocation, imageList.ElementAt(j).fileLocation)) // is this the same picture?
                    {
                        if (imageList.ElementAt(i).ratio == imageList.ElementAt(j).ratio) // is the size ratio the same, meaning can we compare these?
                        {
                            SpinWait.SpinUntil(delegate
                            {
                                try
                                {
                                    rightImg = (Bitmap)Image.FromFile(imageList.ElementAt(j).fileLocation); //load in the pic we want to compare with
                                }
                                catch
                                {
                                    return false;
                                }
                                return true;
                            });
                            
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
                                    findings.Add(imageList.ElementAt(i).fileLocation);
                                    findings.Add(imageList.ElementAt(j).fileLocation);
                                    findings.Add(sumAvgDiff.ToString());
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
            using (FileStream fs = new FileStream(leftFile, FileMode.Open))
            {
                using (var ms = new MemoryStream())
                {
                    fs.CopyTo(ms);
                    using (FileStream fs2 = new FileStream(rightFile, FileMode.Open))
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

        public static void writeMatchesToFile()
        {
            using (StreamWriter sr = new StreamWriter("listofmatches.txt", true))
            {
                for (int i = 0; i < findings.Count; i += 3)
                {
                    sr.WriteLine("{0}", findings.ElementAt(i + 0));
                    sr.WriteLine("{0}", findings.ElementAt(i + 1));
                    sr.WriteLine("Match: {0}", findings.ElementAt(i + 2));
                    sr.WriteLine();
                }
            }
        }
    }
}
