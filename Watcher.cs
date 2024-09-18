using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics; 

namespace MD_Watcher
{
    public partial class WatcherUI : Form
    {
        /// <summary>
        /// Is COMSniffer service at this computer
        /// </summary>
        private bool COMSnifferFound = false;
        /// <summary>
        /// Quantity of log MD
        /// </summary>
        private int LogQuantity = 0;

        
        public WatcherUI()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {          
            //Today log by default
            SetTodayLog(); 

            //status
            ReceiveStat();

            //count quantity of logs
            LogQuantity = SetLogQuantity(); 
            
        }

        private void btnCheckAllLogs_Click(object sender, EventArgs e)
        {

            if (MessageBox.Show("Check of all logs will be long. \r\n" +
                "Problems will be detected only for logs with sugnificent corruptions\r\nAgree?", "Warning",
                 MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                return; 
            
            if (saveFile.ShowDialog() == DialogResult.Cancel)
                return;


            string PathName = txtPath.Text.Replace("\\", "\\\\");
            string Res = "";
            string[] Fil = Directory.GetFiles(PathName);
            string[] sr;
            int N = 0;
            int AvgN = 0;

            for (int j = 0; j < Fil.Length; j++)
            {
                sr = File.ReadAllLines(Fil[j]);

                AvgN = 0;

                for (int i = 0; i < sr.Length; i++)
                {
                    sr[i] = sr[i].Replace("\0", "");
                    //Res += "Line` '" + sr[i] + "', blankspaces totally ";
                    N = sr[i].Length - sr[i].Replace(" ", "").Length;
                    AvgN += N;
                    //Res += N.ToString() + "\r\n";
                }

                AvgN = AvgN / sr.Length;

                Res = j.ToString() + " " + Fil[j] + " average quantity of blankspaces for lines " + AvgN.ToString();
                
                if (Math.Abs(AvgN - 70) > 10)
                    Res += " corruption detected ";

                Res += "\r\n";

                File.AppendAllText(saveFile.FileName , Res);

            }
        }

        private void btnSelectPath_Click(object sender, EventArgs e)
        {
            if (fldSelectPath.ShowDialog() == DialogResult.OK)
                txtPath.Text = fldSelectPath.SelectedPath;   
        }

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            openFile.InitialDirectory = txtPath.Text;   
            if (openFile.ShowDialog() == DialogResult.OK)
            {
                txtSelectFile.Text = openFile.FileName;    
            }
        }

        private void btnCheckFile_Click(object sender, EventArgs e)
        {
            int ErrorCount = 0;
            bool ShowMessages = true; 
            string[] sr;
            int N;
            sr = File.ReadAllLines(txtSelectFile.Text.Replace("\\", "\\\\"));
            for (int i = 0; i < sr.Length; i++)
            {
                N = sr[i].Length - sr[i].Replace(" ", "").Length;
                if ((i > 1) && (i < sr.Length - 2))
                {
                    if (Math.Abs(N - 70) > 15)
                    {
                        ErrorCount += 1;

                        if (ShowMessages)
                        {

                            if (MessageBox.Show("Press 'Yes' to stop output of messages about errors at this file\r\n" +
                                sr[i - 2].Replace("\0", "") + "\r\n" + sr[i - 1].Replace("\0", "") + "\r\n" + sr[i].Replace("\0", "") +
                                    "r\n" + sr[i + 1].Replace("\0", "") + "r\n" + sr[i + 2].Replace("\0", "") + "\r\n",
                                "ОCorruption detected", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
                            {
                                ShowMessages = false; 
                            }
                        }
                    }
                }
            }

            if (ErrorCount == 0)
            {
                MessageBox.Show("Corruptions not found", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Totally " + ErrorCount.ToString() + " corruptions found", "Corruptions detected",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);         
            }
        }

        private void btnNotepad_Click(object sender, EventArgs e)
        {
            Process.Start(txtSelectFile.Text);    
        }

        /// <summary>
        /// Get status of COMSniffer service
        /// </summary>
        private void ReceiveStat()
        {
            try
            {
                COMSnifferFound = true; 
                lblStat.Text = "COMSniffer status = " + services.Status.ToString();
            }
            catch
            {
                COMSnifferFound = false; 
                lblStat.Text = "COMSniffer not found"; 
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            int ErrorCount = 0;
            string[] sr;
            int N;
            int BeginN = 0;
            int NewLogQuantity = SetLogQuantity();
            string NewFile = DetermineCurrentLog();
 
            sr = File.ReadAllLines(txtSelectFile.Text.Replace("\\", "\\\\"));

            if (sr.Length > 20)
            {
                BeginN = sr.Length - 20;
            }

            for (int i = BeginN; i < sr.Length; i++)
            {
                N = sr[i].Length - sr[i].Replace(" ", "").Length;
                if ((i > 1) && (i < sr.Length - 2))
                {
                    if (Math.Abs(N - 70) > 15)
                    {
                        ErrorCount += 1;
                    }
                }
            }

            ReceiveStat(); 

            File.AppendAllText(txtLog.Text.Replace("\\", "\\\\"), "Control of log MD " + txtSelectFile.Text + " в " + 
                DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + " : " +
                "corruptions " + ErrorCount.ToString() + ". \r\n" + lblStat.Text + "\r\n" + 
                "Totally logs MD " + LogQuantity.ToString() + "\r\n");            

            if ((ErrorCount > Int32.Parse(txtErrorCount.Text)) && COMSnifferFound) 
            {
                File.AppendAllText(txtLog.Text.Replace("\\", "\\\\"), "Reboot of COMSniffer\r\n");
                services.Stop();
                services.Start();  
            }            

            if (LogQuantity < NewLogQuantity)
            {
                File.AppendAllText(txtLog.Text.Replace("\\", "\\\\"), "Changing of quantity of files detected\r\n");                
                LogQuantity = NewLogQuantity; 
            }

            if (txtPath.Text + NewFile != txtSelectFile.Text)
            {
                File.AppendAllText(txtLog.Text.Replace("\\", "\\\\"), "Changing of name of log\r\n");  
                txtSelectFile.Text = txtPath.Text + NewFile;
            }
        }

        
        /// <summary>
        /// Choose today log
        /// </summary>
        private void SetTodayLog()
        {           
            txtSelectFile.Text =  txtPath.Text//.Replace("\\", "\\\\") 
                + "MDSPB_";

            if (DateTime.Today.Day < 10)
                txtSelectFile.Text += "0" + DateTime.Today.Day.ToString();
            else
                txtSelectFile.Text += DateTime.Today.Day.ToString();

            if (DateTime.Today.Month < 10)
                txtSelectFile.Text += ".0" + DateTime.Today.Month.ToString() + ".";
            else
                txtSelectFile.Text += "." + DateTime.Today.Month.ToString() + ".";

            txtSelectFile.Text += DateTime.Today.Year.ToString() + ".log";
        }

        /// <summary>
        /// Define quantity of logs
        /// </summary>
        private int SetLogQuantity()
        {
            int Ret = 0;

            try
            {
                Ret = Directory.GetFiles(txtPath.Text.Replace("\\", "\\\\")).Length;   
            }
            catch { }

            return Ret;
        }

        /// <summary>
        /// Find current log by max date
        /// </summary>
        /// <returns></returns>
        private string DetermineCurrentLog()
        {
            string PathName = txtPath.Text.Replace("\\", "\\\\");
            string[] Fil = Directory.GetFiles(PathName);
            DateTime[] dt = new DateTime[Fil.Length];
            DateTime MaxDt = DateTime.MinValue;
            string Ret = "";

            for (int i = 0; i < Fil.Length; i++)
            {
                Fil[i] = Fil[i].Replace(PathName, "");
                Fil[i] = Fil[i].Replace("MDSPB_", "");
                Fil[i] = Fil[i].Replace(".log", "");
                try
                {
                    dt[i] = DateTime.Parse(Fil[i]);
                }
                catch { }
            }

            for (int i = 0; i < dt.Length; i++)
            {
                if (MaxDt < dt[i])
                {
                    MaxDt = dt[i];
                }
            }

            Ret = "MDSPB_";

            if (MaxDt.Day < 10)
                Ret += "0" + MaxDt.Day.ToString() + ".";
            else
                Ret += MaxDt.Day.ToString() + ".";

            if (MaxDt.Month < 10)
                Ret += "0" + MaxDt.Month.ToString() + ".";
            else
                Ret += MaxDt.Month.ToString() + ".";

            Ret += MaxDt.Year.ToString() + ".log";

            return Ret; 
         
        }
    }
}
