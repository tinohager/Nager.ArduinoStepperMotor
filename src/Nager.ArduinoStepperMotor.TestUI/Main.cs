﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Nager.ArduinoStepperMotor.TestUI
{
    public partial class Main : Form
    {
        private SerialPort _serialPort;
        private ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();
        private bool _stopUiUpdate = false;
        private bool _updateSpeed = false;

        public Main()
        {
            this.InitializeComponent();
            this.RefreshSerialPorts();

            new Thread(this.RefreshUi).Start();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            this._stopUiUpdate = true;
        }

        private void RefreshUi()
        {
            var sb = new StringBuilder();

            while (!this._stopUiUpdate)
            {
                sb.Clear();

                try
                {
                    foreach (var item in this._queue)
                    {
                        sb.AppendLine(item);
                    }

                    if (this.textBoxReceive.InvokeRequired)
                    {
                        this.textBoxReceive.Invoke(new Action(() =>
                        {
                            this.textBoxReceive.Text = sb.ToString();
                        }));
                    }
                    else
                    {
                        this.textBoxReceive.Text = sb.ToString();
                    }
                }
                catch (Exception exception)
                {

                }

                Thread.Sleep(50);
            }
        }

        private void SerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                while (this._serialPort.BytesToRead > 0)
                {
                    var data = this._serialPort.ReadLine();
                    this.smoothMotorControlWithStepCount1.DataReceived(data);
  
                    this._queue.Enqueue($"{DateTime.Now:mm:ss.fff} - {data.Trim()}");

                    if (this._queue.Count > 20)
                    {
                        this._queue.TryDequeue(out var temp);
                        this._queue.TryDequeue(out temp);
                    }
                }
            }
            catch (Exception exception)
            {

            }
        }

        private void WriteSerialData(string data)
        {
            if (this._serialPort == null)
            {
                return;
            }

            if (!this._serialPort.IsOpen)
            {
                return;
            }

            //var sw = new Stopwatch();
            //sw.Start();
            this._serialPort.WriteLine(data);
            //sw.Stop();
            //this._queue.Enqueue($"{DateTime.Now:mm:ss.fff} Send - {data} {sw.Elapsed.TotalMilliseconds}ms");
        }

        private void buttonStart_Click(object sender, System.EventArgs e)
        {
            this.WriteSerialData("start");
        }

        private void buttonStop_Click(object sender, System.EventArgs e)
        {
            this.WriteSerialData("stop");
        }

        private void buttonMoveLeft_Click(object sender, System.EventArgs e)
        {
            var rotate = this.trackBar1.Value;
            this.WriteSerialData($"move=-{rotate}");
        }

        private void buttonMoveRight_Click(object sender, System.EventArgs e)
        {
            var rotate = this.trackBar1.Value;
            this.WriteSerialData($"move={rotate}");
        }

        private void UpdateSpeed()
        {
            if (!this._updateSpeed)
            {
                return;
            }

            var speed = this.trackBarSpeed.Value;
            this.textBoxSpeed.Text = speed.ToString();
            this.WriteSerialData($"speed={speed}");
        }

        private void trackBarSpeed_ValueChanged(object sender, System.EventArgs e)
        {
            this.UpdateSpeed();
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            var rotate = this.trackBar1.Value;
            this.textBoxSteps.Text = rotate.ToString();
        }

        private void trackBarSpeed_MouseUp(object sender, MouseEventArgs e)
        {
            this._updateSpeed = true;
            this.UpdateSpeed();
        }

        private void trackBarSpeed_MouseDown(object sender, MouseEventArgs e)
        {
            this._updateSpeed = false;
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if (this._serialPort != null && this._serialPort.IsOpen)
            {
                return;
            }

            var serialPort = this.comboBoxSerialPort.SelectedItem as string;

            this._serialPort = new SerialPort(serialPort, 115200, Parity.None, 8, StopBits.One);
            //this._serialPort.Handshake = Handshake.None;
            this._serialPort.DataReceived += this.SerialPortDataReceived;
            this._serialPort.RtsEnable = true;
            //this._serialPort.ReadTimeout = 10000;
            this._serialPort.Open();

            this.buttonConnect.Enabled = false;
            this.buttonDisconnect.Enabled = true;
        }

        private void buttonDisconnect_Click(object sender, EventArgs e)
        {
            if (this._serialPort == null)
            {
                return;
            }

            this._serialPort.Close();
            this._serialPort.DataReceived -= this.SerialPortDataReceived;

            this.buttonConnect.Enabled = true;
            this.buttonDisconnect.Enabled = false;
        }

        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            this.RefreshSerialPorts();
        }

        private void RefreshSerialPorts()
        {
            this.comboBoxSerialPort.DataSource = SerialPort.GetPortNames();
        }

        private void smoothMotorControlWithStepCount1_SendCommand(string data)
        {
            this.WriteSerialData(data);
        }
    }
}