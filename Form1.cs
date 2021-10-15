using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace UpdateImageMgr
{
    public partial class Form1 : Form
    {        
        int fCount;
        int totalfile = 0;

        string srcDirName = @"\\daps.seoul.co.kr\PatchImageMgr";
        string destDirName = @"C:\이미지배정기";

        // ini 파일 쓰기
        [DllImport("kernel32")]
        public static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct NETRESOURCE
        {
            public uint dwScope;
            public uint dwType;
            public uint dwDisplayType;
            public uint dwUsage;
            public string lpLocalName;
            public string lpRemoteName;
            public string lpComment;
            public string lpProvider;
        }
        [DllImport("mpr.dll", CharSet = CharSet.Auto)]
        public static extern int WNetUseConnection(IntPtr hwndOwner, [MarshalAs(UnmanagedType.Struct)] ref NETRESOURCE lpNetResource, string lpPassword, string lpUserID, uint dwFlags, StringBuilder lpAccessName, ref int lpBufferSize, out uint lpResult);

        public Form1()
        {
            InitializeComponent();
            
            Delay(2000);
            
            try
            {
                start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "확인", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
        }

        private void start()
        {
            int res = netUse();

            if (res != 0 && res != 1219)
            {
                MessageBox.Show(this, "DAPS 서버에 연결할 수 없습니다. 에러코드: " + res.ToString(), "확인", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            else
            {
                foreach (FileInfo file in new DirectoryInfo(srcDirName).GetFiles())
                {
                    if (file.Extension.Equals(".exe"))
                    {
                        try
                        {
                            foreach (Process p in Process.GetProcessesByName(file.Name.Split('.')[0]))
                            {
                                if (p.Id != Process.GetCurrentProcess().Id)
                                    p.Kill();
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(this, ex.ToString(), "확인", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            return;
                        }
                    }
                }

                fCount = Directory.GetFiles(srcDirName, "*", SearchOption.AllDirectories).Length;
                progressBar1.Maximum = fCount;

                if (fCount > 0)
                    backgroundWorker1.RunWorkerAsync();
            }
        }

        private int netUse()
        {
            int capacity = 64;
            uint resultFlags = 0;
            StringBuilder sb = new StringBuilder(capacity);
            NETRESOURCE ns = new NETRESOURCE();

            ns.dwType = 1;           // 공유 디스크
            ns.lpLocalName = null;   // 로컬 드라이브
            ns.lpRemoteName = srcDirName;
            ns.lpProvider = null;

            return WNetUseConnection(IntPtr.Zero, ref ns, "!!updateuser@@", "daps\\updateuser", 0, sb, ref capacity, out resultFlags);
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            DirectoryCopy(srcDirName, destDirName, true);
        }

        private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            // If the source directory does not exist, throw an exception.
            if (!dir.Exists)
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);

            // If the destination directory does not exist, create it.
            if (!Directory.Exists(destDirName))
                Directory.CreateDirectory(destDirName);

            // Get the file contents of the directory to copy.
            foreach (FileInfo file in dir.GetFiles())
            {
                // Copy the file.
                if (file.Name != "ImageMgr.ini" && file.Name != "ImageMgr_Ver.txt")
                    file.CopyTo(Path.Combine(destDirName, file.Name), true);

                totalfile++;

                progressBar1.Invoke((MethodInvoker)delegate
                {
                    // Running on the UI thread
                    progressBar1.Value = totalfile;
                                        
                    if (totalfile == fCount)    // 파일 다운 완료 시
                    {
                        Delay(1500);
                        Application.Exit();
                    }                        
                });
            }

            // If copySubDirs is true, copy the subdirectories.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dir.GetDirectories())
                {
                    // Copy the subdirectories.
                    DirectoryCopy(subdir.FullName, Path.Combine(destDirName, subdir.Name), copySubDirs);
                }
            }

            // 현재 버전 쓰기
            WritePrivateProfileString("VERSION", "VER", File.ReadAllText(srcDirName + @"\ImageMgr_Ver.txt"), destDirName + @"\ImageMgr.ini");
        }

        private DateTime Delay(int ms)
        {
            DateTime dateTimeNow = DateTime.Now;
            TimeSpan duration = new TimeSpan(0, 0, 0, 0, ms);
            DateTime dateTimeAdd = dateTimeNow.Add(duration);

            while (dateTimeAdd >= dateTimeNow)
            {
                Application.DoEvents();
                dateTimeNow = DateTime.Now;
            }

            return DateTime.Now;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //MessageBox.Show(this, "업데이트 완료.", "확인", MessageBoxButtons.OK);

            //if (File.Exists(destDirName + @"\ImageMgr.exe"))
                Process.Start(destDirName + @"\ImageMgr.exe");
        }
    }
}
