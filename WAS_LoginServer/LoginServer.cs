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

#region ENUMS
enum DATABASE_FIELD_COUNT
{
    GAMEOBJECT_TEMPLATE = 15,
    GAMEOBJECT = 11,
    ITEM_TEMPLATE = 27,
    ITEM = 6,
    POINTSOFINTEREST = 11,
}

enum ITEM_CLASSES : ulong
{
    INVALID = 0,
    GENERIC = 1,
    WEAPON = 2,
    HELMET = 3,
    GLOVES = 4,
    BODY = 5,
    SHOULDER = 6,
    CHARM = 7,
    POTION = 8,
}

enum ITEM_SUBCLASSES : ulong
{
    // Weapon 0-99
    SHIELD_SWORD = 0,
    SWORD_SWORD = 1,
    SPEAR = 2,
    BOW = 3,
    HANDGUN = 4,
    SWORD_2H = 5,
    STAFF_2H = 6,
    WAND_TOME = 7,
}

#endregion

namespace WAS_LoginServer
{
    public partial class frmLoginServer : Form
    {
        private bool sendSelf = true;

        private byte[] m_buffer = new byte[8092];
        private Socket m_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private CultureInfo m_objFormatProvider;

        private GameObjectGUIDHandler m_objGuidHandler;

        private List<GameObjectTemplate_DB> m_listGameObjectTemplates;
        private List<GameObject_DB> m_listGameObjects;

        private List<ItemTemplate_DB> m_listItemTemplates;
        private List<Item> m_listItems;

        private List<PlayerObject> m_listPlayerObject;
        private List<Grid> m_listGrids;

        private List<PointOfInterest_DB> m_listPointOfInterests;

        private MySqlConnection sqlconn;

        private bool hasDBConnection = false;

        Configuration objConfig = new Configuration(@".\was.conf");

        public frmLoginServer()
        {
            InitializeComponent();

            CheckForIllegalCrossThreadCalls = false;

            sqlconn = new MySqlConnection("server=" + objConfig["server"] + ";user=" + objConfig["user"] + ";database=" + objConfig["database"] + ";port=" + objConfig["port"] + ";password=" + objConfig["password"] + ";");

            if (!Directory.Exists("log"))
                Directory.CreateDirectory("log");

            //clientSockets = new List<ClientSocket>();

            m_listPlayerObject = new List<PlayerObject>();

            m_listGameObjectTemplates = new List<GameObjectTemplate_DB>();
            m_listGameObjects = new List<GameObject_DB>();

            m_listItemTemplates = new List<ItemTemplate_DB>();
            m_listItems = new List<Item>();

            m_listGrids = new List<Grid>();

            m_objGuidHandler = new GameObjectGUIDHandler();

            m_listPointOfInterests = new List<PointOfInterest_DB>();

            try
            {
                //Console.WriteLine("Connecting to MySQL...");
                txbLog.AppendText("Connecting to MySQL...\n");
                sqlconn.Open();
                hasDBConnection = true;
                // Perform database operations 
            }
            catch (Exception ex)
            {
                txbLog.AppendText(ex.ToString() + "\n");
                sqlconn.Close();
                hasDBConnection = false;
                //Console.WriteLine(ex.ToString());
            }
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

            if (hasDBConnection)
            {
                lblStatus.Text = "Running";
                lblStatus.BackColor = Color.Green;
                txbLog.AppendText("Server is running\n");
            }
            else
            {
                lblStatus.Text = "No DB";
                lblStatus.BackColor = Color.Yellow;
                txbLog.AppendText("Server is a bit running\n");
            }

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
                    SendData(s, "|0x003/" + m_listPlayerObject[i].getPlayerObjectSerialized(m_listItems, m_listItemTemplates));
            }
        }

        private void BroadcastToPlayersExcept(Socket s,string strMessage)
        {
            for (int i = 0; i < m_listPlayerObject.Count; i++)
            {
                if(m_listPlayerObject[i].m_Socket != s || sendSelf)
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
                case "0x006":
                    handlePlayerEquipmentChange(s, strData);
                    break;
                case "0x100":
                    handleGameobjectEntryRequest(s, strData);
                    break;
                case "0x200":
                    handleItemTemplateRequest(s, strData);
                    break;
            }
        }

