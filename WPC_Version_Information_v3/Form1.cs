using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using System.Diagnostics;

namespace WPC_Version_Information
{
    
    public partial class Version_Information : Form
    {
        private float X;
        private float Y;
        private float Max_X;
        private float Max_Y;
        Boolean Max_Windows_Form_flag = false;
        Boolean Form_Load_End_flag = false;
        //List<String> list = new List <string>();
        //Boolean Thread_run_flag = true;
        //Boolean Thread_Test_Flag = false;
        Boolean Action_Button_Flag = false;
        Boolean SerialPort_Connect_Check_Flag = false;
        Boolean Receive_Start_Flag = false;
        Boolean Receive_End_Flag = false;
        Boolean Serial_Enagle_Flag = false;
        Boolean Loader_Version_Flag = false;
        /*
        string Input_MCUFW_Ver_Hbyte;
        string Input_MCUFW_Ver_Lbyte;
        string Input_MCUFW_Release_Date;
        string Input_SoC_Ver;
        string Input_IDT_Release_Date;
        string Input_CAi_Release_Date;
        */
        String Receive_Data = "NO_DATA";
        String Hardware_Ver_Info = "0.0";
        String MCU_FW_Ver_First_Info = "00";
        String MCU_FW_Ver_Second_Info = "00";
        String MCU_Release_Date_Info = "000000";
        String SoC_FW_Ver_First_Info = "00";
        String SoC_FW_Ver_Second_Info = "0000";
        String IDT_Release_Date_Info = "000000";
        String CAi_Modify_Info = "0";
        String CAi_Release_Date_Info = "000000";
        Byte[] Transmit_ACK = new byte[6];// { 65, 79, 75, 13, 10 }; // "AOK/r/n" //
        //String Transmit_ACK = "414F4BDA"; // "AOK/r/n" //
        string[] getcomport;
        int ReadDataByte = 0;
        int Byte_Cnt = 0;
        int Receive_Ver_Cnt = 0;
        int test_pcba_pcs = 0;
        //int cnt = 0;
        //int thread_case = 0;
        string[] MCU_FW_Ver_Data = new string[15];
        string[] SOC_FW_Ver_Data = new string[27];
        string[] HW_Ver_Data = new string[8];
        string filepath = string.Empty;
        string VerDataLine;
        //string Subdata;
        int PRODUCT_SEL = 0;    //0:760B, 1:195D, 2:892 //
        int number_of_ver = 0;
        int number_of_ver_760B = 0;
        int number_of_ver_195D = 0;
        int number_of_ver_892 = 0;
        int number_of_product = 0;
        Product Load_Product = new Product();
        Ver760B Load_760BVersion = new Ver760B();
        Ver195D Load_195DVersion = new Ver195D();
        Ver892 Load_892Version = new Ver892();

       
        
        // ManualResetEvent ShutdownEvent = new ManualResetEvent(false);   // 執行緒初始設定沒有shutdown //
        // ManualResetEvent PauseEvent = new ManualResetEvent(true);       // 執行緒初始設定為pause狀態 //


        private Thread SerialReceive_Thread;    // uart 接收執行緒函式定義 //

