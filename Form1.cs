using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.ComponentModel.Composition;
using Itri.Vmx.Cnc;
using Itri.Vmx.Host;
using Itri.Vmx.Daq;
using System.IO;
using System.Threading;
using System.Diagnostics;
using MySqlConnector;






namespace Vmx_Project2
{
    [Export(typeof(IVmxApp))]
    public partial class Form1 : Form, IVmxApp
    {
        public string AppName => "Vmx Project 2";
        public Image Image => Properties.Resources.icon;
        public Form1()
        {
            InitializeComponent();
        }

        CncAdaptor cnc = null;
        DaqAdaptor daq = null;
        double[] container = null;
        DaqBuffer buffer = null;
        Task monitorTask = null;
        CancellationTokenSource cts = new CancellationTokenSource();
        int monitorDelay = 100;
        bool RunState = false;

        public bool Initialize(IVmxHost host)
        {

            if (host.CncAdaptors.Length == 0 || host.DaqAdaptors.Length == 0)
            {
                return false;
            }

            cnc = host.CncAdaptors[0];
            daq = host.DaqAdaptors[0];

            if (daq.IsStarted == false)
            {
                daq.Start();
            }

            monitorTask = new Task(Monitor, cts.Token);
            monitorTask.Start();

            return true;
        }