        private void handleItemTemplateRequest(Socket s, string strData)
        {
            txbLog.AppendText("handleItemTemplateRequest(s, " + strData + ")\n");

            string[] splittedData = strData.Split('/');
            // 0 = 0x200
            // 1 = Entry

            ulong ulEntry = 0;

            try
            {
                ulEntry = ulong.Parse(splittedData[1], m_objFormatProvider);

                txbLog.AppendText("handleItemTemplateRequest => Entry:" + ulEntry.ToString() + ")\n");

                ItemTemplate_DB objTempItemTemplate = m_listItemTemplates.Find(x => x.getEntry() == ulEntry);

                txbLog.AppendText("handleItemTemplateRequest => Name:" + objTempItemTemplate.getItemTemplateName() + ")\n");

                string strDataToSend = "|0x201/" + objTempItemTemplate.getSerializedData(m_objFormatProvider);

                SendData(s, strDataToSend);

                //txbLog.AppendText("handleItemTemplateRequest sending: " + strDataToSend + "\n");
            }
            catch(Exception ex)
            {
                txbLog.AppendText("ERROR: handleItemTemplateRequest Message: " + ex.Message + "\n");
            }
        }

        private void handleGameobjectEntryRequest(Socket s, string strData)
        {
            string[] splittedData = strData.Split('/');
            // 0 = 0x100
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
            // but only if server is running
            if(!hasDBConnection)
            {
                // 1 = type
                // 2 = status
                // 3 = guid
                string strData = "|0x002/0/0";
                // currentguid = 
                SendData(s, strData);
                lbxClients.Items.RemoveAt(lbxClients.Items.IndexOf(s.RemoteEndPoint.ToString()));
                return;
            }

            // lets check in DB if player may login
            ulong ulAccId = 0;
            ulong ulGmLevel = 0;
            ulong ulStatus = 1;

            int iLoginResult = loginPlayer(strName, strPassword, ref ulAccId, ref ulGmLevel, ref ulStatus);

            if(iLoginResult != 1)
            {
                // Player can't login
                // TODO: use some good logic behind status
                string strData = "|0x002/" + iLoginResult.ToString() + "/0";
                SendData(s, strData);
                lbxClients.Items.RemoveAt(lbxClients.Items.IndexOf(s.RemoteEndPoint.ToString()));
                return;
            }

            txbLog.AppendText("Player " + strName + " had a valid login\n");

            ulong ulGUID = 0;
            string strNickName = "";
            ulong ulRace = 0;
            ulong ulGender = 0;
            ulong ulLevel = 1;
            ulong ulXP = 0;
            ulong ulMoney = 0;
            ulong ulFlags1 = 0;
            ulong ulFlags2 = 0;
            float fPosX = 0.0f;
            float fPosY = 0.0f;
            float fPosZ = 0.0f;
            float fRotX = 0.0f;
            float fRotY = 0.0f;
            float fRotZ = 0.0f;
            ulong ulMap = 0;

            // now we should read the character object
            if (!readCharacter(ulAccId, ref ulGUID, ref strNickName, ref ulRace, ref ulGender, ref ulLevel, ref ulXP, ref ulMoney, ref ulFlags1, ref ulFlags2, ref fPosX, ref fPosY, ref fPosZ, ref fRotX, ref fRotY, ref fRotZ, ref ulMap))
            {
                lbxClients.Items.RemoveAt(lbxClients.Items.IndexOf(s.RemoteEndPoint.ToString()));
                return;
            }

            txbLog.AppendText("User " + strName + " has a valid character\n");


            // Types
            // |0x002 = login result
            // |0x003 = new player

            // need to get all items
            List<Item> tmpItemList = m_listItems.FindAll(item => item.getOwner() == ulGUID);

            txbLog.AppendText("Character " + strNickName + " has " + tmpItemList.Count.ToString() + " Items\n");

            // Next step: check if items are faulty
            // For now thats just having 2 items of same type equipped
            // there are 7 slots
            bool[] hasItem = new bool[7];
            foreach (Item equippedItem in tmpItemList)
            {
                // 2 = equipped
                if(equippedItem.getState() == 2)
                {
                    // now get the template by entry
                    try
                    {
                        ItemTemplate_DB tmpTemplate = m_listItemTemplates.Find(item => item.getEntry() == equippedItem.getEntry());

                        // so it is an equipped item, check if there was an item for slot
                        // slot is determined by item class - 2
                        if(hasItem[tmpTemplate.getItemTemplateClass(0) - 2])
                        {
                            txbLog.AppendText("Player has two items in slot! Unequipping last!\n");
                            // change item state
                            // 1 = inventory
                            // update will be done later

                            m_listItems.Find(item => item.getGUID() == equippedItem.getGUID()).setState(1);
                        }
                        else
                        {
                            hasItem[tmpTemplate.getItemTemplateClass(0) - 2] = true;
                        }
                    }
                    catch
                    {
                        txbLog.AppendText("item '" + equippedItem.getEntry().ToString() + "' has no valid template!\n");
                    }
                }

            }

            // everything is fine, add it
            m_listPlayerObject.Add(new PlayerObject(ulGUID, s, strNickName, tmpItemList, ulMap, fPosX, fPosY));

            // find equiped weapon
            ulong ulEntryWeapon = 0;

            if(m_listItems.Exists(item => (item.getOwner() == ulGUID && item.getState() == 2)))
                ulEntryWeapon = m_listItems.Find(item => (item.getOwner() == ulGUID && item.getState() == 2)).getEntry();

            // tell player who he is
            // build message
            string strLoginResult = "|0x002/1/" + ulGUID.ToString();
            // currentguid = 
            SendData(s, strLoginResult);

            // now broadcast new player to everybody
            //string strBroadcastNewPlayer = "|0x003/" + ulGUID.ToString() + "/" + strNickName + "/" + ulEntryWeapon.ToString();
            string strBroadcastNewPlayer = "|0x003/" + (m_listPlayerObject.Find(item => (item.m_uiGUID == ulGUID))).getPlayerObjectSerialized(m_listItems, m_listItemTemplates);
            BroadcastToPlayersExcept(s, strBroadcastNewPlayer);

            lblConnectedClients.Text = "Connected clients: " + m_listPlayerObject.Count.ToString();

            // and send all players to new player
            SendAllPlayersToSocket(s);
        }

