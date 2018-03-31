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
using System.Globalization;

using MySql.Data.MySqlClient;

enum DATABASE_FIELD_COUNT
{
    GAMEOBJECT_TEMPLATE = 15,
    GAMEOBJECT = 11,
}

namespace WAS_LoginServer
{
    public partial class frmLoginServer : Form
    {
        private byte[] m_buffer = new byte[8092];
        private Socket m_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private CultureInfo m_objFormatProvider;

        private GameObjectGUIDHandler m_objGuidHandler;

        private List<GameObjectTemplate_DB> m_listGameObjectTemplates;
        private List<GameObject_DB> m_listGameObjects;

        private List<PlayerObject> m_listPlayerObject;
        private List<Grid> m_listGrids;

        public frmLoginServer()
        {
            InitializeComponent();

            CheckForIllegalCrossThreadCalls = false;

            if (!Directory.Exists("log"))
                Directory.CreateDirectory("log");

            //clientSockets = new List<ClientSocket>();

            m_listPlayerObject = new List<PlayerObject>();

            m_listGameObjectTemplates = new List<GameObjectTemplate_DB>();
            m_listGameObjects = new List<GameObject_DB>();

            m_listGrids = new List<Grid>();

            m_objGuidHandler = new GameObjectGUIDHandler();
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

            m_ServerSocket.NoDelay = true;

            m_objFormatProvider = CultureInfo.CreateSpecificCulture("en-US");

            readDatabase();

            m_ServerSocket.Listen(1);
            lblStatus.Text = "Running";
            lblStatus.BackColor = Color.Green;
            txbLog.AppendText("Server is running\n");

            m_ServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            Socket s = m_ServerSocket.EndAccept(ar);
            //clientSockets.Add(new ClientSocket(s));
            lbxClients.Items.Add(s.RemoteEndPoint.ToString());
            lblConnectedClients.Text = "Connected clients: " + m_listPlayerObject.Count.ToString();
            txbLog.AppendText("New client connected from: " + s.RemoteEndPoint.ToString() + "\n");
            s.BeginReceive(m_buffer, 0, m_buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), s);
            m_ServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            Socket s = (Socket)ar.AsyncState;
            if (s.Connected)
            {
                int receivedBytes;
                try
                {
                    receivedBytes = s.EndReceive(ar);
                }
                catch (Exception)
                {
                    for (int i = 0; i < m_listPlayerObject.Count; i++)
                    {
                        if (m_listPlayerObject[i].m_Socket.RemoteEndPoint.ToString().Equals(s.RemoteEndPoint.ToString()))
                        {
                            lbxClients.Items.RemoveAt(lbxClients.Items.IndexOf(s.RemoteEndPoint.ToString()));
                            //clientSockets.RemoveAt(i);
                            txbLog.AppendText("Client " + s.RemoteEndPoint.ToString() + " disconnected \n");

                            // cleanup playerObjects = new List<PlayerGameObjects>();
                            removePlayerObjectBySocket(s);
                            lblConnectedClients.Text = "Connected clients: " + m_listPlayerObject.Count.ToString();
                        }
                    }
                    return;
                }

                if (receivedBytes != 0)
                {
                    byte[] dataBuffer = new byte[receivedBytes];
                    Array.Copy(m_buffer, dataBuffer, receivedBytes);

                    string strReceived = Encoding.ASCII.GetString(dataBuffer);

                    string[] splittedData = strReceived.Split('|');

                    // First split is always empty
                    if (splittedData.Length > 1)
                    {
                        for(int i = 1; i < splittedData.Length; i++)
                            HandlePacket(s, splittedData[i]);
                    }
                }
                else
                {
                    for (int i = 0; i < m_listPlayerObject.Count; i++)
                    {
                        if (m_listPlayerObject[i].m_Socket.RemoteEndPoint.ToString().Equals(s.RemoteEndPoint.ToString()))
                        {
                            lbxClients.Items.RemoveAt(lbxClients.Items.IndexOf(s.RemoteEndPoint.ToString()));
                            //clientSockets.RemoveAt(i);
                            lblConnectedClients.Text = "Connected clients: " + m_listPlayerObject.Count.ToString();
                            txbLog.AppendText("Client " + s.RemoteEndPoint.ToString() + " sent no data \n");
                        }
                    }
                    return;
                }
                try
                {
                    s.BeginReceive(m_buffer, 0, m_buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), s);
                }
                catch
                { }
            }
        }

        private void SendData(Socket s, string strMessage)
        {
            try
            {
                byte[] bytData = Encoding.ASCII.GetBytes(strMessage);
                s.BeginSend(bytData, 0, bytData.Length, SocketFlags.None, new AsyncCallback(SendCallback), s);
                m_ServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
            }
            catch
            { }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket s = (Socket)ar.AsyncState;
                s.EndSend(ar);
            }
            catch
            { }
        }

        private void SendAllPlayersToSocket(Socket s)
        {
            for (int i = 0; i < m_listPlayerObject.Count; i++)
            {
                if (m_listPlayerObject[i].m_Socket != s)
                    SendData(s, "|0x003/" + m_listPlayerObject[i].getPlayerObjectSerialized());
            }
        }

        private void BroadcastToPlayersExcept(Socket s,string strMessage)
        {
            for (int i = 0; i < m_listPlayerObject.Count; i++)
            {
                if(m_listPlayerObject[i].m_Socket != s)
                    SendData(m_listPlayerObject[i].m_Socket, strMessage);
            }
        }

        private void BroadcastToPlayers(string strMessage)
        {
            for (int i = 0; i < m_listPlayerObject.Count; i++)
            {
                SendData(m_listPlayerObject[i].m_Socket, strMessage);
            }
        }

        private void Respond(Socket s, string strMessage)
        {
            SendData(s, strMessage);
        }

        private void HandlePacket(Socket s, string strData)
        {
            string[] splittedData = strData.Split('/');
            switch (splittedData[0])
            {
                default:
                    txbLog.AppendText("Unknown packet: " + strData + "\n");
                    break;
                case "0x000":
                    txbLog.AppendText("Login request: " + splittedData[1] + " " + splittedData[2] + "\n");
                    handleLoginRequest(s, splittedData[1], splittedData[2]);
                    break;
                case "0x001":
                    txbLog.AppendText("Logout request: " + splittedData[1] + " " + splittedData[2] + "\n");
                    break;
                case "0x004":
                    handlePlayerPositionUpdatePackage(s, strData);
                    break;
                case "0x100":
                    handleGameobjectEntryRequest(s, strData);
                    break;
            }
        }

        private void handleGameobjectEntryRequest(Socket s, string strData)
        {
            string[] splittedData = strData.Split('/');
            // 0 = 0x010
            // 1 = player GUID
            // 2 = Gameobject Entry

            ulong ulSenderGUID = 0;
            ulong ulGobjectEntry = 0;

            try
            {
                ulSenderGUID = ulong.Parse(splittedData[1], m_objFormatProvider);
                ulGobjectEntry = ulong.Parse(splittedData[2], m_objFormatProvider);

                //m_listGameObjectTemplates;

                GameObjectTemplate_DB objTempGobjectTemplate = m_listGameObjectTemplates.Find(x => x.getEntry() == ulGobjectEntry);

                string strDataToSend = "|0x101/" + objTempGobjectTemplate.getSerializedData(m_objFormatProvider);

                SendData(s, strDataToSend);
            }
            catch
            {

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

        public void handleLoginRequest(Socket s, string strName, string strPassword)
        {
            // Check something in DB

            // Types
            // |0x002 = login result
            // |0x003 = new player

            // everything is fine, add it
            UInt64 uiNewGUID = m_objGuidHandler.generateNewGUID();

            m_listPlayerObject.Add(new PlayerObject(uiNewGUID, s, strName));

            // tell player who he is
            // build message
            string strLoginResult = "|0x002/" + uiNewGUID.ToString();
            // currentguid = 
            SendData(s, strLoginResult);

            // now broadcast new player to everybody
            string strBroadcastNewPlayer = "|0x003/" + uiNewGUID.ToString() + "/" + strName;
            BroadcastToPlayersExcept(s, strBroadcastNewPlayer);

            lblConnectedClients.Text = "Connected clients: " + m_listPlayerObject.Count.ToString();

            // and send all players to new player
            SendAllPlayersToSocket(s);
        }

        private void readDatabase()
        {
            string connStr ="server=localhost;user=root;database=was;port=3306;password=s25;";
            MySqlConnection conn = new MySqlConnection(connStr);

            try
            {
                //Console.WriteLine("Connecting to MySQL...");
                txbLog.AppendText("Connecting to MySQL...\n");
                conn.Open();
                // Perform database operations 
            }
            catch (Exception ex)
            {
                txbLog.AppendText(ex.ToString() + "\n");
                conn.Close();
                return;
                //Console.WriteLine(ex.ToString());
            }

            readTable_gameobject_template(conn);
            readTable_gameobject(conn);

            conn.Close();
            txbLog.AppendText("MySQL Done.\n");
            //Console.WriteLine("Done.");
        }

        public bool readTable_gameobject_template(MySqlConnection conn)
        {
            string sql = "SELECT * FROM gameobject_template";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            MySqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                if(rdr.FieldCount == (int)DATABASE_FIELD_COUNT.GAMEOBJECT_TEMPLATE)
                {
                    UInt64 uiEntry      = UInt64.Parse(rdr.GetValue(0).ToString());
                    UInt64 uiType       = UInt64.Parse(rdr.GetValue(1).ToString());
                    UInt64 uiDisplayID  = UInt64.Parse(rdr.GetValue(2).ToString());
                    string strName      = rdr.GetValue(3).ToString();
                    float  fScale       = float.Parse(rdr.GetValue(4).ToString(), m_objFormatProvider);

                    UInt64 uiData0 = UInt64.Parse(rdr.GetValue(5).ToString());
                    UInt64 uiData1 = UInt64.Parse(rdr.GetValue(6).ToString());
                    UInt64 uiData2 = UInt64.Parse(rdr.GetValue(7).ToString());
                    UInt64 uiData3 = UInt64.Parse(rdr.GetValue(8).ToString());
                    UInt64 uiData4 = UInt64.Parse(rdr.GetValue(9).ToString());
                    UInt64 uiData5 = UInt64.Parse(rdr.GetValue(10).ToString());
                    UInt64 uiData6 = UInt64.Parse(rdr.GetValue(11).ToString());
                    UInt64 uiData7 = UInt64.Parse(rdr.GetValue(12).ToString());

                    string strScriptName = rdr.GetValue(3).ToString();

                    m_listGameObjectTemplates.Add(new GameObjectTemplate_DB(uiEntry, uiType, uiDisplayID, strName, fScale, uiData0, uiData1, uiData2, uiData3, uiData4, uiData5, uiData6, uiData7, strScriptName));
                }
                else
                {
                    string row = "";
                    for (int i = 0; i < rdr.FieldCount; i++)
                        row += rdr.GetValue(i).ToString() + ", ";
                    txbLog.AppendText("Could not load gameobject_template: " + row + "\n");
                }
            }
            rdr.Close();
            txbLog.AppendText("GameobjectTemplate loaded: " + m_listGameObjectTemplates.Count.ToString() + " Entries.\n");

            return true;
        }

        public bool readTable_gameobject(MySqlConnection conn)
        {
            string sql = "SELECT * FROM gameobject";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            MySqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                if (rdr.FieldCount == (int)DATABASE_FIELD_COUNT.GAMEOBJECT)
                {
                    UInt64 uiGUID = UInt64.Parse(rdr.GetValue(0).ToString());
                    UInt64 uiEntry = UInt64.Parse(rdr.GetValue(1).ToString());
                    UInt64 uiMap = UInt64.Parse(rdr.GetValue(2).ToString());

                    float fPosX = float.Parse(rdr.GetValue(3).ToString().Replace(',','.'), m_objFormatProvider);
                    float fPosY = float.Parse(rdr.GetValue(4).ToString().Replace(',', '.'), m_objFormatProvider);
                    float fPosZ = float.Parse(rdr.GetValue(5).ToString().Replace(',', '.'), m_objFormatProvider);

                    float fRotX = float.Parse(rdr.GetValue(6).ToString().Replace(',', '.'), m_objFormatProvider);
                    float fRotY = float.Parse(rdr.GetValue(7).ToString().Replace(',', '.'), m_objFormatProvider);
                    float fRotZ = float.Parse(rdr.GetValue(8).ToString().Replace(',', '.'), m_objFormatProvider);

                    UInt64 uiSpawntime = UInt64.Parse(rdr.GetValue(9).ToString());

                    UInt64 uiState = UInt64.Parse(rdr.GetValue(10).ToString());

                    // check if the entry is valid
                    if (m_listGameObjectTemplates.Exists(item => item.getEntry() == uiEntry))
                    {
                        m_listGameObjects.Add(new GameObject_DB(uiGUID, uiEntry, uiMap, fPosX, fPosY, fPosZ, fRotX, fRotY, fRotZ, uiSpawntime, uiState));

                        // check if this is a new grid?
                        string strGridID = m_listGameObjects.Find(item => item.getGUID() == uiGUID).getGridID();

                        if(!m_listGrids.Exists(item => item.getStringID() == strGridID))
                        {
                            m_listGrids.Add(new Grid((ulong)fPosX/32, (ulong)fPosY/32));
                        }
                    }
                    else
                    {
                        txbLog.AppendText("invalid entry: " + uiEntry.ToString() + " for gameobject: " + uiGUID + "\n");
                    }
                }
                else
                {
                    string row = "";
                    for (int i = 0; i < rdr.FieldCount; i++)
                        row += rdr.GetValue(i).ToString() + ", ";
                    txbLog.AppendText("Could not load gameobject: " + row + "\n");
                }
            }
            txbLog.AppendText("Gameobject loaded: " + m_listGameObjects.Count.ToString() + " Entries.\n");

            rdr.Close();
            return true;
        }

        public void handlePlayerPositionUpdatePackage(Socket s, string strData)
        {
            string[] splittedData = strData.Split('/');
            // 0 = 0x004 (Player Position Update)       splittedData[0]
            // 1 = GUID                                 splittedData[1]
            // 2 = Type (1 = Head, 2 = LHD, 3 = RHD)    splittedData[2]
            // 3 = Pos X                                splittedData[3]
            // 4 = Pos Y                                splittedData[4]
            // 5 = Pos Z                                splittedData[5]
            // 6 = Rot X                                splittedData[6]
            // 7 = Rot Y                                splittedData[7]
            // 8 = Rot Z                                splittedData[8]
            // 9 = Rot W                                splittedData[9]
            // 10 = Vec X                               splittedData[10]
            // 11 = Vec Y                               splittedData[11]
            // 12 = Vec Z                               splittedData[12]

            // Player needs to exist
            PlayerObject objTempPlayer = m_listPlayerObject.Find(x => x.m_uiGUID == ulong.Parse(splittedData[1], m_objFormatProvider));
            ulong ulGUID = 0;
            ushort usType = 0;
            float posX = 0.0f;
            float posY = 0.0f;
            float posZ = 0.0f;
            float rotX = 0.0f;
            float rotY = 0.0f;
            float rotZ = 0.0f;
            float rotW = 0.0f;

            try
            {
                ulGUID = ulong.Parse(splittedData[1], m_objFormatProvider);
                usType = ushort.Parse(splittedData[2], m_objFormatProvider);

                posX = float.Parse(splittedData[3], m_objFormatProvider);
                posY = float.Parse(splittedData[4], m_objFormatProvider);
                posZ = float.Parse(splittedData[5], m_objFormatProvider);

                // Rotation
                rotX = float.Parse(splittedData[6], m_objFormatProvider);
                rotY = float.Parse(splittedData[7], m_objFormatProvider);
                rotZ = float.Parse(splittedData[8], m_objFormatProvider);
                rotW = float.Parse(splittedData[9], m_objFormatProvider);
            }
            catch
            {
                txbLog.AppendText("An error occured parsing a player 0x004 package\n");
                return;
            }

            objTempPlayer.updatePlayerBodyTransform(usType, posX, posY, posZ, rotX, rotY, rotZ, rotW);

            if (usType == 0)
            {
                string strGridID = ((ulong)posX / 32).ToString() + "|" + ((ulong)posY / 32).ToString();

                if (objTempPlayer.m_strGridID != strGridID)
                {
                    // set new grid ID
                    objTempPlayer.m_strGridID = strGridID;

                    // check if grid is loaded!
                    // if not, load it!
                    if (!m_listGrids.Exists(item => item.getStringID() == strGridID))
                    {
                        m_listGrids.Add(new Grid((ulong)posX / 32, (ulong)posY / 32));
                    }

                    // check if player knows grid!
                    // if not, send it to the player
                    if (!(m_listGrids.Find(item => item.getStringID() == strGridID)).hasPlayer(ulGUID))
                    {
                        // player doesnt know grid, send all objects

                        // get list of all objects
                        List<GameObject_DB> lGameobjects = m_listGameObjects.Where(go => go.getGridID().Equals(strGridID)).ToList();

                        foreach (GameObject_DB goob in lGameobjects)
                        {
                            string strDataToSend = "";

                            strDataToSend = "|0x102/" + goob.serializeGameobject(m_objFormatProvider);

                            SendData(s, strDataToSend);
                        }
                    }
                }
            }

            BroadcastUpdatePlayerBodyTransform(ulong.Parse(splittedData[1], m_objFormatProvider), usType);
        }

        private void BroadcastUpdatePlayerBodyTransform(ulong ulGUID, ushort usType)
        {
            string strData = "|0x005" + "/" + (m_listPlayerObject.Find(x => x.m_uiGUID == ulGUID)).getPlayerBodyObjectSerialized(usType);

            foreach(PlayerObject objPlayer in m_listPlayerObject)
            {
                if(objPlayer.m_uiGUID != ulGUID)
                    SendData(objPlayer.m_Socket, strData);
            }
        }

        private void removePlayerObjectBySocket (Socket s)
        {
            m_listPlayerObject.RemoveAll(item => item.m_Socket == s);
        }
    }

    public class PlayerBodyObject
    {
        public UInt16 m_uiType { get; set; }
        public UInt16 m_uiState { get; set; }
        private float[] m_fPosition = new float[3];
        private float[] m_fRotation = new float[4];

        // returns a string in the following format
        // "type/state/posx/posy/posz/rotx/roty/rotz/rotw"
        public string serializePlayerBodyObject(CultureInfo cultureInfo)
        {
            string[] data = new string[9];

            data[0] = m_uiType.ToString(cultureInfo);
            data[1] = m_uiState.ToString(cultureInfo);

            data[2] = m_fPosition[0].ToString(cultureInfo);
            data[3] = m_fPosition[1].ToString(cultureInfo);
            data[4] = m_fPosition[2].ToString(cultureInfo);

            data[5] = m_fRotation[0].ToString(cultureInfo);
            data[6] = m_fRotation[1].ToString(cultureInfo);
            data[7] = m_fRotation[2].ToString(cultureInfo);
            data[8] = m_fRotation[3].ToString(cultureInfo);

            string strData = "";

            for(int i = 0; i < 9; i++)
            {
                strData += data[i];

                if (i < 8)
                    strData += "/";
            }

            return strData;
        }

        public void getTransform(ref float posx, ref float posy, ref float posz, ref float rotx, ref float roty, ref float rotz, ref float rotw)
        {
            getPosition(ref posx, ref posy, ref posz);
            getRotation(ref rotx, ref roty, ref rotz, ref rotw);
        }

        public void setTransform(float posx, float posy, float posz, float rotx, float roty, float rotz, float rotw)
        {
            setPosition(posx, posy, posz);
            setRotation(rotx, roty, rotz, rotw);
        }

        public void getPosition(ref float posx, ref float posy, ref float posz)
        {
            posx = m_fPosition[0];
            posy = m_fPosition[1];
            posz = m_fPosition[2];
        }

        public void getRotation(ref float rotx, ref float roty, ref float rotz, ref float rotw)
        {
            rotx = m_fRotation[0];
            roty = m_fRotation[1];
            rotz = m_fRotation[2];
            rotw = m_fRotation[3];
        }

        public void setPosition(float posx, float posy, float posz)
        {
            m_fPosition[0] = posx;
            m_fPosition[1] = posy;
            m_fPosition[2] = posz;
        }

        public void setRotation(float rotx, float roty, float rotz, float rotw)
        {
            m_fRotation[0] = rotx;
            m_fRotation[1] = roty;
            m_fRotation[2] = rotz;
            m_fRotation[3] = rotw;
        }

        public PlayerBodyObject(UInt16 type, UInt16 state)
        {
            this.m_uiType = type;
            this.m_uiState = state;

            m_fPosition[0] = 0;
            m_fPosition[1] = 0;
            m_fPosition[2] = 0;

            m_fRotation[0] = 0;
            m_fRotation[1] = 0;
            m_fRotation[2] = 0;
            m_fRotation[3] = 0;
        }

        public PlayerBodyObject(UInt16 type, UInt16 state, float posx, float posy, float posz, float rotx, float roty, float rotz, float rotw)
        {
            this.m_uiType = type;
            this.m_uiState = state;

            m_fPosition[0] = posx;
            m_fPosition[1] = posy;
            m_fPosition[2] = posz;

            m_fRotation[0] = rotx;
            m_fRotation[1] = roty;
            m_fRotation[2] = rotz;
            m_fRotation[3] = rotw;
        }
    }

    public class GameObjectGUIDHandler
    {
        private UInt64 m_uiCurrentMaxGUID;

        public UInt64 generateNewGUID()
        {
            m_uiCurrentMaxGUID += 1;
            return m_uiCurrentMaxGUID;
        }

        public GameObjectGUIDHandler()
        {
            m_uiCurrentMaxGUID = 0;
        }
    }

    public class GameObjectTemplate_DB
    {
        private UInt64 m_uiEntry;
        private UInt64 m_uiType;
        private UInt64 m_uiDisplayID;
        private string m_strName;
        private float m_fScale;
        private UInt64[] m_uiData = new UInt64[8];
        private string m_strScriptName;
        //public string m_strComment;

        public UInt64 getEntry() { return m_uiEntry; }
        public void setEntry(UInt64 m_uiNewEntry) { this.m_uiEntry = m_uiNewEntry; }

        public UInt64 getType() { return m_uiType; }
        public void setType(UInt64 m_uiNewType) { this.m_uiType = m_uiNewType; }

        public UInt64 getDisplayID() { return m_uiDisplayID; }
        public void setDisplayID(UInt64 m_uiNewDisplayID) { this.m_uiDisplayID = m_uiNewDisplayID; }

        public string getName() { return m_strName; }
        public void setName(string m_strNewName) { this.m_strName = m_strNewName; }

        public float getScale() { return m_fScale; }
        public void setScale(float m_fNewScale) { this.m_fScale = m_fNewScale; }

        public string getScriptName() { return m_strScriptName; }
        public void setScriptName(string m_strNewScriptName) { this.m_strScriptName = m_strNewScriptName; }

        // returns a string as following:
        // entry/type/displayid/name/scale/data1/data2/data3/data4/data5/data6/data7/data8
        public string getSerializedData(CultureInfo objFormatProvider)
        {
            string strData = "";

            string strEntry = m_uiEntry.ToString(objFormatProvider);
            string strType = m_uiType.ToString(objFormatProvider);
            string strDisplayID = m_uiDisplayID.ToString(objFormatProvider);
            // string strName = m_strName;
            string strScale = m_fScale.ToString(objFormatProvider);

            string strData0 = m_uiData[0].ToString(objFormatProvider);
            string strData1 = m_uiData[1].ToString(objFormatProvider);
            string strData2 = m_uiData[2].ToString(objFormatProvider);
            string strData3 = m_uiData[3].ToString(objFormatProvider);
            string strData4 = m_uiData[4].ToString(objFormatProvider);
            string strData5 = m_uiData[5].ToString(objFormatProvider);
            string strData6 = m_uiData[6].ToString(objFormatProvider);
            string strData7 = m_uiData[7].ToString(objFormatProvider);

            strData = strEntry + "/" + strType + "/" + strDisplayID + "/" + m_strName + "/" + strScale + "/" + strData0 + "/" + strData1 + "/" + strData2 + "/" + strData3 + "/" + strData4 + "/" + strData5 + "/" + strData6 + "/" + strData7;

            return strData;
        }

        public UInt64 getData(UInt16 uiIndex)
        {
            if (uiIndex < 0 || uiIndex >= 8)
                return 0;

            return m_uiData[uiIndex];
        }

        public void setData(UInt16 uiIndex, UInt64 uiData)
        {
            if (uiIndex < 0 || uiIndex >= 8)
                return;

            m_uiData[uiIndex] = uiData;
        }

        public void setData(UInt64 uiData0, UInt64 uiData1, UInt64 uiData2, UInt64 uiData3, UInt64 uiData4, UInt64 uiData5, UInt64 uiData6, UInt64 uiData7)
        {
            m_uiData[0] = uiData0;
            m_uiData[1] = uiData1;
            m_uiData[2] = uiData2;
            m_uiData[3] = uiData3;
            m_uiData[4] = uiData4;
            m_uiData[5] = uiData5;
            m_uiData[6] = uiData6;
            m_uiData[7] = uiData7;
        }

        public GameObjectTemplate_DB(UInt64 m_uiEntry, UInt64 m_uiType, UInt64 m_uiDisplayID, string m_strName, float m_fScale, UInt64 uiData0, UInt64 uiData1, UInt64 uiData2, UInt64 uiData3, UInt64 uiData4, UInt64 uiData5, UInt64 uiData6, UInt64 uiData7, string m_strScriptName)
        {
            this.m_uiEntry = m_uiEntry;
            this.m_uiType = m_uiType;
            this.m_uiDisplayID = m_uiDisplayID;
            this.m_strName = m_strName;
            this.m_fScale = m_fScale;
            this.m_strScriptName = m_strScriptName;

            m_uiData[0] = uiData0;
            m_uiData[1] = uiData1;
            m_uiData[2] = uiData2;
            m_uiData[3] = uiData3;
            m_uiData[4] = uiData4;
            m_uiData[5] = uiData5;
            m_uiData[6] = uiData6;
            m_uiData[7] = uiData7;
        }
    }

    public class GameObject_DB
    {
        private UInt64 m_uiGUID;
        private UInt64 m_uiEntry;
        private UInt64 m_uiMap;

        private string m_strGridID;

        private float[] m_fPosition = new float[6];

        private UInt64 m_uiSpawntime;
        private UInt64 m_uiState;

        // returns data as follows:
        // guid/entry/strMap/posx/posy/posz/rotx/roty/rotz/state
        public string serializeGameobject(CultureInfo objFormatProvider)
        {
            string strData = "";

            string strGUID = m_uiGUID.ToString(objFormatProvider);
            string strEntry = m_uiEntry.ToString(objFormatProvider);
            string strMap = m_uiMap.ToString(objFormatProvider);

            string strData0 = m_fPosition[0].ToString(objFormatProvider);
            string strData1 = m_fPosition[1].ToString(objFormatProvider);
            string strData2 = m_fPosition[2].ToString(objFormatProvider);
            string strData3 = m_fPosition[3].ToString(objFormatProvider);
            string strData4 = m_fPosition[4].ToString(objFormatProvider);
            string strData5 = m_fPosition[5].ToString(objFormatProvider);

            string strState = m_uiState.ToString(objFormatProvider);

            strData = strGUID + "/" + strEntry + "/" + strMap + "/" + strData0 + "/" + strData1 + "/" + strData2 + "/" + strData3 + "/" + strData4 + "/" + strData5 + "/" + strState;

            return strData;
        }

        public string getGridID()
        {
            return m_strGridID;
        }

        public ulong getGUID()
        {
            return m_uiGUID;
        }

        public ulong getEntry()
        {
            return m_uiEntry;
        }

        public GameObject_DB (UInt64 uiGUID, UInt64 uiEntry, UInt64 uiMap, float fPosX, float fPosY, float fPosZ, float fRotX, float fRotY, float fRotZ, UInt64 uiSpawntime, UInt64 uiState)
        {
            m_uiGUID = uiGUID;
            m_uiEntry = uiEntry;
            m_uiMap = uiMap;
            m_fPosition[0] = fPosX;
            m_fPosition[1] = fPosY;
            m_fPosition[2] = fPosZ;
            m_fPosition[3] = fRotX;
            m_fPosition[4] = fRotY;
            m_fPosition[5] = fRotZ;
            m_uiSpawntime = uiSpawntime;
            m_uiState = uiState;

            int gridX = (int)(fPosX / 32);
            int gridY = (int)(fPosY / 32);

            m_strGridID = gridX.ToString() + "|" + gridY.ToString();

            // check if grid exists
        }
    }

    public class PlayerObject
    {
        public UInt64 m_uiGUID { get; set; }
        public Socket m_Socket { get; set; }
        public string m_strName { get; set; }
        public string m_strGridID { get; set; }
        private List<PlayerBodyObject> playerObjects { get; set; }
        private CultureInfo m_objFormatProvider;

        // returns a string as follows:
        // guid/name
        public string getPlayerObjectSerialized()
        {
            string strData = "";

            strData = m_uiGUID.ToString() + "/" + m_strName;

            return strData;
        }

        // get player object serialized
        // returns a string as following:
        // guid/type/state/posx/posy/posz/rotx/roty/rotz/rotw
        public string getPlayerBodyObjectSerialized(UInt16 uiType)
        {
            // first, serialize the data
            string strData = (playerObjects.Find(x => x.m_uiType == uiType)).serializePlayerBodyObject(m_objFormatProvider);

            // then add the guid
            strData = m_uiGUID.ToString() + "/" + strData;

            return strData;
        }

        public void updatePlayerBodyTransform(UInt16 uiType, float newPosX, float newPosY, float newPosZ, float newRotX, float newRotY, float newRotZ, float newRotW)
        {
            (playerObjects.Find(x => x.m_uiType == uiType)).setTransform(newPosX, newPosY, newPosZ, newRotX, newRotY, newRotZ, newRotW);
        }

        public void updatePlayerBodyRotation(UInt16 uiType, float newRotX, float newRotY, float newRotZ, float newRotW)
        {
            (playerObjects.Find(x => x.m_uiType == uiType)).setRotation(newRotX, newRotY, newRotZ, newRotW);
        }

        public void updatePlayerBodyPosition(UInt16 uiType, float newPosX, float newPosY, float newPosZ)
        {
            (playerObjects.Find(x => x.m_uiType == uiType)).setPosition(newPosX, newPosY, newPosZ);
        }

        // get player object by type
        public PlayerBodyObject getPlayerBodyObject(UInt16 uiType)
        {
            return (playerObjects.Find(x => x.m_uiType == uiType));
        }

        // update state by type
        public void updatePlayerBody(UInt16 uiType, UInt16 uiNewState)
        {
            (playerObjects.Find(x => x.m_uiType == uiType)).m_uiState = uiNewState;
        }

        public PlayerObject(UInt64 uiGUID, Socket Socket, string strName)
        {
            this.m_uiGUID = uiGUID;
            this.m_Socket = Socket;
            this.m_Socket.NoDelay = true;
            this.m_strName = strName;

            m_objFormatProvider = CultureInfo.CreateSpecificCulture("en-US");

            playerObjects = new List<PlayerBodyObject>();

            // Create members, state 0 = inactive
            playerObjects.Add(new PlayerBodyObject(0, 0));
            playerObjects.Add(new PlayerBodyObject(1, 0));
            playerObjects.Add(new PlayerBodyObject(2, 0));
        }
    }

    public class Grid
    {
        private string strGridID;
        private ulong[] ulPosition = new ulong[2];
        private List<ulong> ulPlayersLoaded;

        public Grid(ulong posX, ulong posY)
        {
            ulPosition[0] = posX;
            ulPosition[1] = posY;

            strGridID = ulPosition[0].ToString() + "|" + ulPosition[1].ToString();

            ulPlayersLoaded = new List<ulong>();
        }

        public void addPlayer(ulong ulPlayerGUID)
        {
            ulPlayersLoaded.Add(ulPlayerGUID);
        }

        public bool hasPlayer(ulong ulPlayerGUID)
        {
            return ulPlayersLoaded.Contains(ulPlayerGUID);
        }

        public void getPosition(ref ulong posx, ref ulong posy)
        {
            posx = ulPosition[0];
            posy = ulPosition[1];
        }

        public string getStringID()
        {
            return strGridID;
        }
    }

}
