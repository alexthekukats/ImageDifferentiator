using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDifferentiator
{
    [Serializable]
    public class ImgDataType
    {
        public string fileLocation { get; }
        public int picWidth { get; }
        public int picHeight { get; }
        public string extension { get; }
        public long fileSize { get; }
        public double ratio { get; }
        public const string ExportLocation = @"megumin_list.dat";
        public bool markedForDeletion { get; set; }
        public bool tested { get; set; }

        public ImgDataType(string loc, int width, int height, string ext, long fsize)
        {
            fileLocation = loc;
            picWidth = width;
            picHeight = height;
            ratio = (double)width / (double)height;
            extension = ext;
            fsize = fileSize;
            markedForDeletion = false;
            tested = false;
        }

        public static List<ImgDataType> Load()
        {
            using (Stream stream = File.Open(ExportLocation, FileMode.Open))
            {
                var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                return (List<ImgDataType>)bformatter.Deserialize(stream);
            }
        }

        public static void Add(string file)
        {
            //imageList.Add(new ImgDataType(file, leftImg.Width, leftImg.Height, ext, new FileInfo(file).Length));
        }

        public static void Save(List<ImgDataType> file, string fileName = ExportLocation)
        {
            Backup(file);
            if (File.Exists(ExportLocation))
            {
                File.Delete(ExportLocation);
            }

            using (Stream stream = File.Open(ExportLocation, FileMode.Create))
            {
                var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                bformatter.Serialize(stream, file);
            }
        }

        public static int DeleteRedundant(List<ImgDataType> imageList)
        {
            int deletedCount = 0;

            foreach (ImgDataType img in imageList)
            {
                //img.tested = true;
                if (img.markedForDeletion)
                {
                    if (File.Exists(img.fileLocation))
                    {
                        File.Delete(img.fileLocation);
                        deletedCount++;
                    }
                    imageList.Remove(img);
                }
            }
            return deletedCount;
        }

        public static void Backup(List<ImgDataType> file)
        {
            if (File.Exists(ExportLocation))
            {
                if (File.Exists(ExportLocation + ".backup"))
                {
                    File.Delete(ExportLocation + ".backup");
                }
                File.Copy(ExportLocation, ExportLocation + ".backup");
            }
        }
    }
}