        private void DelegateHelper(Control control, string value)
        {
            if (control.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    control.Text = value;
                });
            }
            else
            {
                control.Text = value;
            }
        }

        private void DelegateHelper(ProgressBar control, int value)
        {
            if (control.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    control.Value = value;
                });
            }
            else
            {
                control.Value = value;
            }
        }


        private string MachineMachineryPositsion()
        {
            double[] machinerypositions = new double[0];
            try
            {
                DataItem item = new DataItem();

                item.Path = "/Axes/MachineryPositions";
                int ec = cnc.ReadDataItem(ref item);
                if (ec >= 0)
                {
                    if (item.Value is double[])
                    {
                        machinerypositions = item.Value as double[];
                    }

                }

                DelegateHelper(lbMachX, machinerypositions[0].ToString());
                DelegateHelper(lbMachY, machinerypositions[1].ToString());
                DelegateHelper(lbMachZ, machinerypositions[2].ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return String.Join(" ", machinerypositions);
        }


        private string MachineAxeLoad()
        {
            double[] axesLoadList = new double[0];
            try
            {
                DataItem item = new DataItem();
                item.Path = "/Axes/LoadList";
                int ec = cnc.ReadDataItem(ref item);
                if (ec >= 0)
                {
                    if (item.Value is double[])
                    {
                        axesLoadList = item.Value as double[];
                    }
                }

                DelegateHelper(pgbX, Convert.ToInt32(axesLoadList[0]));
                DelegateHelper(pgbY, Convert.ToInt32(axesLoadList[1]));
                DelegateHelper(pgbZ, Convert.ToInt32(axesLoadList[2]));
                DelegateHelper(lbLoadX, axesLoadList[0].ToString() + "%");
                DelegateHelper(lbLoadY, axesLoadList[1].ToString() + "%");
                DelegateHelper(lbLoadZ, axesLoadList[2].ToString() + "%");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return String.Join(" ", axesLoadList);
        }


        private string MachineSpindleSpeed()
        {
            string spindelspeed = null;
            try
            {

                double[] spindleList = new double[0];
                DataItem item = new DataItem();

                item.Path = "/Spindle/ActualSpeedList";
                int ec = cnc.ReadDataItem(ref item);
                if (ec >= 0)
                {
                    if (item.Value is double[])
                    {
                        spindleList = item.Value as double[];
                        spindelspeed = spindleList[0].ToString();
                    }
                    else
                    {
                        spindelspeed = item.Value.ToString();
                    }

                }
                else
                {
                    item = new DataItem();
                    item.Path = "/Spindle/ActualSpeed";
                    ec = cnc.ReadDataItem(ref item);
                    if (ec >= 0)
                    {
                        spindelspeed = item.Value.ToString();
                    }
                    else
                    {
                        spindelspeed = $"Error code:{ec.ToString()}";
                    }
                }

                DelegateHelper(lbSpindleSpeed, spindelspeed);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return spindelspeed;
        }


        private string MachineFeedrate()
        {
            string machinefeedrate = null;
            try
            {
                DataItem item = new DataItem();

                item.Path = "/controller/feedrate";
                int ec = cnc.ReadDataItem(ref item);
                if (ec >= 0)
                {
                    machinefeedrate = item.Value.ToString();
                }
                else
                {
                    machinefeedrate = $"Error code:{ec.ToString()}";
                }

                DelegateHelper(lbFeedrate, machinefeedrate);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return machinefeedrate;
        }


        private string MachineFeedrateOverride()
        {
            string machinefeedrateovr = null;
            try
            {
                DataItem item = new DataItem();

                item.Path = "/controller/feedrateoverride";
                int ec = cnc.ReadDataItem(ref item);
                if (ec >= 0)
                {
                    machinefeedrateovr = item.Value.ToString();
                }
                else
                {
                    machinefeedrateovr = $"Error code:{ec.ToString()}";
                }

                DelegateHelper(lbFeedrateOvr, machinefeedrateovr);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return machinefeedrateovr;
        }


        private string MachineMode()
        {
            string OperatingMode = null;
            try
            {
                int value = -99;
                DataItem item = new DataItem();
                item.Path = "/controller/operatingmode";
                int ec = cnc.ReadDataItem(ref item);
                if (ec >= 0)
                {
                    value = Convert.ToInt32(item.Value);
                    switch (value)
                    {
                        case 0:
                            OperatingMode = "JOG";
                            break;
                        case 1:
                            OperatingMode = "MDI";
                            break;
                        case 2:
                            OperatingMode = "MEM";
                            break;
                    }
                }
                else
                {
                    OperatingMode = $"Error code:{ec.ToString()}";
                }

                DelegateHelper(lbOpMode, OperatingMode);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return OperatingMode;
        }


        private string MachineStatus()
        {
            string OperatingStatus = null;
            try
            {
                int value = -99;
                DataItem item = new DataItem();
                item.Path = "/controller/operatingstatus";
                int ec = cnc.ReadDataItem(ref item);
                if (ec >= 0)
                {
                    value = Convert.ToInt32(item.Value);
                    if (value == 0)
                    {
                        value = 1;
                    }
                }
                item = new DataItem();
                item.Path = "/controller/alarmstatus";
                ec = cnc.ReadDataItem(ref item);
                if (ec >= 0)
                {
                    bool alarm = Convert.ToBoolean(item.Value);
                    if (alarm)
                    {
                        value = 4;
                    }
                }

                if (value == 1 || value == 3)
                {
                    OperatingStatus = "IDLE";
                }
                else if (value == 2)
                {
                    OperatingStatus = "RUN";
                }
                else if (value == 4)
                {
                    OperatingStatus = "ALARM";
                }
                else
                {
                    OperatingStatus = "OTHER";
                }

                DelegateHelper(lbOpStatus, OperatingStatus);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return OperatingStatus;
        }


        private void Monitor()
        {
            Task.Delay(1000).Wait();
            while (!cts.IsCancellationRequested)
            {
                if (RunState)
                {
                    if (cnc.IsConnected == true)
                    {
                        try
                        {
                            string machinerypos = MachineMachineryPositsion();
                            string spindlespeed = MachineSpindleSpeed();
                            string load = MachineAxeLoad();
                            string feedrateoverride = MachineFeedrateOverride();
                            string feedarte = MachineFeedrate();
                            string machinemode = MachineMode();
                            string machinestatus = MachineStatus();

                            if (Properties.Settings.Default.RecordCheck1 && Properties.Settings.Default.FolderPath != "")
                            {
                                try
                                {
                                    string filePath = Properties.Settings.Default.FolderPath;
                                    if (!Directory.Exists(filePath))
                                    {
                                        Directory.CreateDirectory(filePath);
                                    }
                                    string fileName = $"{filePath}/{DateTime.Now.ToString("yyyy-MM-dd")}.csv";
                                    if (!File.Exists(fileName))
                                    {
                                        string fileHeader = "Mahcine MOdel,Operation Mode,Operation Status,Feed Rate,Spindle Speed,Machinery Position," +
                                            "Axial load,Update Time" + Environment.NewLine;
                                        File.AppendAllText(fileName, fileHeader, Encoding.UTF8);
                                    }
                                    string logTime = DateTime.Now.ToString("HH:mm:ss.fff");
                                    string log = $"{cnc.CncName},{machinestatus},{machinemode},{feedarte},{spindlespeed}," +
                                        $"{machinerypos},{load},{logTime}{Environment.NewLine}";
                                    File.AppendAllText(fileName, log, Encoding.UTF8);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.ToString());
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.ToString());
                        }
                    }
                }
                Task.Delay(monitorDelay).Wait();
            }
        }


        private double RmsValue(double[] rawData)
        {
            try
            {
                Debug.WriteLine(rawData.Length.ToString());

                int square = 0;
                double mean = 0;
                double root = 0;

                for (int i = 0; i < rawData.Length; i++)
                {
                    square += (int)Math.Pow(rawData[i], 2);
                }
                square = Math.Abs(square);
                Debug.WriteLine("square:" + square.ToString());
                mean = (square / (float)rawData.Length);
                Debug.WriteLine("mean:" + mean.ToString());
                root = (float)Math.Sqrt(mean);
                Debug.WriteLine("root:" + root.ToString());
                return root;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return 0.0;
            }
        }


        private void timerDaq_Tick(object sender, EventArgs e)
        {
            double rms = 0;
            timerDaq.Stop();
            try
            {
                buffer.GetAllData(0, container);
                rms = RmsValue(container);

                if (rms == double.NaN)
                {
                    rms = 0.0;
                }

                if (rms.ToString() == "nun Value")
                {
                    Debug.WriteLine(rms.GetType());
                    rms = 0.0;
                }

                if (rms is double == false)
                {
                    rms = 0.0;
                }
                else
                {
                    Math.Round(rms, 3);
                }

                try
                {
                    Debug.WriteLine($"RMS : {rms.ToString()}");
                    chartDaq.Series[0].Points.AddY(rms);
                    if (chartDaq.Series[0].Points.Count > 100)
                    {
                        chartDaq.Series[0].Points.RemoveAt(0);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            timerDaq.Start();
        }



        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            RunState = true;

            if (buffer == null)
            {
                buffer = new DaqBuffer(daq, daq.Settings.SamplingRate);
            }
            if (container == null)
            {
                container = buffer.CreateContainerForBufferData();
            }
            timerDaq.Start();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            RunState = false;
            timerDaq.Stop();
            buffer = null;
            container = null;
        }

        private void btnExportFile_Click(object sender, EventArgs e)
        {
            try
            {
                FolderBrowserDialog browserDialog = new FolderBrowserDialog();
                if (browserDialog.ShowDialog() == DialogResult.OK)
                {
                    string folderPath = browserDialog.SelectedPath;
                    lbExportPath.Text = folderPath;
                    Properties.Settings.Default.FolderPath = folderPath;

                    Properties.Settings.Default.Save
                    ();
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show
                ($"Security error.\n\nError message: {ex.Message}\n\n" +
                     $"Details:\n\n{ex.StackTrace}");
            }
        }

        private void cbExportRecord_CheckedChanged(object sender, EventArgs e)
        {
            bool exportState = false;
            exportState = cbExportRecord.Checked;
            Properties.Settings.Default.RecordCheck1 = exportState;

            Properties.Settings.Default.Save();
        }
    }
}