        private bool readCharacter(ulong ulAccId, ref ulong ulGUID, ref string strNickName, ref ulong ulRace, ref ulong ulGender, ref ulong ulLevel, ref ulong ulXP, ref ulong ulMoney, ref ulong ulFlags1, ref ulong ulFlags2, ref float fPosX, ref float fPosY, ref float fPosZ, ref float fRotX, ref float fRotY, ref float fRotZ, ref ulong ulMap)
        {
            if (!hasDBConnection)
                return false; // no DB

            string sql = "SELECT * from characters where account='" + ulAccId.ToString() + "'";
            MySqlCommand cmd = new MySqlCommand(sql, sqlconn);
            MySqlDataReader rdr = cmd.ExecuteReader();

            if (!rdr.HasRows)
            {
                rdr.Close();
                return false; // no character
            }

            while (rdr.Read())
            {
                ulGUID = ulong.Parse(rdr.GetValue(0).ToString());

                // 1 = account

                strNickName = rdr.GetValue(2).ToString();
                ulRace = ulong.Parse(rdr.GetValue(3).ToString());
                ulGender = ulong.Parse(rdr.GetValue(4).ToString());
                ulLevel = ulong.Parse(rdr.GetValue(5).ToString());
                ulXP = ulong.Parse(rdr.GetValue(6).ToString());
                ulMoney = ulong.Parse(rdr.GetValue(7).ToString());
                ulFlags1 = ulong.Parse(rdr.GetValue(8).ToString());
                ulFlags2 = ulong.Parse(rdr.GetValue(9).ToString());

                fPosX = float.Parse(rdr.GetValue(10).ToString().Replace(',', '.'), m_objFormatProvider);
                fPosY = float.Parse(rdr.GetValue(11).ToString().Replace(',', '.'), m_objFormatProvider);
                fPosZ = float.Parse(rdr.GetValue(12).ToString().Replace(',', '.'), m_objFormatProvider);

                fRotX = float.Parse(rdr.GetValue(13).ToString().Replace(',', '.'), m_objFormatProvider);
                fRotY = float.Parse(rdr.GetValue(14).ToString().Replace(',', '.'), m_objFormatProvider);
                fRotZ = float.Parse(rdr.GetValue(15).ToString().Replace(',', '.'), m_objFormatProvider);

                ulMap = ulong.Parse(rdr.GetValue(16).ToString());
            }
            rdr.Close();

            return true;
        }

