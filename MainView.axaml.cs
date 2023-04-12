using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using EIP_Lib;
using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.Device.Pwm;
using System.Device.Pwm.Drivers;
using System.Collections.ObjectModel;
using Avalonia;
using MessageBox.Avalonia;
using System.Xml;
using System.Collections.Generic;
using System.Xml.Linq;

namespace LeanovationHitachi
{
    public partial class MainView : UserControl
    {
        EIP EIP1 = null;
        EIP EIP2 = null;
        int[] ClassAttr1;
        int[] ClassAttr2;
        AttrData attr1;
        AttrData attr2;
        double value = 0;

        PwmChannel pwmChannel = PwmChannel.Create(0, 0, 1000, 0);

        public MainView()
        {
            InitializeComponent();
            SetPrinterOne();
            SetPrinterTwo();
            LoadXml();
            txtDataSpeed.Text = value.ToString();
            pwmChannel.Start();



        }

        public class Message
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Content { get; set; }
            public string Speed { get; set; }
        }

        private void LoadXml()
        {
            var fullpath = AppDomain.CurrentDomain.BaseDirectory + "Messages/Messages.xml";
            XDocument doc = XDocument.Load(fullpath);

            // Get all the message elements
            IEnumerable<XElement> messages = doc.Descendants("message");

            // Create a list to store the messages
            List<Message> messageList = new List<Message>();

            // Loop through each message element and add a Message object to the list
            foreach (XElement message in messages)
            {
                int id = (int)message.Attribute("id");
                string name = message.Element("name").Value;
                string content = message.Element("content").Value;
                string speed = message.Element("speed").Value;
                messageList.Add(new Message { Id = id, Name = name, Content = content, Speed = speed });
            }

            // Bind the list of messages to the DataGrid
            MyDataGrid.Items = messageList;
        }


        private void btnSlider_Click(object sender, RoutedEventArgs e)
        {


            double sliderValue = Convert.ToDouble(txtMessageOut.Text);

            if (!VerifyData(sliderValue))
            {
                return;
            }
            txtDataSpeed.Text = sliderValue.ToString();
            sliderValue = Convertion(sliderValue);

            Trace.WriteLine(sliderValue);

            if (sliderValue > value)
            {
                for (int i = ((int)value); i <= sliderValue; i++)
                {
                    pwmChannel.DutyCycle = (double)i / 1000;
                    Trace.WriteLine(i);
                    System.Threading.Thread.Sleep(10);
                }
            }
            else
            {
                for (int i = ((int)value); i >= sliderValue; i--)
                {
                    pwmChannel.DutyCycle = (double)i / 1000;
                    Trace.WriteLine(i);
                    System.Threading.Thread.Sleep(10);
                }
            }
            value = sliderValue;







        }

        private bool VerifyData(double value)
        {
            var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager
      .GetMessageBoxStandardWindow("Error", "La cantidad no está en el rango establecido, debe  ser entre 5-45 RPM.");
            if (value < 5 || value > 45)
            {
                messageBoxStandardWindow.Show();
                return false;
            }

            return true;
        }
        private double Convertion(double value)
        {
            double converted = 0;
            if (value >= 0 || value <= 30)
            {
                converted = ((value - 2.5) * 1000) / 46.5;
            }
            if (value >= 30 && value <= 33)
            {
                converted = ((value - 2.4) * 1000) / 46.5;
            }
            if (value > 33 && value <= 35)
            {
                converted = ((value - 2.3) * 1000) / 46.5;
            }
            if (value > 35 && value <= 44)
            {
                converted = ((value - 2.2) * 1000) / 46.5;
            }
            if (value == 45)
            {
                converted = ((value - 1.9) * 1000) / 46.5;
            }

            return converted;
            //1000 = 60hz
            //x = 15hz

        }


        private void btnMessage_Click(object sender, RoutedEventArgs e)
        {
            ConnectPrinterOne();
            ConnectPrinterTwo();
            SendMsgPrinterOne();
            SendMsgPrinterTwo();
            UpdatePrinterOne();
            UpdatePrinterTwo();
        }



        private void SetPrinterOne()
        {

            // Get a new EtherNet/IP instance
            EIP1 = new EIP("10.10.10.1", 44818, @"C:\Temp\EIP");
            EIP1.Log += EIP1_Log;
        }

        private void SetPrinterTwo()
        {

            EIP2 = new EIP("10.10.10.2", 44818, @"C:\Temp\EIP");
            EIP2.Log += EIP2_Log;
        }


        private void ConnectPrinterOne()
        {
            try
            {
                if (EIP1.StartSession())
                {
                    // Open a path to the device
                    if (EIP1.ForwardOpen())
                    {
                        // Blindly Set COM on and read it back
                        if (EIP1.SetAttribute(ccIJP.Online_Offline, "On Line"))
                        {

                        }
                        // Close the forward to avoid a timeout
                    }
                    EIP1.ForwardClose();
                }
                else
                {
                    EIP1.EndSession();
                }
            }
            catch (Exception ex)
            {
                EIP1.EndSession();
            }

        }