        public Version_Information()
        {
            InitializeComponent();

            Max_X = Convert.ToSingle(SystemInformation.PrimaryMonitorSize.Width);   // 取得電腦螢幕Width //
            X =  Convert.ToSingle(this.Width);                                  // 取得App視窗Width //
            //this.Width = SystemInformation.PrimaryMonitorSize.Width;

            Max_Y = Convert.ToSingle(SystemInformation.PrimaryMonitorSize.Height);  // 取得電腦螢幕Height //
            Y = Convert.ToSingle(this.Height);                                   // 取得App視窗Height //
           // this.Height = SystemInformation.PrimaryMonitorSize.Height;
            setTag(this);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            /*
            float newx = (Max_X) / X;// 1203;
            float newy = (Max_Y) / Y;// 1007;
            SetControls(newx, newy, this);
            */
            Transmit_ACK[0] = 65;
            Transmit_ACK[1] = 79;
            Transmit_ACK[2] = 75;
            Transmit_ACK[3] = 48;
            Transmit_ACK[4] = 13;
            Transmit_ACK[5] = 10;


            Ver_comboBox.DropDownStyle = ComboBoxStyle.DropDownList;    //設定comboBox僅可使用下拉式選單選擇項目而不能自行輸入,確保使用者不會有異常資料輸入//
            Product_comboBox.DropDownStyle = ComboBoxStyle.DropDownList;//設定comboBox僅可使用下拉式選單選擇項目而不能自行輸入,確保使用者不會有異常資料輸入//
            Action_Btn.ForeColor = System.Drawing.Color.Green;
            Exit_Btn.ForeColor = System.Drawing.Color.Red;
            Hardware_Ver_label.Text = Hardware_Ver_Info;
            MCU_FW_Ver_label.Text = MCU_FW_Ver_First_Info + "." + MCU_FW_Ver_Second_Info;
            MCU_Release_Date_label.Text = MCU_Release_Date_Info;
            SoC_FW_Ver_label.Text = SoC_FW_Ver_First_Info + "." + SoC_FW_Ver_Second_Info;
            IDT_Release_Date_label.Text = IDT_Release_Date_Info;
            CAi_Modify_Times_label.Text = CAi_Modify_Info;
            Cai_Release_Date_label.Text = CAi_Release_Date_Info;

            Loading_Version_Information_Task(); // 載入版本資訊 //
            /*
            X = Convert.ToSingle(this.Width);                                  // 取得App視窗Width //
            Y = Convert.ToSingle(this.Height);                                 // 取得App視窗Height //
            setTag(this);
            */
            Form_Load_End_flag = true;
            timer1.Enabled = true;
        }
        //-----------------------------------------------------------------------------------------//
        private void setTag(Control cons)
        {
            foreach (Control con in cons.Controls)
            {
                con.Tag = con.Width + ":" + con.Height + ":" + con.Left + ":" + con.Top + ":" + con.Font.Size;
                if (con.Controls.Count > 0)
                {
                    setTag(con);
                }
            }
        }
        private void SetControls(float newx, float newy, Control cons)
        {
            foreach (Control con in cons.Controls)
            {
                if (con.Tag != null)
                {
                    string[] mytag = con.Tag.ToString().Split(new char[] { ':' });
                    con.Width = Convert.ToInt32(System.Convert.ToSingle(mytag[0]) * newx);//寬度
                    con.Height = Convert.ToInt32(System.Convert.ToSingle(mytag[1]) * newy);//高度
                    con.Left = Convert.ToInt32(System.Convert.ToSingle(mytag[2]) * newx);//左邊距
                    con.Top = Convert.ToInt32(System.Convert.ToSingle(mytag[3]) * newy);//頂邊距
                    Single currentSize = System.Convert.ToSingle(mytag[4]) * newy;//字型大小
                    con.Font = new Font(con.Font.Name, currentSize, con.Font.Style, con.Font.Unit);
                    if (con.Controls.Count > 0)
                    {
                        SetControls(newx, newy, con);
                    }
                }

            }
        }
        private void Version_Information_Resize(object sender, EventArgs e)
        {
            if(Form_Load_End_flag==true)
            {
                /*float newx = (this.Width) / X;
                float newy = (this.Height) / Y;
                SetControls(newx, newy, this);
                */
                Max_Windows_Form_flag = !Max_Windows_Form_flag;
                if (Max_Windows_Form_flag == true)
                {
                    float newx = (this.Width) / X;
                    float newy = (this.Height) / Y;
                    SetControls(newx, newy, this);
                }
                else
                {
                    float newx = (this.Width) / X;// 1203;
                    float newy = (this.Height) / Y;// 1007;
                    SetControls(newx, newy, this);
                }
            }
            
            
        }
        //-----------------------------------------------------------------------------------------//
        private void Action_Btn_Click(object sender, EventArgs e)
        {
            /*if(timer1.Enabled==false)
            {
                timer1.Enabled = true;
            }*/
            Action_Button_Flag = !Action_Button_Flag;
            if (Action_Button_Flag == false)
            {
                Serial_Enagle_Flag = false;
                //SerialReceive_Thread.Join();
                SerialPort.Close();
                Action_Btn.Text = "START";
                Action_Btn.ForeColor = System.Drawing.Color.Green;
                SerialPort_Connect_Check_Flag = false;

                Check_Status_label.Text = "---";
                Check_Status_label.ForeColor = System.Drawing.Color.Black;


                Receive_Data = "NO_DATA";
                Hardware_Ver_Info = "0.0";
                MCU_FW_Ver_First_Info = "00";
                MCU_FW_Ver_Second_Info = "00";
                MCU_Release_Date_Info = "000000";
                SoC_FW_Ver_First_Info = "00";
                SoC_FW_Ver_Second_Info = "0000";
                IDT_Release_Date_Info = "000000";
                CAi_Modify_Info = "0";
                CAi_Release_Date_Info = "000000";
                Hardware_Ver_label.Text = Hardware_Ver_Info;
                MCU_FW_Ver_label.Text = MCU_FW_Ver_First_Info + "." + MCU_FW_Ver_Second_Info;
                MCU_Release_Date_label.Text = MCU_Release_Date_Info;
                SoC_FW_Ver_label.Text = SoC_FW_Ver_First_Info + "." + SoC_FW_Ver_Second_Info;
                IDT_Release_Date_label.Text = IDT_Release_Date_Info;
                CAi_Modify_Times_label.Text = CAi_Modify_Info;
                Cai_Release_Date_label.Text = CAi_Release_Date_Info;

                //Input_MCUFW_Ver_Hbyte_textBox.ReadOnly = false;
                //Input_MCUFW_Ver_Lbyte_textBox.ReadOnly = false;
                //Input_MCUFW_Release_Date_textBox.ReadOnly = false;
                //Input_SoC_Ver_textBox.ReadOnly = false;
                //Input_IDT_Release_Date_textBox.ReadOnly = false;
                //Input_CAi_Release_Date_textBox.ReadOnly = false;

                Hardware_Ver_label.ForeColor = System.Drawing.Color.Black;
                MCU_FW_Ver_label.ForeColor = System.Drawing.Color.Black;
                MCU_Release_Date_label.ForeColor = System.Drawing.Color.Black;
                SoC_FW_Ver_label.ForeColor = System.Drawing.Color.Black;
                IDT_Release_Date_label.ForeColor = System.Drawing.Color.Black;
                Cai_Release_Date_label.ForeColor = System.Drawing.Color.Black;

                Product_comboBox.Enabled = true;
                Ver_comboBox.Enabled = true;


                //timer1.Enabled = false;
            }
            else if (Action_Button_Flag == true)
            {
                
                /*if((Input_MCUFW_Ver_Hbyte_textBox.Text==string.Empty)||(Input_MCUFW_Ver_Lbyte_textBox.Text==string.Empty)||
                   (Input_MCUFW_Release_Date_textBox.Text==string.Empty) ||(Input_SoC_Ver_textBox.Text==string.Empty)||
                   (Input_IDT_Release_Date_textBox.Text==string.Empty) ||(Input_CAi_Release_Date_textBox.Text==string.Empty))
                {*/
                if(Loader_Version_Flag==false)
                { 
                    Action_Button_Flag = false;
                    MessageBox.Show("Please Loading Version Information", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    try
                    {
                        getcomport = SerialPort.GetPortNames();
                        SerialPort.PortName = Convert.ToString(getcomport[0]);
                        SerialPort.BaudRate = 9600;
                        SerialPort.DataBits = 8;
                        SerialPort.StopBits = System.IO.Ports.StopBits.One;
                        SerialPort.Parity = System.IO.Ports.Parity.None;
                        SerialPort.Open();

                        Serial_Enagle_Flag = true;

                        SerialReceive_Thread = new Thread(SerialReceive_Task);  //--- Serial接收函式建立 ---//
                        SerialReceive_Thread.IsBackground = true;               //--- 設定Serial為背景作業 ---//
                        SerialReceive_Thread.Start();                           //--- 啟動Serial接收執行緒 ---//


                        Action_Btn.Text = "STOP";
                        Action_Btn.ForeColor = System.Drawing.Color.Orange;
                        SerialPort_Connect_Check_Flag = true;


                        Product_comboBox.Enabled = false;
                        Ver_comboBox.Enabled = false;

                        if(timer1.Enabled==false)
                        {
                            timer1.Enabled = true;
                        }
                        

                        //-----------------------------------------------------------------//
                        //Input_MCUFW_Ver_Hbyte = Input_MCUFW_Ver_Hbyte_textBox.Text;
                        //Input_MCUFW_Ver_Lbyte = Input_MCUFW_Ver_Lbyte_textBox.Text;
                        //Input_MCUFW_Release_Date = Input_MCUFW_Release_Date_textBox.Text;
                        //Input_SoC_Ver = Input_SoC_Ver_textBox.Text;
                        //Input_IDT_Release_Date = Input_IDT_Release_Date_textBox.Text;
                        //Input_CAi_Release_Date = Input_CAi_Release_Date_textBox.Text;
                        //Input_MCUFW_Ver_Hbyte_textBox.ReadOnly = true;
                        //Input_MCUFW_Ver_Lbyte_textBox.ReadOnly = true;
                        //Input_MCUFW_Release_Date_textBox.ReadOnly = true;
                        //Input_SoC_Ver_textBox.ReadOnly = true;
                        //Input_IDT_Release_Date_textBox.ReadOnly = true;
                        //Input_CAi_Release_Date_textBox.ReadOnly = true;
                        //-----------------------------------------------------------------//
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine("{0}", err);
                        MessageBox.Show("Serial Port未正確連接!", "COM連線異常", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        Action_Button_Flag = false;
                        Action_Btn.Text = "START";
                        Action_Btn.ForeColor = System.Drawing.Color.Green;
                        SerialPort_Connect_Check_Flag = false;

                        //timer1.Enabled = false;
                    }
                }
            }
            else { }
        }
        //========================== Uart Receiver 控制程序 ===========================================================//
        private void SerialReceive_Task()
        {
            while(Serial_Enagle_Flag==true)
            {
                try
                {
                    if (SerialPort.IsOpen == true)
                    {
                        if(Receive_End_Flag==false)
                        {
                            ReadDataByte = SerialPort.ReadByte();
                            switch (ReadDataByte)
                            {
                                case 77:    // M //
                                    Receive_Start_Flag = true;
                                    Receive_Data = "MCU";
                                    MCU_FW_Ver_Data[0] = Convert.ToString((char)ReadDataByte);
                                    Byte_Cnt = 1;
                                    break;
                                case 83:    // S //
                                    Receive_Data = "SOC";
                                    SOC_FW_Ver_Data[0] = Convert.ToString((char)ReadDataByte);
                                    Byte_Cnt = 1;
                                    break;
                                case 72:    // H //
                                    Receive_Data = "HW";
                                    HW_Ver_Data[0] = Convert.ToString((char)ReadDataByte);
                                    Byte_Cnt = 1;
                                    break;
                                case 10:
                                    if (Receive_Data == "MCU")
                                    {
                                        MCU_FW_Ver_Data[Byte_Cnt] = Convert.ToString((char)ReadDataByte);
                                        Transmit_ACK[3] = 49; // 1 //
                                        Receive_Ver_Cnt = 1;
                                    }
                                    else if (Receive_Data == "SOC")
                                    {
                                        SOC_FW_Ver_Data[Byte_Cnt] = Convert.ToString((char)ReadDataByte);
                                        Transmit_ACK[3] = 50; // 2 //
                                        Receive_Ver_Cnt = 2;
                                    }
                                    else if (Receive_Data == "HW")
                                    {
                                        HW_Ver_Data[Byte_Cnt] = Convert.ToString((char)ReadDataByte);
                                        Transmit_ACK[3] = 51; // 3 //
                                        Receive_Ver_Cnt = 3;
                                    }
                                    SerialPort.Write(Transmit_ACK, 0, 6); // --- 20210709 --- //
                                    Byte_Cnt = 0;
                                    if (Receive_Ver_Cnt == 3)
                                    {
                                        Receive_Ver_Cnt = 0;
                                        Receive_Start_Flag = false;
                                        Receive_End_Flag = true;
                                        test_pcba_pcs++;
                                    }
                                    break;
                                default:
                                    if (Receive_Start_Flag == true)
                                    {
                                        if (Receive_Data == "MCU")
                                        {
                                            MCU_FW_Ver_Data[Byte_Cnt] = Convert.ToString((char)ReadDataByte);
                                        }
                                        else if (Receive_Data == "SOC")
                                        {
                                            SOC_FW_Ver_Data[Byte_Cnt] = Convert.ToString((char)ReadDataByte);
                                        }
                                        else if (Receive_Data == "HW")
                                        {
                                            HW_Ver_Data[Byte_Cnt] = Convert.ToString((char)ReadDataByte);
                                        }
                                        Byte_Cnt++;
                                    }
                                    break;
                            }
                        }
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine("{0}", err);
                    Receive_End_Flag = false;
                    Serial_Enagle_Flag = false;
                    SerialReceive_Thread.Join();
                }
            }
        }
        //==============================================================================================================================//

        private void timer1_Tick(object sender, EventArgs e)
        {
            Date_toolStripStatusLabel3.Text = DateTimeOffset.Now.ToString("yyyy/MM/dd tt hh:mm");
            if (SerialPort.IsOpen == true)
            {
                //Capture_COMPort_label.Text = "true";
                COM_toolStripStatusLabel.Text = SerialPort.PortName;
                COM_toolStripStatusLabel.ForeColor = System.Drawing.Color.Green;
            }
            else
            {
                if (SerialPort_Connect_Check_Flag == true)
                {
                    timer1.Enabled = false;
                    //Capture_COMPort_label.Text = "false";

                    COM_toolStripStatusLabel.Text = "Disconnect";
                    COM_toolStripStatusLabel.ForeColor = System.Drawing.Color.Red;

                    MessageBox.Show("Serial Port未正確連接!", "COM連線異常", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Action_Button_Flag = false;
                    Action_Btn.Text = "START";
                    Action_Btn.ForeColor = System.Drawing.Color.Green;
                    SerialPort_Connect_Check_Flag = false;

                    SerialPort.Close();
                    //SerialReceive_Thread.Join();                        //--- 啟動Serial接收執行緒 ---//



                }
                else
                {
                    //Capture_COMPort_label.Text = "false";
                    COM_toolStripStatusLabel.Text = "Disconnect";
                    COM_toolStripStatusLabel.ForeColor = System.Drawing.Color.Red;
                }
            }
            Data_Process_Task();

            /*if (Receive_End_Flag == true)
            {
                Receive_End_Flag = false;
                Data_Process_Task();
                //SerialPort.Write(Transmit_ACK, 0, 5);
                //test_pcba_pcs++;
                //read_pcba_pcs_label.Text = Convert.ToString(test_pcba_pcs);
            }*/
        }
        //=================================================================================================================================//
        private void Loading_Version_Information_Task()
        {
            var keyword = string.Empty;
            var substr = new StringBuilder("");
            var i = 0;
            var ver_group = 0;
            string fullpath;
            try
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    fullpath = System.Environment.CurrentDirectory; // 取得當下路徑目錄位置 //
                    openFileDialog.InitialDirectory = fullpath;     // 取得檔案所在的目錄 //
                    openFileDialog.Filter = "ini files (*.ini)|*.ini";
                    openFileDialog.FilterIndex = 1;
                    openFileDialog.RestoreDirectory = true;
                    openFileDialog.FileName = fullpath + "\\WPCGT01-version_info.ini";
                    filepath = openFileDialog.FileName;
                    StreamReader str = new StreamReader(@filepath);
                    while (str.EndOfStream != true)//------------------------------------------20210715 增加while loop----------------//
                    {
                        VerDataLine = str.ReadLine();
                        for (i = 0; i < VerDataLine.Length; i++)
                        {
                            keyword = VerDataLine.Substring(i, 1);
                            if (keyword != "-")
                            {
                                substr.Append(keyword);
                            }
                            else
                            {
                                if(Convert.ToString(substr)!= "*")
                                {
                                    switch (ver_group)
                                    {
                                        case 0:
                                            Load_Product.Product_Select.Add(number_of_product.ToString());
                                            Load_Product.Product_Select[number_of_product] = Convert.ToString(substr);
                                            ver_group = 1;
                                            break;
                                        case 1:
                                            if (number_of_product == 0)
                                            {
                                                Load_760BVersion.HW_Ver.Add(number_of_ver.ToString());
                                                Load_760BVersion.HW_Ver[number_of_ver] = Convert.ToString(substr);
                                            }
                                            else if (number_of_product == 1)
                                            {
                                                Load_195DVersion.HW_Ver.Add(number_of_ver.ToString());
                                                Load_195DVersion.HW_Ver[number_of_ver] = Convert.ToString(substr);
                                            }
                                            else if (number_of_product == 2)
                                            {
                                                Load_892Version.HW_Ver.Add(number_of_ver.ToString());
                                                Load_892Version.HW_Ver[number_of_ver] = Convert.ToString(substr);
                                            }
                                            else { }
                                            ver_group = 2;
                                            break;
                                        case 2:
                                            if (number_of_product == 0)
                                            {
                                                Load_760BVersion.MCU_Ver.Add(number_of_ver.ToString());
                                                Load_760BVersion.MCU_Ver[number_of_ver] = Convert.ToString(substr);
                                            }
                                            else if (number_of_product == 1)
                                            {
                                                Load_195DVersion.MCU_Ver.Add(number_of_ver.ToString());
                                                Load_195DVersion.MCU_Ver[number_of_ver] = Convert.ToString(substr);
                                            }
                                            else if (number_of_product == 2)
                                            {
                                                Load_892Version.MCU_Ver.Add(number_of_ver.ToString());
                                                Load_892Version.MCU_Ver[number_of_ver] = Convert.ToString(substr);
                                            }
                                            else { }
                                            ver_group = 3;
                                            break;
                                        case 3:
                                            if (number_of_product == 0)
                                            {
                                                Load_760BVersion.SOC_Ver.Add(number_of_ver.ToString());
                                                Load_760BVersion.SOC_Ver[number_of_ver] = Convert.ToString(substr);
                                            }
                                            else if (number_of_product == 1)
                                            {
                                                Load_195DVersion.SOC_Ver.Add(number_of_ver.ToString());
                                                Load_195DVersion.SOC_Ver[number_of_ver] = Convert.ToString(substr);
                                            }
                                            else if (number_of_product == 2)
                                            {
                                                Load_892Version.SOC_Ver.Add(number_of_ver.ToString());
                                                Load_892Version.SOC_Ver[number_of_ver] = Convert.ToString(substr);
                                            }
                                            else { }
                                            ver_group = 4;
                                            break;
                                        case 4:
                                            if (number_of_product == 0)
                                            {
                                                Load_760BVersion.MCU_Release_Date.Add(number_of_ver.ToString());
                                                Load_760BVersion.MCU_Release_Date[number_of_ver] = Convert.ToString(substr);
                                            }
                                            else if (number_of_product == 1)
                                            {
                                                Load_195DVersion.MCU_Release_Date.Add(number_of_ver.ToString());
                                                Load_195DVersion.MCU_Release_Date[number_of_ver] = Convert.ToString(substr);
                                            }
                                            else if (number_of_product == 2)
                                            {
                                                Load_892Version.MCU_Release_Date.Add(number_of_ver.ToString());
                                                Load_892Version.MCU_Release_Date[number_of_ver] = Convert.ToString(substr);
                                            }
                                            else { }
                                            ver_group = 5;
                                            break;
                                        case 5:
                                            if (number_of_product == 0)
                                            {
                                                Load_760BVersion.SOC_Release_Date.Add(number_of_ver.ToString());
                                                Load_760BVersion.SOC_Release_Date[number_of_ver] = Convert.ToString(substr);
                                            }
                                            else if (number_of_product == 1)
                                            {
                                                Load_195DVersion.SOC_Release_Date.Add(number_of_ver.ToString());
                                                Load_195DVersion.SOC_Release_Date[number_of_ver] = Convert.ToString(substr);
                                            }
                                            else if (number_of_product == 2)
                                            {
                                                Load_892Version.SOC_Release_Date.Add(number_of_ver.ToString());
                                                Load_892Version.SOC_Release_Date[number_of_ver] = Convert.ToString(substr);
                                            }
                                            else { }
                                            ver_group = 6;
                                            break;
                                        case 6:
                                            if (number_of_product == 0)
                                            {
                                                Load_760BVersion.SOC_Cus_Release_Date.Add(number_of_ver.ToString());
                                                Load_760BVersion.SOC_Cus_Release_Date[number_of_ver] = Convert.ToString(substr);
                                            }
                                            else if (number_of_product == 1)
                                            {
                                                Load_195DVersion.SOC_Cus_Release_Date.Add(number_of_ver.ToString());
                                                Load_195DVersion.SOC_Cus_Release_Date[number_of_ver] = Convert.ToString(substr);
                                            }
                                            else if (number_of_product == 2)
                                            {
                                                Load_892Version.SOC_Cus_Release_Date.Add(number_of_ver.ToString());
                                                Load_892Version.SOC_Cus_Release_Date[number_of_ver] = Convert.ToString(substr);
                                            }
                                            else { }
                                            ver_group = 1;
                                            number_of_ver++;
                                            break;
                                        default:
                                            break;
                                    }
                                    keyword = string.Empty;
                                    substr = new StringBuilder("");
                                }
                                else
                                {
                                    ver_group = 0;
                                    if(number_of_product == 0)
                                    {
                                        number_of_ver_760B = number_of_ver;
                                    }
                                    else if(number_of_product == 1)
                                    {
                                        number_of_ver_195D = number_of_ver;
                                    }
                                    else if (number_of_product == 2)
                                    {
                                        number_of_ver_892 = number_of_ver;
                                    }
                                    number_of_ver = 0;
                                    number_of_product++;
                                    keyword = string.Empty;
                                    substr = new StringBuilder("");
                                }
                            }
                        }
                    }//---------------------------------------------------------------------20210715 增加while loop----------------//
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("{0}", err);
                MessageBox.Show("選擇ini檔案所在位置!", "WPCGT01-version_info.ini - 找不到檔案", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    fullpath = System.Environment.CurrentDirectory; // 取得當下路徑目錄位置 //
                    openFileDialog.InitialDirectory = fullpath;     // 取得檔案所在的目錄 //
                    openFileDialog.Filter = "ini files (*.ini)|*.ini";
                    openFileDialog.FilterIndex = 1;
                    openFileDialog.RestoreDirectory = true;
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        filepath = openFileDialog.FileName;         // Get the path of specified file //
                        StreamReader str = new StreamReader(@filepath);
                        while (str.EndOfStream != true)//------------------------------------------20210715 增加while loop----------------//
                        {
                            VerDataLine = str.ReadLine();
                            for (i = 0; i < VerDataLine.Length; i++)
                            {
                                keyword = VerDataLine.Substring(i, 1);
                                if (keyword != "-")
                                {
                                    substr.Append(keyword);
                                }
                                else
                                {
                                    if (Convert.ToString(substr) != "*")
                                    {
                                        switch (ver_group)
                                        {
                                            case 0:
                                                Load_Product.Product_Select.Add(number_of_product.ToString());
                                                Load_Product.Product_Select[number_of_product] = Convert.ToString(substr);
                                                ver_group = 1;
                                                break;
                                            case 1:
                                                if (number_of_product == 0)
                                                {
                                                    Load_760BVersion.HW_Ver.Add(number_of_ver.ToString());
                                                    Load_760BVersion.HW_Ver[number_of_ver] = Convert.ToString(substr);
                                                }
                                                else if (number_of_product == 1)
                                                {
                                                    Load_195DVersion.HW_Ver.Add(number_of_ver.ToString());
                                                    Load_195DVersion.HW_Ver[number_of_ver] = Convert.ToString(substr);
                                                }
                                                else if (number_of_product == 2)
                                                {
                                                    Load_892Version.HW_Ver.Add(number_of_ver.ToString());
                                                    Load_892Version.HW_Ver[number_of_ver] = Convert.ToString(substr);
                                                }
                                                else { }
                                                ver_group = 2;
                                                break;
                                            case 2:
                                                if (number_of_product == 0)
                                                {
                                                    Load_760BVersion.MCU_Ver.Add(number_of_ver.ToString());
                                                    Load_760BVersion.MCU_Ver[number_of_ver] = Convert.ToString(substr);
                                                }
                                                else if (number_of_product == 1)
                                                {
                                                    Load_195DVersion.MCU_Ver.Add(number_of_ver.ToString());
                                                    Load_195DVersion.MCU_Ver[number_of_ver] = Convert.ToString(substr);
                                                }
                                                else if (number_of_product == 2)
                                                {
                                                    Load_892Version.MCU_Ver.Add(number_of_ver.ToString());
                                                    Load_892Version.MCU_Ver[number_of_ver] = Convert.ToString(substr);
                                                }
                                                else { }
                                                ver_group = 3;
                                                break;
                                            case 3:
                                                if (number_of_product == 0)
                                                {
                                                    Load_760BVersion.SOC_Ver.Add(number_of_ver.ToString());
                                                    Load_760BVersion.SOC_Ver[number_of_ver] = Convert.ToString(substr);
                                                }
                                                else if (number_of_product == 1)
                                                {
                                                    Load_195DVersion.SOC_Ver.Add(number_of_ver.ToString());
                                                    Load_195DVersion.SOC_Ver[number_of_ver] = Convert.ToString(substr);
                                                }
                                                else if (number_of_product == 2)
                                                {
                                                    Load_892Version.SOC_Ver.Add(number_of_ver.ToString());
                                                    Load_892Version.SOC_Ver[number_of_ver] = Convert.ToString(substr);
                                                }
                                                else { }
                                                ver_group = 4;
                                                break;
                                            case 4:
                                                if (number_of_product == 0)
                                                {
                                                    Load_760BVersion.MCU_Release_Date.Add(number_of_ver.ToString());
                                                    Load_760BVersion.MCU_Release_Date[number_of_ver] = Convert.ToString(substr);
                                                }
                                                else if (number_of_product == 1)
                                                {
                                                    Load_195DVersion.MCU_Release_Date.Add(number_of_ver.ToString());
                                                    Load_195DVersion.MCU_Release_Date[number_of_ver] = Convert.ToString(substr);
                                                }
                                                else if (number_of_product == 2)
                                                {
                                                    Load_892Version.MCU_Release_Date.Add(number_of_ver.ToString());
                                                    Load_892Version.MCU_Release_Date[number_of_ver] = Convert.ToString(substr);
                                                }
                                                else { }
                                                ver_group = 5;
                                                break;
                                            case 5:
                                                if (number_of_product == 0)
                                                {
                                                    Load_760BVersion.SOC_Release_Date.Add(number_of_ver.ToString());
                                                    Load_760BVersion.SOC_Release_Date[number_of_ver] = Convert.ToString(substr);
                                                }
                                                else if (number_of_product == 1)
                                                {
                                                    Load_195DVersion.SOC_Release_Date.Add(number_of_ver.ToString());
                                                    Load_195DVersion.SOC_Release_Date[number_of_ver] = Convert.ToString(substr);
                                                }
                                                else if (number_of_product == 2)
                                                {
                                                    Load_892Version.SOC_Release_Date.Add(number_of_ver.ToString());
                                                    Load_892Version.SOC_Release_Date[number_of_ver] = Convert.ToString(substr);
                                                }
                                                else { }
                                                ver_group = 6;
                                                break;
                                            case 6:
                                                if (number_of_product == 0)
                                                {
                                                    Load_760BVersion.SOC_Cus_Release_Date.Add(number_of_ver.ToString());
                                                    Load_760BVersion.SOC_Cus_Release_Date[number_of_ver] = Convert.ToString(substr);
                                                }
                                                else if (number_of_product == 1)
                                                {
                                                    Load_195DVersion.SOC_Cus_Release_Date.Add(number_of_ver.ToString());
                                                    Load_195DVersion.SOC_Cus_Release_Date[number_of_ver] = Convert.ToString(substr);
                                                }
                                                else if (number_of_product == 2)
                                                {
                                                    Load_892Version.SOC_Cus_Release_Date.Add(number_of_ver.ToString());
                                                    Load_892Version.SOC_Cus_Release_Date[number_of_ver] = Convert.ToString(substr);
                                                }
                                                else { }
                                                ver_group = 1;
                                                number_of_ver++;
                                                break;
                                            default:
                                                break;
                                        }
                                        keyword = string.Empty;
                                        substr = new StringBuilder("");
                                    }
                                    else
                                    {
                                        ver_group = 0;
                                        if (number_of_product == 0)
                                        {
                                            number_of_ver_760B = number_of_ver;
                                        }
                                        else if (number_of_product == 1)
                                        {
                                            number_of_ver_195D = number_of_ver;
                                        }
                                        else if (number_of_product == 2)
                                        {
                                            number_of_ver_892 = number_of_ver;
                                        }
                                        number_of_ver = 0;
                                        number_of_product++;
                                        keyword = string.Empty;
                                        substr = new StringBuilder("");
                                    }
                                }
                            }
                        }//---------------------------------------------------------------------20210715 增加while loop----------------//
                    }
                }
            }
            for (int j = 0; j < number_of_product; j++)
            {
                Product_comboBox.Items.Insert(j, Load_Product.Product_Select[j]);    // 新增下拉式選單選項，使用產品別 作為comboBox下拉式選單選項list //
            }
            /*for (int j = 0; j < number_of_ver; j++)
            {
                Ver_comboBox.Items.Insert(j, Load_760BVersion.MCU_Ver[j]);  // 新增下拉式選單選項，使用 MCU版本 作為comboBox下拉式選單選項list //
            }*/
        }
        //=================================================================================================================================//
        private void Data_Process_Task()
        {
            if (Receive_End_Flag == true)
            {
                if(PRODUCT_SEL==2)
                {
                    if (HW_Ver_Data[2]=="6")    {HW_Ver_Data[2] = "F";}
                    Hardware_Ver_label.Text = HW_Ver_Data[2] + "." + HW_Ver_Data[3];
                }
                else
                {
                    Hardware_Ver_label.Text = HW_Ver_Data[2] + "." + HW_Ver_Data[3];
                }
                
                MCU_FW_Ver_label.Text = (MCU_FW_Ver_Data[2] + MCU_FW_Ver_Data[3]) + "." + (MCU_FW_Ver_Data[4] + MCU_FW_Ver_Data[5]);
                MCU_Release_Date_label.Text = MCU_FW_Ver_Data[7] + MCU_FW_Ver_Data[8] + MCU_FW_Ver_Data[9] + MCU_FW_Ver_Data[10] + MCU_FW_Ver_Data[11] + MCU_FW_Ver_Data[12];
                SoC_FW_Ver_label.Text = SOC_FW_Ver_Data[2] + SOC_FW_Ver_Data[3] + "." + SOC_FW_Ver_Data[4] + SOC_FW_Ver_Data[5] + SOC_FW_Ver_Data[6] + SOC_FW_Ver_Data[7];
                IDT_Release_Date_label.Text = SOC_FW_Ver_Data[9] + SOC_FW_Ver_Data[10] + SOC_FW_Ver_Data[11] + SOC_FW_Ver_Data[12] + SOC_FW_Ver_Data[13] + SOC_FW_Ver_Data[14];
                CAi_Modify_Times_label.Text = SOC_FW_Ver_Data[24];
                Cai_Release_Date_label.Text = SOC_FW_Ver_Data[16] + SOC_FW_Ver_Data[17] + SOC_FW_Ver_Data[18] + SOC_FW_Ver_Data[19] + SOC_FW_Ver_Data[20] + SOC_FW_Ver_Data[21];

                Hardware_Ver_label.ForeColor = System.Drawing.Color.Green;
                MCU_FW_Ver_label.ForeColor = System.Drawing.Color.Green;
                MCU_Release_Date_label.ForeColor = System.Drawing.Color.Green;
                SoC_FW_Ver_label.ForeColor = System.Drawing.Color.Green;
                IDT_Release_Date_label.ForeColor = System.Drawing.Color.Green;
                Cai_Release_Date_label.ForeColor = System.Drawing.Color.Green;

                /*if ((Hardware_Ver_label.Text == Load_HW_Ver_label.Text) && (MCU_FW_Ver_label.Text == Load_MCU_Ver_label.Text) &&
                   (SoC_FW_Ver_label.Text == Load_SOC_Ver_label.Text) && (MCU_Release_Date_label.Text == Load_MCU_Rel_Date_label.Text) &&
                   (IDT_Release_Date_label.Text == Load_SOC_Rel_Date_label.Text) && (Cai_Release_Date_label.Text == Load_SOC_Cus_Rel_Date_label.Text))*/
                if ((MCU_FW_Ver_label.Text == Load_MCU_Ver_label.Text) && (SoC_FW_Ver_label.Text == Load_SOC_Ver_label.Text) && 
                    (MCU_Release_Date_label.Text == Load_MCU_Rel_Date_label.Text) && (IDT_Release_Date_label.Text == Load_SOC_Rel_Date_label.Text) && 
                    (Cai_Release_Date_label.Text == Load_SOC_Cus_Rel_Date_label.Text))
                {
                    Check_Status_label.Text = "OK";
                    Check_Status_label.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    /*if (Hardware_Ver_label.Text != Load_HW_Ver_label.Text)
                    {
                        Hardware_Ver_label.ForeColor = System.Drawing.Color.Red;
                    }*/
                    if (MCU_FW_Ver_label.Text != Load_MCU_Ver_label.Text)
                    {
                        MCU_FW_Ver_label.ForeColor = System.Drawing.Color.Red;
                    }
                    if (MCU_Release_Date_label.Text != Load_MCU_Rel_Date_label.Text)
                    {
                        MCU_Release_Date_label.ForeColor = System.Drawing.Color.Red;
                    }
                    if (SoC_FW_Ver_label.Text != Load_SOC_Ver_label.Text)
                    {
                        SoC_FW_Ver_label.ForeColor = System.Drawing.Color.Red;
                    }
                    if (IDT_Release_Date_label.Text != Load_SOC_Rel_Date_label.Text)
                    {
                        IDT_Release_Date_label.ForeColor = System.Drawing.Color.Red;
                    }
                    if (Cai_Release_Date_label.Text != Load_SOC_Cus_Rel_Date_label.Text)
                    {
                        Cai_Release_Date_label.ForeColor = System.Drawing.Color.Red;
                    }
                    Check_Status_label.Text = "NG";
                    Check_Status_label.ForeColor = System.Drawing.Color.Red;
                }

                read_pcba_pcs_label.Text = Convert.ToString(test_pcba_pcs);

                Receive_End_Flag = false;
            }
                

        }
        
        /*private void Input_MCUFW_Ver_Hbyte_textBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(((int)e.KeyChar<48|(int)e.KeyChar>57) & (int)e.KeyChar!=8)
            {
                e.Handled = true;
            }
        }

        private void Input_MCUFW_Ver_Lbyte_textBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (((int)e.KeyChar < 48 | (int)e.KeyChar > 57) & (int)e.KeyChar != 8)
            {
                e.Handled = true;
            }
        }

        private void Input_MCUFW_Release_Date_textBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (((int)e.KeyChar < 48 | (int)e.KeyChar > 57) & (int)e.KeyChar != 8)
            {
                e.Handled = true;
            }
        }

        private void Input_CAi_Release_Date_textBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (((int)e.KeyChar < 48 | (int)e.KeyChar > 57) & (int)e.KeyChar != 8)
            {
                e.Handled = true;
            }
        }*/


        private void Exit_Btn_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("WPC product Version Information User Interface - V1.3", "About", MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        private void userMenuToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string filepath;

            filepath = System.Environment.CurrentDirectory; // 取得當下路徑目錄位置 //
            ProcessStartInfo open = new ProcessStartInfo();
            open.FileName = "User_Menu.pdf";
            open.WorkingDirectory = @filepath;
            try
            {
                Process.Start(open);
            }
            catch (Exception err)
            {
                Console.WriteLine("{0}", err);
                MessageBox.Show("找不到User_Menu.pdf檔案 !", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            
        }


        private void Ver_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(PRODUCT_SEL==0)
            {
                Load_HW_Ver_label.Text = Load_760BVersion.HW_Ver[Ver_comboBox.SelectedIndex];
                Load_MCU_Ver_label.Text = Load_760BVersion.MCU_Ver[Ver_comboBox.SelectedIndex];
                Load_SOC_Ver_label.Text = Load_760BVersion.SOC_Ver[Ver_comboBox.SelectedIndex];
                Load_MCU_Rel_Date_label.Text = Load_760BVersion.MCU_Release_Date[Ver_comboBox.SelectedIndex];
                Load_SOC_Rel_Date_label.Text = Load_760BVersion.SOC_Release_Date[Ver_comboBox.SelectedIndex];
                Load_SOC_Cus_Rel_Date_label.Text = Load_760BVersion.SOC_Cus_Release_Date[Ver_comboBox.SelectedIndex];
            }
            else if (PRODUCT_SEL == 1)
            {
                Load_HW_Ver_label.Text = Load_195DVersion.HW_Ver[Ver_comboBox.SelectedIndex];
                Load_MCU_Ver_label.Text = Load_195DVersion.MCU_Ver[Ver_comboBox.SelectedIndex];
                Load_SOC_Ver_label.Text = Load_195DVersion.SOC_Ver[Ver_comboBox.SelectedIndex];
                Load_MCU_Rel_Date_label.Text = Load_195DVersion.MCU_Release_Date[Ver_comboBox.SelectedIndex];
                Load_SOC_Rel_Date_label.Text = Load_195DVersion.SOC_Release_Date[Ver_comboBox.SelectedIndex];
                Load_SOC_Cus_Rel_Date_label.Text = Load_195DVersion.SOC_Cus_Release_Date[Ver_comboBox.SelectedIndex];
            }
            else if (PRODUCT_SEL == 2)
            {
                Load_HW_Ver_label.Text = Load_892Version.HW_Ver[Ver_comboBox.SelectedIndex];
                Load_MCU_Ver_label.Text = Load_892Version.MCU_Ver[Ver_comboBox.SelectedIndex];
                Load_SOC_Ver_label.Text = Load_892Version.SOC_Ver[Ver_comboBox.SelectedIndex];
                Load_MCU_Rel_Date_label.Text = Load_892Version.MCU_Release_Date[Ver_comboBox.SelectedIndex];
                Load_SOC_Rel_Date_label.Text = Load_892Version.SOC_Release_Date[Ver_comboBox.SelectedIndex];
                Load_SOC_Cus_Rel_Date_label.Text = Load_892Version.SOC_Cus_Release_Date[Ver_comboBox.SelectedIndex];
            }

            Load_HW_Ver_label.ForeColor = System.Drawing.Color.Green;
            Load_MCU_Ver_label.ForeColor = System.Drawing.Color.Green;
            Load_SOC_Ver_label.ForeColor = System.Drawing.Color.Green;
            Load_MCU_Rel_Date_label.ForeColor = System.Drawing.Color.Green;
            Load_SOC_Rel_Date_label.ForeColor = System.Drawing.Color.Green;
            Load_SOC_Cus_Rel_Date_label.ForeColor = System.Drawing.Color.Green;
            
            Loader_Version_Flag = true;
        }

        private void Product_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Load_HW_Ver_label.Text = "0.0";
            Load_MCU_Ver_label.Text = "00.00";
            Load_SOC_Ver_label.Text = "00.0000";
            Load_MCU_Rel_Date_label.Text = "000000";
            Load_SOC_Rel_Date_label.Text = "000000";
            Load_SOC_Cus_Rel_Date_label.Text = "000000";
            Load_HW_Ver_label.ForeColor = System.Drawing.Color.Black;
            Load_MCU_Ver_label.ForeColor = System.Drawing.Color.Black;
            Load_SOC_Ver_label.ForeColor = System.Drawing.Color.Black;
            Load_MCU_Rel_Date_label.ForeColor = System.Drawing.Color.Black;
            Load_SOC_Rel_Date_label.ForeColor = System.Drawing.Color.Black;
            Load_SOC_Cus_Rel_Date_label.ForeColor = System.Drawing.Color.Black;

            if (Product_comboBox.SelectedIndex==0)
            {
               PRODUCT_SEL = 0;
               Ver_Inf_group.Text = "760B Version Information";
               Ver_comboBox.Items.Clear();
               for (int j = 0; j < number_of_ver_760B; j++)
               {
                    Ver_comboBox.Items.Insert(j, Load_760BVersion.MCU_Ver[j]);  // 新增下拉式選單選項，使用 MCU版本 作為comboBox下拉式選單選項list //
               }
            }
            else if (Product_comboBox.SelectedIndex == 1)
            {
                PRODUCT_SEL = 1;
                Ver_Inf_group.Text = "195D Version Information";
                Ver_comboBox.Items.Clear();
                for (int j = 0; j < number_of_ver_195D; j++)
                {
                    Ver_comboBox.Items.Insert(j, Load_195DVersion.MCU_Ver[j]);  // 新增下拉式選單選項，使用 MCU版本 作為comboBox下拉式選單選項list //
                }
            }
            else if (Product_comboBox.SelectedIndex == 2)
            {
                PRODUCT_SEL = 2;
                Ver_Inf_group.Text = "892 Version Information";
                Ver_comboBox.Items.Clear();
                for (int j = 0; j < number_of_ver_892; j++)
                {
                    Ver_comboBox.Items.Insert(j, Load_892Version.MCU_Ver[j]);  // 新增下拉式選單選項，使用 MCU版本 作為comboBox下拉式選單選項list //
                }
            }
        }
    }
    //=====================================================================================================================================//
    public partial class Ver760B
    {
        public List<String> HW_Ver = new List<String>();                // 宣告HW_Ver 動態字串陣列 //
        public List<String> MCU_Ver = new List<String>();               // 宣告MCU_Ver 動態字串陣列 //
        public List<String> SOC_Ver = new List<String>();               // 宣告SOC_Ver 動態字串陣列 //
        public List<String> MCU_Release_Date = new List<String>();      // 宣告MCU_Release_Date 動態字串陣列 //
        public List<String> SOC_Release_Date = new List<String>();      // 宣告HSOC_Release_Date 動態字串陣列 //
        public List<String> SOC_Cus_Release_Date = new List<String>();  // 宣告SOC_Cus_Release_Date 動態字串陣列 //
    }
    public partial class Ver195D
    {
        public List<String> HW_Ver = new List<String>();                // 宣告HW_Ver 動態字串陣列 //
        public List<String> MCU_Ver = new List<String>();               // 宣告MCU_Ver 動態字串陣列 //
        public List<String> SOC_Ver = new List<String>();               // 宣告SOC_Ver 動態字串陣列 //
        public List<String> MCU_Release_Date = new List<String>();      // 宣告MCU_Release_Date 動態字串陣列 //
        public List<String> SOC_Release_Date = new List<String>();      // 宣告HSOC_Release_Date 動態字串陣列 //
        public List<String> SOC_Cus_Release_Date = new List<String>();  // 宣告SOC_Cus_Release_Date 動態字串陣列 //
    }
    public partial class Ver892
    {
        public List<String> HW_Ver = new List<String>();                // 宣告HW_Ver 動態字串陣列 //
        public List<String> MCU_Ver = new List<String>();               // 宣告MCU_Ver 動態字串陣列 //
        public List<String> SOC_Ver = new List<String>();               // 宣告SOC_Ver 動態字串陣列 //
        public List<String> MCU_Release_Date = new List<String>();      // 宣告MCU_Release_Date 動態字串陣列 //
        public List<String> SOC_Release_Date = new List<String>();      // 宣告HSOC_Release_Date 動態字串陣列 //
        public List<String> SOC_Cus_Release_Date = new List<String>();  // 宣告SOC_Cus_Release_Date 動態字串陣列 //
    }
    public partial class Product
    {
        public List<String> Product_Select = new List<String>();        // 宣告Product_Select 動態字串陣列 //
    }
    //=====================================================================================================================================//
}