        private int loginPlayer(string strName, string strPassword, ref ulong ID, ref ulong gm_level, ref ulong status)
        {
            if (!hasDBConnection)
                return 2; // no DB


            string sql = "SELECT * from accounts where username='" + strName + "' and sha_pass='" + strPassword + "'";

            //txbLog.AppendText("SQL: "+ sql + "\n");
            //
            //return 2;

            MySqlCommand cmd = new MySqlCommand(sql, sqlconn);
            MySqlDataReader rdr = cmd.ExecuteReader();

            // no result = no valid login
            if (!rdr.HasRows)
            {
                rdr.Close();
                return 3; // no Account
            }

            while (rdr.Read())
            {
                ID = ulong.Parse(rdr.GetValue(0).ToString());

                // 1 = username
                // 2 = sha_pass

                gm_level = ulong.Parse(rdr.GetValue(3).ToString());

                // 4 = email
                // 5 = joindate
                // 6 = lastip
                // 7 = lastlogin

                status = ulong.Parse(rdr.GetValue(8).ToString());
            }
            rdr.Close();

            if (status != 0)
                return 4; // not active
            else
                return 1; // no error
        }

        private void readDatabase()
        {
            if (hasDBConnection)
            {
                readTable_gameobject_template(sqlconn);
                readTable_gameobject(sqlconn);
                readTable_item_template(sqlconn);
                readTable_item(sqlconn);
                readTable_pointsofinterest(sqlconn);
            }

            txbLog.AppendText("MySQL Done.\n");
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
                    float  fScale       = float.Parse(rdr.GetValue(4).ToString().Replace(',', '.'), m_objFormatProvider);

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

        public bool readTable_pointsofinterest(MySqlConnection conn)
        {
            string sql = "SELECT * FROM PointsOfInterest";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            MySqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                if (rdr.FieldCount == (int)DATABASE_FIELD_COUNT.POINTSOFINTEREST)
                {
                    ulong ulEntry = ulong.Parse(rdr.GetValue(0).ToString());
                    ulong ulGroup = ulong.Parse(rdr.GetValue(1).ToString());
                    ulong ulType = ulong.Parse(rdr.GetValue(2).ToString());
                    ulong ulMap = ulong.Parse(rdr.GetValue(3).ToString());

                    float fPosX = float.Parse(rdr.GetValue(4).ToString().Replace(',', '.'), m_objFormatProvider);
                    float fPosY = float.Parse(rdr.GetValue(5).ToString().Replace(',', '.'), m_objFormatProvider);
                    float fPosZ = float.Parse(rdr.GetValue(6).ToString().Replace(',', '.'), m_objFormatProvider);

                    float fRotX = float.Parse(rdr.GetValue(7).ToString().Replace(',', '.'), m_objFormatProvider);
                    float fRotY = float.Parse(rdr.GetValue(8).ToString().Replace(',', '.'), m_objFormatProvider);
                    float fRotZ = float.Parse(rdr.GetValue(9).ToString().Replace(',', '.'), m_objFormatProvider);

                    // comment is not loaded into server
                    // string strComment = rdr.GetValue(10).ToString()

                    m_listPointOfInterests.Add(new PointOfInterest_DB(ulEntry, ulGroup, ulType, ulMap, fPosX, fPosY, fPosZ, fRotX, fRotY, fRotZ));

                    // check if this is a new grid?
                    int gridX = (int)(fPosX / 32);
                    int gridY = (int)(fPosY / 32);

                    string strGridID = ulMap.ToString() + "|" + gridX.ToString() + "|" + gridY.ToString();

                    if (!m_listGrids.Exists(item => item.getStringID() == strGridID))
                    {
                        m_listGrids.Add(new Grid((ulong)ulMap, (ulong)fPosX / 32, (ulong)fPosY / 32));
                    }
                }
            }

            txbLog.AppendText("PointsOfInterst loaded: " + m_listPointOfInterests.Count.ToString() + " Entries.\n");

            rdr.Close();

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
                            m_listGrids.Add(new Grid((ulong)uiMap, (ulong)fPosX/32, (ulong)fPosY/32));
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

        public bool readTable_item(MySqlConnection conn)
        {
            string sql = "SELECT * FROM item";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            MySqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                if (rdr.FieldCount == (int)DATABASE_FIELD_COUNT.ITEM)
                {
                    ulong[] ulData = new ulong[4];

                    for (int i = 0; i < 4; i++)
                        ulData[i] = ulong.Parse(rdr.GetValue(i).ToString(), m_objFormatProvider);

                    // check if the entry is valid
                    if (m_listItemTemplates.Exists(item => item.getEntry() == ulData[1]))
                    {
                        m_listItems.Add(new Item(ulData, true));
                    }
                    else
                    {
                        txbLog.AppendText("invalid entry: " + ulData[1].ToString() + " for item: " + ulData[0] + "\n");
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
            txbLog.AppendText("Item loaded: " + m_listItems.Count.ToString() + " Entries.\n");

            rdr.Close();

            return true;
        }

        public bool readTable_item_template(MySqlConnection conn)
        {
            string sql = "SELECT * FROM item_template";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            MySqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                if (rdr.FieldCount == (int)DATABASE_FIELD_COUNT.ITEM_TEMPLATE)
                {
                    ulong ulEntry =         ulong.Parse(rdr.GetValue(0).ToString(), m_objFormatProvider);
                    ulong ulClass =         ulong.Parse(rdr.GetValue(1).ToString(), m_objFormatProvider);
                    ulong ulSubClass =      ulong.Parse(rdr.GetValue(2).ToString(), m_objFormatProvider);
                    string strName =        rdr.GetValue(3).ToString();
                    ulong ulDisplayId1 =    ulong.Parse(rdr.GetValue(4).ToString(), m_objFormatProvider);
                    ulong ulDisplayId2 =    ulong.Parse(rdr.GetValue(5).ToString(), m_objFormatProvider);
                    ulong ulQuality =       ulong.Parse(rdr.GetValue(6).ToString(), m_objFormatProvider);
                    ulong ulPrice =         ulong.Parse(rdr.GetValue(7).ToString(), m_objFormatProvider);
                    ulong ulItemLevel =     ulong.Parse(rdr.GetValue(8).ToString(), m_objFormatProvider);
                    ulong ulMaxUses =       ulong.Parse(rdr.GetValue(9).ToString(), m_objFormatProvider);

                    int iOffset = 10;

                    ulong[] ulAllowances = new ulong[2];
                    for(ulong i = 0; i < 2; i++)
                    {
                        ulAllowances[i] = ulong.Parse(rdr.GetValue(iOffset).ToString(), m_objFormatProvider);
                        iOffset++;
                    }

                    ulong[] ulRequirements = new ulong[3];
                    for (ulong i = 0; i < 3; i++)
                    {
                        ulRequirements[i] = ulong.Parse(rdr.GetValue(iOffset).ToString(), m_objFormatProvider);
                        iOffset++;
                    }

                    ulong[] ulArmor = new ulong[2];
                    for (ulong i = 0; i < 2; i++)
                    {
                        ulArmor[i] = ulong.Parse(rdr.GetValue(iOffset).ToString(), m_objFormatProvider);
                        iOffset++;
                    }

                    ulong[] ulDamage = new ulong[2];
                    for (ulong i = 0; i < 2; i++)
                    {
                        ulDamage[i] = ulong.Parse(rdr.GetValue(iOffset).ToString(), m_objFormatProvider);
                        iOffset++;
                    }

                    ulong[] ulStatTypes = new ulong[4];
                    for (ulong i = 0; i < 4; i++)
                    {
                        ulStatTypes[i] = ulong.Parse(rdr.GetValue(iOffset).ToString(), m_objFormatProvider);
                        iOffset++;
                    }

                    ulong[] ulStatValues = new ulong[4];
                    for (ulong i = 0; i < 4; i++)
                    {
                        ulStatValues[i] = ulong.Parse(rdr.GetValue(iOffset).ToString(), m_objFormatProvider);
                        iOffset++;
                    }

                    m_listItemTemplates.Add(new ItemTemplate_DB(ulEntry, ulClass, ulSubClass, strName, ulDisplayId1, ulDisplayId2, ulQuality, ulPrice, ulItemLevel, ulMaxUses, ulAllowances, ulRequirements, ulArmor, ulDamage, ulStatTypes, ulStatValues));
                }
                else
                {
                    string row = "";
                    for (int i = 0; i < rdr.FieldCount; i++)
                        row += rdr.GetValue(i).ToString() + ", ";
                    txbLog.AppendText("Could not load item_template: " + row + "\n");
                }
            }
            txbLog.AppendText("item_template loaded: " + m_listItemTemplates.Count.ToString() + " Entries.\n");

            rdr.Close();
            return true;
        }

        public void handlePlayerEquipmentChange(Socket s, string strData)
        {
            string[] splittedData = strData.Split('/');

            txbLog.AppendText("received handlePlayerEquipmentChange\n");
            txbLog.AppendText(strData + "\n");

            if (!m_listPlayerObject.Exists(x => x.m_uiGUID == ulong.Parse(splittedData[1], m_objFormatProvider)))
                return;

            PlayerObject objTempPlayer = m_listPlayerObject.Find(x => x.m_uiGUID == ulong.Parse(splittedData[1], m_objFormatProvider));

            objTempPlayer.setEquipment(ulong.Parse(splittedData[2], m_objFormatProvider), ulong.Parse(splittedData[3], m_objFormatProvider));

            string strBroadcastPlayerEquipment = "|0x007/" + objTempPlayer.m_uiGUID.ToString() + "/" + ulong.Parse(splittedData[3], m_objFormatProvider) + "/" + objTempPlayer.getEquipment(ulong.Parse(splittedData[3], m_objFormatProvider)).ToString();
            BroadcastToPlayersExcept(s, strBroadcastPlayerEquipment);
        }

        public void handlePlayerPositionUpdatePackage(Socket s, string strData)
        {
            //txbLog.AppendText("handlePlayerPositionUpdatePackage\n");

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

            if (!m_listPlayerObject.Exists(x => x.m_uiGUID == ulong.Parse(splittedData[1], m_objFormatProvider)))
                return;

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
                string strGridID = objTempPlayer.GetMap().ToString() + "|" + ((ulong)posX / 32).ToString() + "|" + ((ulong)posY / 32).ToString();

                if (objTempPlayer.m_strGridID != strGridID)
                {
                    // set new grid ID
                    //objTempPlayer.m_strGridID = strGridID;

                    objTempPlayer.PlayerChangeGrid(strGridID, PLAYER_GRIDCHANGE_TYPE.BY_MOVEMENT);

                    // check if grid is loaded!
                    // if not, load it!
                    if (!m_listGrids.Exists(item => item.getStringID() == strGridID))
                    {
                        m_listGrids.Add(new Grid((ulong)objTempPlayer.GetMap(),(ulong)posX / 32, (ulong)posY / 32));
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

            //txbLog.AppendText("handlePlayerPositionUpdatePackage\n");

            BroadcastUpdatePlayerBodyTransform(ulong.Parse(splittedData[1], m_objFormatProvider), usType);
        }

        private void BroadcastUpdatePlayerBodyTransform(ulong ulGUID, ushort usType)
        {
            string strData = "|0x005" + "/" + (m_listPlayerObject.Find(x => x.m_uiGUID == ulGUID)).getPlayerBodyObjectSerialized(usType);

            //txbLog.AppendText("Sending: " + strData + "\n");

            foreach(PlayerObject objPlayer in m_listPlayerObject)
            {
                if(objPlayer.m_uiGUID != ulGUID || sendSelf)
                    SendData(objPlayer.m_Socket, strData);
            }
        }

        private void removePlayerObjectBySocket (Socket s)
        {
            m_listPlayerObject.RemoveAll(item => item.m_Socket == s);
        }
    }
}
