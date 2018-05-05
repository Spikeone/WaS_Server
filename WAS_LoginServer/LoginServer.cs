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
    ITEM_TEMPLATE = 27,
    ITEM = 6,
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
                    SendData(s, "|0x003/" + m_listPlayerObject[i].getPlayerObjectSerialized(m_listItems));
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
            m_listPlayerObject.Add(new PlayerObject(ulGUID, s, strNickName, tmpItemList));

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
            string strBroadcastNewPlayer = "|0x003/" + (m_listPlayerObject.Find(item => (item.m_uiGUID == ulGUID))).getPlayerObjectSerialized(m_listItems);
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
    public class PlayerObject
    {
        public UInt64 m_uiGUID { get; set; }
        public Socket m_Socket { get; set; }
        public string m_strName { get; set; }
        public string m_strGridID { get; set; }
        private List<PlayerBodyObject> playerObjects { get; set; }
        private CultureInfo m_objFormatProvider;

        private ulong m_ulEquipedWeapon = 0;

        private List<ulong> m_listItemGUIDs;

        public ulong getEquipment(ulong ulndex)
        {
            return m_ulEquipedWeapon;
        }

        public void setEquipment(ulong ulIndex, ulong ulEntry)
        {
            if (m_ulEquipedWeapon != 0)
                m_ulEquipedWeapon = 0;
            else
                m_ulEquipedWeapon = 5;
        }

        // returns a string as follows:
        // guid/name/Equipment1/Equipment2/Equipment3/Equipment4/Equipment5/Equipment6/Equipment7
        //WEAPON = 2,
        //HELMET = 3,
        //GLOVES = 4,
        //BODY = 5,
        //SHOULDER = 6,
        //CHARM = 7,
        //POTION = 8,
        public string getPlayerObjectSerialized(List<Item> m_listItems)
        {
            string strData = "";
            string strEquipment = "";

            // gives every item that is owned by the current player and equipped
            // maybe it's not import what type those items are, it's only important to get the entrys
            if (m_listItems.Exists(item => (item.getOwner() == m_uiGUID && item.getState() == 2)))
            {
                List<Item> tmpItemList = m_listItems.FindAll(item => (item.getOwner() == m_uiGUID && item.getState() == 2));

                foreach(Item tmpItem in tmpItemList)
                {
                    strEquipment += "/" + tmpItem.getEntry();
                }
            }

            strData = m_uiGUID.ToString() + "/" + m_strName + strEquipment;

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
            m_listItemGUIDs = new List<ulong>();

            // Create members, state 0 = inactive
            playerObjects.Add(new PlayerBodyObject(0, 0));
            playerObjects.Add(new PlayerBodyObject(1, 0));
            playerObjects.Add(new PlayerBodyObject(2, 0));
        }

        public PlayerObject(UInt64 uiGUID, Socket Socket, string strName, List<Item> lItems)
        {
            this.m_uiGUID = uiGUID;
            this.m_Socket = Socket;
            this.m_Socket.NoDelay = true;
            this.m_strName = strName;

            m_objFormatProvider = CultureInfo.CreateSpecificCulture("en-US");

            playerObjects = new List<PlayerBodyObject>();
            m_listItemGUIDs = new List<ulong>();

            foreach (Item item in lItems)
                m_listItemGUIDs.Add(item.getGUID());

            // Create members, state 0 = inactive
            playerObjects.Add(new PlayerBodyObject(0, 0));
            playerObjects.Add(new PlayerBodyObject(1, 0));
            playerObjects.Add(new PlayerBodyObject(2, 0));

            if (lItems.Exists(item => (item.getState() == 2)))
                m_ulEquipedWeapon = lItems.Find(item => (item.getState() == 2)).getEntry();
        }

        ~PlayerObject()
        {
            playerObjects.Clear();
            m_Socket.Close();
            m_Socket = null;
            m_objFormatProvider = null;
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

    public class ItemTemplate_DB
    {
        ulong ulEntry;

        // class
        // subclass
        ulong[] ulClass = new ulong[2];
        string strName;
        // display ID mainhand
        // display ID offhand
        ulong[] ulDisplayIds = new ulong[2];
        ulong ulQuality;
        ulong ulPrice;
        ulong ulItemLevel;
        ulong ulMaxUses;

        // Allowed Classes
        // Allowed Races
        private ulong[] ulAllowances = new ulong[2];

        // Required Level
        // Required Skill
        // Required Reputation
        private ulong[] ulRequirements = new ulong[3];

        // Magic Armor
        // Physical Armor
        private ulong[] ulArmor = new ulong[2];

        // Damage min
        // Damage max
        private ulong[] ulDamage = new ulong[2];

        // Stat Type 1
        // Stat Type 2
        // Stat Type 3
        // Stat Type 4
        private ulong[] ulStatTypes = new ulong[4];

        // Stat Value 1
        // Stat Value 2
        // Stat Value 3
        // Stat Value 4
        private ulong[] ulStatValues = new ulong[4];

        public ulong getEntry() { return ulEntry; }

        public string getItemTemplateName() { return strName; }

        public ulong getItemTemplateQuality() { return ulQuality; }

        public ulong getItemTemplatePrice() { return ulPrice; }

        public ulong getItemTemplateLevel() { return ulItemLevel; }

        public ulong getItemTemplateMaxUses() { return ulMaxUses; }

        // Class

        public ulong getItemTemplateClass(ulong uiIndex)
        {
            if (uiIndex > 1)
                return 0;
            else
                return ulClass[uiIndex];
        }

        public void getItemTemplateClasses(ref ulong ulItemClass, ref ulong ulItemSubClass)
        {
            ulItemClass = ulClass[0];
            ulItemSubClass = ulClass[1];
        }

        // Display

        public ulong getItemTemplateDisplayID(ulong uiIndex)
        {
            if (uiIndex > 1)
                return 0;
            else
                return ulDisplayIds[uiIndex];
        }

        public void getItemTemplateulDisplayIds(ref ulong ulItemDisplayMain, ref ulong ulItemDisplayOff)
        {
            ulItemDisplayMain = ulDisplayIds[0];
            ulItemDisplayOff = ulDisplayIds[1];
        }

        // Allowance

        public ulong getItemTemplateAllowance(ulong uiIndex)
        {
            if (uiIndex > 1)
                return 0;
            else
                return ulAllowances[uiIndex];
        }

        public void getItemTemplateAllowance(ref ulong ulClass, ref ulong ulRace)
        {
            ulClass = ulAllowances[0];
            ulRace = ulAllowances[1];
        }

        private bool isItemTemplateAllowed(ulong ulIndex, ulong ulValue)
        {
            if (ulAllowances[ulIndex] == 0)
                return true;

            return (ulAllowances[ulIndex] & ulValue) == ulValue;
        }

        public bool isItemTemplateAllowedForClass(ulong ulClass)
        {
            return isItemTemplateAllowed(0, ulClass);
        }

        public bool isItemTemplateAllowedForRace(ulong ulRace)
        {
            return isItemTemplateAllowed(1, ulRace);
        }

        public bool isItemTemplateAllowedForCharacter(ulong ulClass, ulong ulRace)
        {
            return isItemTemplateAllowedForClass(ulClass) && isItemTemplateAllowedForRace(ulRace);
        }

        // Requirement

        public ulong getItemTemplateRequirement(ulong ulIndex)
        {
            if (ulIndex > 2)
                return 0;
            else
                return ulRequirements[ulIndex];
        }

        public void getItemTemplateRequirements(ref ulong ulLevel, ref ulong ulSkill, ref ulong ulReputation)
        {
            ulLevel = ulRequirements[0];
            ulSkill = ulRequirements[1];
            ulReputation = ulRequirements[2];
        }

        private bool meetsItemTemplateRequirement(ulong ulIndex, ulong ulValue)
        {
            if (ulRequirements[ulIndex] == 0)
                return true;

            return (ulRequirements[ulIndex] & ulValue) == ulValue;
        }

        public bool meetsItemTemplateLevelRequirement(ulong ulLevel)
        {
            return meetsItemTemplateRequirement(0, ulLevel);
        }

        public bool meetsItemTemplateSkillRequirement(ulong ulSkill)
        {
            return meetsItemTemplateRequirement(1, ulSkill);
        }

        public bool meetsItemTemplateReputationRequirement(ulong ulReputation)
        {
            return meetsItemTemplateRequirement(2, ulReputation);
        }

        public bool meetsItemTemplateRequirementForCharacter(ulong ulLevel, ulong ulSkill, ulong ulReputation)
        {
            return meetsItemTemplateLevelRequirement(ulLevel) && meetsItemTemplateSkillRequirement(ulSkill) && meetsItemTemplateReputationRequirement(ulReputation);
        }

        // Armor

        private ulong getItemTemplateArmor(ulong ulIndex)
        {
            if (ulIndex > 1)
                return 0;
            else
                return ulArmor[ulIndex];
        }

        public ulong getItemTemplateMagicArmor() { return getItemTemplateArmor(0); }

        public ulong getItemTemplatePhysicalArmor() { return getItemTemplateArmor(1); }

        public void getItemTemplateArmor(ref ulong ulMagicArmor, ref ulong ulPhysicalArmor)
        {
            ulMagicArmor = getItemTemplateArmor(0);
            ulPhysicalArmor = getItemTemplateArmor(1);
        }

        // Damage

        private ulong getItemTemplateDamage(ulong ulIndex)
        {
            if (ulIndex > 1)
                return 0;
            else
                return ulDamage[ulIndex];
        }

        public ulong getItemTemplateMinDamage() { return getItemTemplateDamage(0); }

        public ulong getItemTemplateMaxDamage() { return getItemTemplateDamage(1); }

        public void getItemTemplateDamage(ref ulong ulMinDamage, ref ulong ulMaxDamage)
        {
            ulMinDamage = getItemTemplateDamage(0);
            ulMaxDamage = getItemTemplateDamage(1);
        }

        // Stats

        private ulong getItemTemplateStatType(ulong ulIndex)
        {
            if (ulIndex > 3)
                return 0;
            else
                return ulStatTypes[ulIndex];
        }

        private ulong getItemTemplateStatValue(ulong ulIndex)
        {
            if (ulIndex > 3)
                return 0;
            else
                return ulStatValues[ulIndex];
        }

        public bool hasItemTemplateStat(ulong ulstat)
        {
            for (ulong i = 0; i < 4; i++)
                if (getItemTemplateStatType(i) == ulstat)
                    return true;

            return false;
        }

        public ulong getItemTemplateStatCount()
        {
            for (ulong i = 0; i < 4; i++)
                if (getItemTemplateStatType(i) == 0)
                    return (i + 1);

            return 0;
        }

        public void getItemTemplateStat(ulong ulIndex, ref ulong ulType, ref ulong ulValue)
        {
            if((ulIndex + 1) > getItemTemplateStatCount())
            {
                ulType = 0;
                ulValue = 0;

            }

            ulType = getItemTemplateStatType(ulIndex);
            ulValue = getItemTemplateStatValue(ulIndex);
        }

        // Constructor

        public ItemTemplate_DB(ulong ulEntry,ulong ulClass, ulong ulSubClass, string strName, ulong ulDisplayIdMain, ulong DisplayIdOff, ulong ulQuality, ulong ulPrice, ulong ulItemLevel, ulong ulMaxUses,
                                ulong[] ulAllowances, ulong[] ulRequirements, ulong[] ulArmor, ulong[] ulDamage, ulong[] ulStatTypes, ulong[] ulStatValues)
        {
            this.ulEntry = ulEntry;
            this.ulClass[0] = ulClass;
            this.ulClass[1] = ulSubClass;
            this.strName = strName;
            this.ulDisplayIds[0] = ulDisplayIdMain;
            this.ulDisplayIds[1] = DisplayIdOff;
            this.ulQuality = ulQuality;
            this.ulPrice = ulPrice;
            this.ulItemLevel = ulItemLevel;
            this.ulMaxUses = ulMaxUses;

            for (ulong i = 0; i < 2; i++)
                this.ulAllowances[i] = ulAllowances[i];

            for (ulong i = 0; i < 3; i++)
                this.ulRequirements[i] = ulRequirements[i];

            for (ulong i = 0; i < 2; i++)
                this.ulArmor[i] = ulArmor[i];

            for (ulong i = 0; i < 2; i++)
                this.ulDamage[i] = ulDamage[i];

            for (ulong i = 0; i < 4; i++)
                this.ulStatTypes[i] = ulStatTypes[i];

            for (ulong i = 0; i < 4; i++)
                this.ulStatValues[i] = ulStatValues[i];
        }

        // Serializer

        // returns a string as following:
        // 
        public string getSerializedData(CultureInfo objFormatProvider)
        {
            string[] strData = new string[27];

            strData[0] = ulEntry.ToString(objFormatProvider);
            strData[1] = ulClass[0].ToString(objFormatProvider);
            strData[2] = ulClass[1].ToString(objFormatProvider);
            strData[3] = strName;
            strData[4] = ulDisplayIds[0].ToString(objFormatProvider);
            strData[5] = ulDisplayIds[1].ToString(objFormatProvider);
            strData[6] = ulQuality.ToString(objFormatProvider);
            strData[7] = ulPrice.ToString(objFormatProvider);
            strData[8] = ulItemLevel.ToString(objFormatProvider);
            strData[9] = ulMaxUses.ToString(objFormatProvider);
            
            strData[10] = ulAllowances[0].ToString(objFormatProvider);
            strData[11] = ulAllowances[1].ToString(objFormatProvider);
            
            strData[12] = ulRequirements[0].ToString(objFormatProvider);
            strData[13] = ulRequirements[1].ToString(objFormatProvider);
            strData[14] = ulRequirements[2].ToString(objFormatProvider);
            
            strData[15] = ulArmor[0].ToString(objFormatProvider);
            strData[16] = ulArmor[1].ToString(objFormatProvider);
            
            strData[17] = ulDamage[0].ToString(objFormatProvider);
            strData[18] = ulDamage[1].ToString(objFormatProvider);
            
            strData[19] = ulStatTypes[0].ToString(objFormatProvider);
            strData[20] = ulStatTypes[1].ToString(objFormatProvider);
            strData[21] = ulStatTypes[2].ToString(objFormatProvider);
            strData[22] = ulStatTypes[3].ToString(objFormatProvider);
            
            strData[23] = ulStatValues[0].ToString(objFormatProvider);
            strData[24] = ulStatValues[1].ToString(objFormatProvider);
            strData[25] = ulStatValues[2].ToString(objFormatProvider);
            strData[26] = ulStatValues[3].ToString(objFormatProvider);

            string strReturnString = "";

            for(ulong i = 0; i < 27; i++)
            {
                if (strReturnString != "")
                    strReturnString += "/";

                strReturnString += strData[i];  
            }

            return strReturnString;
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

    public class Configuration
    {
        private Dictionary<string, string> objConfig = new Dictionary<string, string>();

        public string this[string strKey]
        {
            get
            {
                return stringGetKeyValue(strKey);
            }
        }

        public Configuration(string strPath)
        {
            if (!File.Exists(strPath))
            {
                // Create a file to write to.
                string[] createText =
                {
                    "###############################################################################",
                    "# ,^.                 __    __                _                           /^\\ #",
                    "# |||                / / /\\ \\ \\__ _ _ __   __| |___                  /\\   \"V\" #",
                    "# |||       _T_      \\ \\/  \\/ / _` | '_ \\ / _` / __|                /__\\   I  #",
                    "# |||   .-.[:|:].-.   \\  /\\  / (_| | | | | (_| \\__ \\               //..\\\\  I  #",
                    "# ===_ /\\|  \"'\"  |/    \\/  \\/ \\__,_|_| |_|\\__,_|___/               \\].`[/  I  #",
                    "#  E]_|\\/ \\--|-|''''|         /_\\  _ __   __| |                    /l\\/j\\  (] #",
                    "#  O  `'  '=[:]| A  |        //_\\\\| '_ \\ / _` |                   /. ~~ ,\\/I  #",
                    "#         /\"\"\"\"|  P |       /  _  \\ | | | (_| |                   \\\\L__j^\\/I  #",
                    "#        /\"\"\"\"\"`.__.'       \\_/ \\_/_| |_|\\__,_|           _        \\/--v}  I  #",
                    "#       []\"/\"\"\"\\\"[]             / _\\_      _____  _ __ __| |___    |    |  I  #",
                    "#       | \\     / |             \\ \\\\ \\ /\\ / / _ \\| '__/ _` / __|   |    |  I  #",
                    "#       | |     | |             _\\ \\\\ V  V / (_) | | | (_| \\__ \\   |    l  I  #",
                    "#     <\\\\\\)     (///>           \\__/ \\_/\\_/ \\___/|_|  \\__,_|___/ _/j  L l\\_!  #",
                    "###############################################################################",
                    "",
                    "###############",
                    "# DB - Server #",
                    "###############",
                    "server      = localhost",
                    "user        = root",
                    "database    = was",
                    "port        = 3306",
                    "password    = s25 "
                };
                File.WriteAllLines(strPath, createText);
            }

            string[] readText = File.ReadAllLines(strPath);
            foreach (string s in readText)
            {
                if (isComment(s))
                    continue;

                if (!hasValues(s))
                    continue;

                string strKey = "";
                string strValue = "";

                if (getValue(s, ref strKey, ref strValue))
                    objConfig.Add(strKey, strValue);
            }
        }

        private string stringGetKeyValue(string strKey)
        {
            if (!objConfig.ContainsKey(strKey))
                return "";

            return objConfig[strKey];
        }

        private bool isComment(string strLine)
        {
            return strLine.Length == 0 ? false : strLine.Trim().Substring(0, 1) == "#";
        }

        private bool hasValues(string strLine)
        {
            return strLine.Length == 0 ? false : strLine.Contains("=");
        }

        private bool getValue(string strLine, ref string strKey, ref string strValue)
        {
            string[] strSplit = strLine.Trim().Split('=');

            if (strSplit.Length != 2)
                return false;

            strKey = strSplit[0].Trim();
            strValue = strSplit[1].Trim();

            return true;
        }

    }

    public class Item
    {
        // guid
        // entry
        // owner
        // state
        ulong[] ulData = new ulong[4];

        // indicated if an insert is needed
        bool isSavedInDB;

        // indicates if an update is needed
        bool isUpToDateInDB;

        public ulong getGUID() { return ulData[0]; }
        public void setGUID(ulong ulNewGUID) { ulData[0] = ulNewGUID; isUpToDateInDB = false; }

        public ulong getEntry() { return ulData[1]; }

        public ulong getOwner() { return ulData[2]; }
        public void setOwner(ulong ulNewOwner) { ulData[2] = ulNewOwner; isUpToDateInDB = false; }

        public ulong getState() { return ulData[3]; }
        public void setState(ulong ulNewState)
        {
            if (ulNewState != ulData[3])
                isUpToDateInDB = false;

            ulData[3] = ulNewState;
        }

        // from DB or Trade
        public Item(ulong[] ulData, bool fromDB)
        {
            for (int i = 0; i < 4; i++)
                this.ulData[i] = ulData[i];

            if(fromDB)
            {
                isSavedInDB = true;
                isUpToDateInDB = true;
            }
            else
            {
                isSavedInDB = false;
                isUpToDateInDB = false;
            }
        }

        // from DB or Trade
        public Item(ulong ulGUID, ulong ulEntry, ulong ulOwner, ulong ulState, bool fromDB)
        {
            ulData[0] = ulGUID;
            ulData[1] = ulEntry;
            ulData[2] = ulOwner;
            ulData[3] = ulState;

            if (fromDB)
            {
                isSavedInDB = true;
                isUpToDateInDB = true;
            }
            else
            {
                isSavedInDB = false;
                isUpToDateInDB = false;
            }

        }

        // Lootet item / received item
        public Item(ulong ulEntry, ulong ulOwner, ulong ulState)
        {
            ulData[0] = 0;
            ulData[1] = ulEntry;
            ulData[2] = ulOwner;
            ulData[3] = ulState;

            isSavedInDB = false;
            isUpToDateInDB = false;
        }

        // Dropped Item
        public Item(ulong ulEntry, ulong ulState)
        {
            ulData[0] = 0;
            ulData[1] = ulEntry;
            ulData[2] = 0;
            ulData[3] = ulState;

            isSavedInDB = false;
            isUpToDateInDB = false;
        }

    }

}