        private void ConnectPrinterTwo()
        {
            try
            {
                if (EIP2.StartSession())
                {
                    // Open a path to the device
                    if (EIP2.ForwardOpen())
                    {
                        // Blindly Set COM on and read it back
                        if (EIP2.SetAttribute(ccIJP.Online_Offline, "On Line"))
                        {

                        }
                        // Close the forward to avoid a timeout
                    }
                    EIP2.ForwardClose();
                }
                else
                {
                    EIP2.EndSession();
                }
            }
            catch (Exception ex)
            {
                EIP2.EndSession();
            }

        }
        public void EIP1_Log(EIP sender, string msg)
        {
        }

        public void EIP2_Log(EIP sender, string msg)
        {
        }

        private void SendMsgPrinterOne()
        {

            EIP1.UseAutomaticReflection = true; // Speed up processing
            if (EIP1.ForwardOpen())
            {        // open a data forwarding path
                try
                {
                    ClassAttr1 = (int[])EIP.ClassCodeAttributes[0].GetEnumValues();
                    attr1 = EIP.AttrDict[EIP.ClassCodes[0], (byte)ClassAttr1[0]];

                    byte[] data = EIP1.FormatOutput(attr1.Service, txtMessageOut.Text);

                    EIP1.ServiceAttribute(EIP.ClassCodes[0], (byte)ClassAttr1[0], data);
                }
                catch (EIPIOException e1)
                {
                    // In case of an EIP I/O error
                    string name = $"{EIP1.GetAttributeName(e1.ClassCode, e1.Attribute)}";
                    string msg = $"EIP I/O Error on {e1.AccessCode}/{e1.ClassCode}/{name}";
                }
                catch
                {
                    // You are on your own here
                }
            }
            EIP1.ForwardClose(); // Must be outside the ForwardOpen if block

        }
        private void SendMsgPrinterTwo()
        {

            EIP2.UseAutomaticReflection = true; // Speed up processing
            if (EIP2.ForwardOpen())
            {        // open a data forwarding path
                try
                {
                    ClassAttr2 = (int[])EIP.ClassCodeAttributes[0].GetEnumValues();
                    attr2 = EIP.AttrDict[EIP.ClassCodes[0], (byte)ClassAttr2[0]];

                    byte[] data = EIP2.FormatOutput(attr2.Service, txtMessageOut.Text);

                    EIP2.ServiceAttribute(EIP.ClassCodes[0], (byte)ClassAttr2[0], data);
                }
                catch (EIPIOException e1)
                {
                    // In case of an EIP I/O error
                    string name = $"{EIP2.GetAttributeName(e1.ClassCode, e1.Attribute)}";
                    string msg = $"EIP I/O Error on {e1.AccessCode}/{e1.ClassCode}/{name}";
                }
                catch
                {
                    // You are on your own here
                }
            }
            EIP2.ForwardClose(); // Must be outside the ForwardOpen if block

        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {

            ConnectPrinterOne();
            ConnectPrinterTwo();
            UpdatePrinterOne();
            UpdatePrinterTwo();
        }




        private void DeleteOnPrinterOne()
        {
            if (EIP1.ForwardOpen())
            {        // open a data forwarding path
                try
                {
                    ClassAttr1 = (int[])EIP.ClassCodeAttributes[0].GetEnumValues();
                    attr1 = EIP.AttrDict[EIP.ClassCodes[0], (byte)ClassAttr1[2]];
                    //EIP1.SetAttribute(ClassCode.Print_data_management, (byte)ccPDM.Delete_Print_Data, txtMessageOut.Text);
                    byte[] data = EIP1.FormatOutput(attr1.Set, txtMessageOut.Text);

                    EIP1.SetAttribute(EIP.ClassCodes[0], (byte)ClassAttr1[2], data);
                }
                catch (EIPIOException e1)
                {
                    // In case of an EIP I/O error
                    string name = $"{EIP1.GetAttributeName(e1.ClassCode, e1.Attribute)}";
                    string msg = $"EIP I/O Error on {e1.AccessCode}/{e1.ClassCode}/{name}";
                }
                catch
                {
                    // You are on your own here
                }
            }
            EIP1.ForwardClose(); // Must be outside the ForwardOpen if block
        }
        private void DeleteOnPrinterTwo()
        {
            if (EIP2.ForwardOpen())
            {        // open a data forwarding path
                try
                {
                    ClassAttr2 = (int[])EIP.ClassCodeAttributes[0].GetEnumValues();
                    attr2 = EIP.AttrDict[EIP.ClassCodes[0], (byte)ClassAttr2[2]];

                    byte[] data = EIP2.FormatOutput(attr2.Set, txtMessageOut.Text);
                    //EIP2.SetAttribute(ClassCode.Print_data_management, (byte)ccPDM.Delete_Print_Data, txtMessageOut.Text);
                    EIP2.SetAttribute(EIP.ClassCodes[0], (byte)ClassAttr2[2], data);
                }
                catch (EIPIOException e1)
                {
                    // In case of an EIP I/O error
                    string name = $"{EIP2.GetAttributeName(e1.ClassCode, e1.Attribute)}";
                    string msg = $"EIP I/O Error on {e1.AccessCode}/{e1.ClassCode}/{name}";
                }
                catch
                {
                    // You are on your own here
                }
            }
            EIP2.ForwardClose(); // Must be outside the ForwardOpen if block
        }


        private void UpdatePrinterOne()
        {

            if (EIP1.ForwardOpen())
            {        // open a data forwarding path
                try
                {
                    if (EIP1.GetAttribute(ClassCode.Print_format, (byte)ccPF.Message_Name, EIP1.Nodata))
                    {
                        txtDataOut1.Text = EIP1.GetDataValue;
                    }



                }
                catch (EIPIOException e1)
                {
                    // In case of an EIP I/O error
                    string name = $"{EIP1.GetAttributeName(e1.ClassCode, e1.Attribute)}";
                    string msg = $"EIP I/O Error on {e1.AccessCode}/{e1.ClassCode}/{name}";
                }


            }

            EIP1.ForwardClose(); // Must be outside the ForwardOpen if block
            if (EIP1.ForwardOpen())
            {        // open a data forwarding path
                try
                {
                    if (EIP1.GetAttribute(ClassCode.Print_format, (byte)ccPF.Print_Character_String, EIP1.Nodata))
                    {
                        msgDataOut1.Text = EIP1.GetDataValue;
                    }



                }
                catch (EIPIOException e1)
                {
                    // In case of an EIP I/O error
                    string name = $"{EIP1.GetAttributeName(e1.ClassCode, e1.Attribute)}";
                    string msg = $"EIP I/O Error on {e1.AccessCode}/{e1.ClassCode}/{name}";
                }


            }

            EIP1.ForwardClose(); // Must be outside the ForwardOpen if block
        }


        private void UpdatePrinterTwo()
        {

            if (EIP2.ForwardOpen())
            {        // open a data forwarding path
                try
                {
                    if (EIP2.GetAttribute(ClassCode.Print_format, (byte)ccPF.Message_Name, EIP2.Nodata))
                    {
                        txtDataOut2.Text = EIP2.GetDataValue;
                    }
                    //if (eip2.getattribute(classcode.print_format, (byte)ccpf.print_character_string, eip2.nodata))
                    //{
                    //    msgdataout2.text = eip2.getdatavalue;
                    //}

                }
                catch (EIPIOException e2)
                {
                    // In case of an EIP I/O error
                    string name = $"{EIP2.GetAttributeName(e2.ClassCode, e2.Attribute)}";
                    string msg = $"EIP I/O Error on {e2.AccessCode}/{e2.ClassCode}/{name}";
                }

            }
            EIP2.ForwardClose(); // Must be outside the ForwardOpen if block
            if (EIP2.ForwardOpen())
            {        // open a data forwarding path
                try
                {
                    if (EIP2.GetAttribute(ClassCode.Print_format, (byte)ccPF.Print_Character_String, EIP2.Nodata))
                    {
                        msgDataOut2.Text = EIP2.GetDataValue;
                    }

                    //if (EIP2.GetAttribute(ClassCode.Print_format, (byte)ccPF.Print_Character_String, EIP2.Nodata))
                    //{
                    //    msgDataOut2.Text = EIP2.GetDataValue;
                    //}

                }
                catch (EIPIOException e2)
                {
                    // In case of an EIP I/O error
                    string name = $"{EIP2.GetAttributeName(e2.ClassCode, e2.Attribute)}";
                    string msg = $"EIP I/O Error on {e2.AccessCode}/{e2.ClassCode}/{name}";
                }

            }
            EIP2.ForwardClose(); // Must be outside the ForwardOpen if block
        }

        private void addKeyPadOne(object sender, RoutedEventArgs e)
        {
            txtMessageOut.Text += btn1.Text;

        }
        private void addKeyPadTwo(object sender, RoutedEventArgs e)
        {
            txtMessageOut.Text += btn2.Text;

        }
        private void addKeyPadThree(object sender, RoutedEventArgs e)
        {
            txtMessageOut.Text += btn3.Text;

        }
        private void addKeyPadFour(object sender, RoutedEventArgs e)
        {
            txtMessageOut.Text += btn4.Text;

        }
        private void addKeyPadFive(object sender, RoutedEventArgs e)
        {
            txtMessageOut.Text += btn5.Text;

        }
        private void addKeyPadSix(object sender, RoutedEventArgs e)
        {
            txtMessageOut.Text += btn6.Text;

        }
        private void addKeyPadSeven(object sender, RoutedEventArgs e)
        {
            txtMessageOut.Text += btn7.Text;

        }
        private void addKeyPadEight(object sender, RoutedEventArgs e)
        {
            txtMessageOut.Text += btn8.Text;

        }
        private void addKeyPadNine(object sender, RoutedEventArgs e)
        {
            txtMessageOut.Text += btn9.Text;

        }
        private void addKeyPadZero(object sender, RoutedEventArgs e)
        {
            txtMessageOut.Text += btn0.Text;

        }
        private void addKeyPadErase(object sender, RoutedEventArgs e)
        {
            txtMessageOut.Text = "";

        }
    }
}
