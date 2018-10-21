using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoSorting
{
    class Program
    {
        static void Main(string[] args)
        {
            PhotoSorting photoSorting = new PhotoSorting();
            Console.WriteLine("I am done");
            Console.ReadLine();
        }
    }

    public class PhotoSorting
    {
        string m_InputPath = @"E:\InputPhotos\Input 2\ ";
        string m_OutputPath = @"E:\Photos\Organised\";
        long m_lTotalFileCount = 0;
        long m_lFileCounter = 0;
        long m_lDisplayCounter = 5;

        public PhotoSorting()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            CopyFilesParallel(m_OutputPath, m_InputPath);
            //CopyFiles(m_OutputPath, m_InputPath);

            watch.Stop();
            TimeSpan t = TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds);
            string ElapsedTime = string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms",
                                    t.Hours,
                                    t.Minutes,
                                    t.Seconds,
                                    t.Milliseconds);
            Console.WriteLine(ElapsedTime);
        }

        private List<String> DirSearch(string sDir)
        {
            List<String> files = new List<String>();

            foreach (string f in Directory.GetFiles(sDir))
            {
                files.Add(f);
            }
            foreach (string d in Directory.GetDirectories(sDir))
            {
                files.AddRange(DirSearch(d));
            }
            m_lTotalFileCount = files.LongCount();
            //Console.WriteLine("In Total " + files.LongCount().ToString() + " files to be processed.");

            return files;
        }

        public void CopyFiles(string targetPath, string sourcePath)
        {
            foreach (string f in DirSearch(sourcePath))
            {
                FileInfo objInfo = new FileInfo(f);

                string DynamicDatePath = DynamicTargetPath(objInfo.LastWriteTime);

                string sTargetPath = System.IO.Path.Combine(targetPath, DynamicDatePath);

                if (!System.IO.Directory.Exists(sTargetPath))
                {
                    System.IO.Directory.CreateDirectory(sTargetPath);
                }
                string sTargetFilePath = System.IO.Path.Combine(sTargetPath, objInfo.Name);

                // To copy a file to another location and 
                // overwrite the destination file if it already exists.
                //System.IO.File.Copy(objInfo.FullName, sTargetPath, true);
                FileCopy(objInfo.FullName, sTargetFilePath, sTargetPath, objInfo.Name);
            }
        }

        public void CopyFilesParallel(string targetPath, string sourcePath)
        {
            //long iCount = 0;
            //long iPrintCount = 200;
            Parallel.ForEach(DirSearch(sourcePath), (f) =>
                                {
                                    FileInfo objInfo = new FileInfo(f);

                                    string DynamicDatePath = DynamicTargetPath(objInfo.LastWriteTime);

                                    string sTargetPath = System.IO.Path.Combine(targetPath, DynamicDatePath);

                                    if (!System.IO.Directory.Exists(sTargetPath))
                                    {
                                        System.IO.Directory.CreateDirectory(sTargetPath);
                                    }
                                    string sTargetFilePath = System.IO.Path.Combine(sTargetPath, objInfo.Name);

                                    // To copy a file to another location and 
                                    // overwrite the destination file if it already exists.
                                    //System.IO.File.Copy(objInfo.FullName, sTargetPath, true);
                                    

                                    lock (this)
                                    {
                                        FileCopy(objInfo.FullName, sTargetFilePath, sTargetPath, objInfo.Name);

                                        if (m_lFileCounter > m_lDisplayCounter)
                                        {
                                            Console.WriteLine("Processed " + m_lDisplayCounter.ToString() + " files out of " + m_lTotalFileCount.ToString() + " files.");

                                            m_lDisplayCounter = m_lDisplayCounter + m_lDisplayCounter;
                                        }

                                        Interlocked.Increment(ref m_lFileCounter);

                                        //m_lFileCounter = m_lFileCounter++;
                                    }
                                });
        }

        public string DynamicTargetPath(DateTime dtDateTime)
        {
            string result = dtDateTime.Year.ToString() + @"\" + dtDateTime.Month.ToString() + "-" + CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(dtDateTime.Month);
            return result;
        }

        public void FileCopy(string sSource, string sDest, string destinationPath, string sFileName)
        {
            if (File.Exists(sDest))
            {
                //lock (this)
                //{
                    FileInfo sSourceFI = new FileInfo(sSource);
                    FileInfo sDestFI = new FileInfo(sDest);

                    if (!FilesAreEqual_Hash(sSourceFI, sDestFI))
                    {
                        var existingFiles = Directory.GetFiles(destinationPath);
                        var fileNum = existingFiles.Count(x => x.Contains(Path.GetFileNameWithoutExtension(sFileName)));
                        sDest = Path.Combine(destinationPath, Path.GetFileNameWithoutExtension(sDest) + " copy" + (fileNum > 1 ? " (" + (fileNum - 1) + ")" : "") + Path.GetExtension(sDest));
                        File.Copy(sSource, sDest);
                    }
                //}                
            }
            else
            {
                File.Copy(sSource, sDest);
            }
        }

        static bool FilesAreEqual_Hash(FileInfo first, FileInfo second)
        {
            byte[] firstHash = MD5.Create().ComputeHash(first.OpenRead());
            byte[] secondHash = MD5.Create().ComputeHash(second.OpenRead());

            for (int i = 0; i < firstHash.Length; i++)
            {
                if (firstHash[i] != secondHash[i])
                    return false;
            }
            return true;
        }

    }
}
