using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Net;
using System.Net.Sockets;
using System.IO;

namespace WAS_LoginServer
{
    public partial class frmLoginServer : Form
    {
        public List<ClientSocket> clientSockets { get; set; }

        private byte[] m_buffer = new byte[1024];
        private Socket m_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        public frmLoginServer()
        {
            InitializeComponent();

            CheckForIllegalCrossThreadCalls = false;

            if (!Directory.Exists("log"))
                Directory.CreateDirectory("log");

            clientSockets = new List<ClientSocket>();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SetupServer();
        }

        private void SetupServer()
        {
            lblStatus.Text = "Starting";
            txbLog.AppendText("Setting up server...\n");
            m_ServerSocket.Bind(new IPEndPoint(IPAddress.Any, 3665));

            m_ServerSocket.Listen(1);
            lblStatus.Text = "Running";
            lblStatus.BackColor = Color.Green;
            txbLog.AppendText("Server is running\n");

            m_ServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            Socket s = m_ServerSocket.EndAccept(ar);
            clientSockets.Add(new ClientSocket(s));
            lbxClients.Items.Add(s.RemoteEndPoint.ToString());
            lblConnectedClients.Text = "Connected clients: " + clientSockets.Count.ToString();
            txbLog.AppendText("New client connected from: " + s.RemoteEndPoint.ToString() + "\n");
            s.BeginReceive(m_buffer, 0, m_buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), s);
            m_ServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            Socket s = (Socket)ar.AsyncState;
            if(s.Connected)
            {
                int receivedBytes;
                try
                {
                    receivedBytes = s.EndReceive(ar);
                }
                catch (Exception)
                {
                    for(int i = 0; i < clientSockets.Count; i++)
                    {
                        if(clientSockets[i].m_Socket.RemoteEndPoint.ToString().Equals(s.RemoteEndPoint.ToString()))
                        {
                            lbxClients.Items.RemoveAt(lbxClients.Items.IndexOf(s.RemoteEndPoint.ToString()));
                            clientSockets.RemoveAt(i);
                            lblConnectedClients.Text = "Connected clients: " + clientSockets.Count.ToString();
                            txbLog.AppendText("Client " + s.RemoteEndPoint.ToString() + " disconnected \n");
                        }
                    }
                    return;
                }

                if(receivedBytes != 0)
                {
                    byte[] dataBuffer = new byte[receivedBytes];
                    Array.Copy(m_buffer, dataBuffer, receivedBytes);

                    string strReceived = Encoding.ASCII.GetString(dataBuffer);

                    HandlePacket(s, strReceived);
                }
                else
                {
                    for (int i = 0; i < clientSockets.Count; i++)
                    {
                        if (clientSockets[i].m_Socket.RemoteEndPoint.ToString().Equals(s.RemoteEndPoint.ToString()))
                        {
                            lbxClients.Items.RemoveAt(lbxClients.Items.IndexOf(s.RemoteEndPoint.ToString()));
                            clientSockets.RemoveAt(i);
                            lblConnectedClients.Text = "Connected clients: " + clientSockets.Count.ToString();
                            txbLog.AppendText("Client " + s.RemoteEndPoint.ToString() + " sent no data \n");
                        }
                    }
                    return;
                }

                s.BeginReceive(m_buffer, 0, m_buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), s);
            }
        }

        private void SendData(Socket s, string strMessage)
        {
            byte[] bytData = Encoding.ASCII.GetBytes(strMessage);
            s.BeginSend(bytData, 0, bytData.Length, SocketFlags.None, new AsyncCallback(SendCallback), s);
            m_ServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        private void SendCallback(IAsyncResult ar)
        {
            Socket s = (Socket)ar.AsyncState;
            s.EndSend(ar);
        }

        private void SendToSelected(string strMessage)
        {
            for (int i = 0; i < lbxClients.SelectedItems.Count; i++)
            {
                for (int j = 0; j < clientSockets.Count; j++)
                {
                    SendData(clientSockets[j].m_Socket, strMessage);
                }
            }
        }

        private void Broadcast(string strMessage)
        {
            for (int i = 0; i < clientSockets.Count; i++)
            {
                SendData(clientSockets[i].m_Socket, strMessage);
            }
        }

        private void Respond(Socket s, string strMessage)
        {
            SendData(s, strMessage);
        }

        private void HandlePacket(Socket s, string strData)
        {
            string[] splittedData = strData.Split('/');
            switch(splittedData[0])
            {
                default:
                    txbLog.AppendText("Unknown packet: " + strData + "\n");
                    break;
                case "0x000":
                    txbLog.AppendText("Login request: " + splittedData[1] + " " + splittedData[2] + "\n");
                    break;
                case "0x001":
                    txbLog.AppendText("Logout request: " + splittedData[1] + " " + splittedData[2] + "\n");
                    break;
            }
        }

        private void LogToFile()
        {
            string file = ".\\log\\" + DateTime.Now.ToString("YYYY-m-d") + ".log";
            if (!File.Exists(file))
            {
                File.Create(file).Dispose();
                using (TextWriter objTextWriter = new StreamWriter(file))
                {
                    objTextWriter.WriteLine("Log from: " + DateTime.Now.ToString("YYYY-m-d"));
                }
            }
            File.AppendAllLines(file, txbLog.Lines);
            txbLog.Clear();
            txbLog.AppendText("Log saved!\n");
        }

        private void btnToSelected_Click(object sender, EventArgs e)
        {
            SendToSelected(txbMessage.Text);
            txbMessage.Clear();
        }

        private void btnSendToAll_Click(object sender, EventArgs e)
        {
            Broadcast(txbMessage.Text);
            txbMessage.Clear();
        }
    }

    public class ClientSocket
    {
        public Socket m_Socket { get; set; }
        public string m_Name;

        public ClientSocket(Socket s)
        {
            this.m_Socket = s;
        }
    }
}
