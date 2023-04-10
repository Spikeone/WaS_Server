using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Globalization;

namespace WAS_LoginServer
{
    public enum PLAYER_GRIDCHANGE_TYPE : ulong
    {
        BY_MOVEMENT = 0, // Player has teleported to a specific point
    }

    public class PlayerObject
    {
        public UInt64 m_uiGUID { get; set; }
        public Socket m_Socket { get; set; }
        public string m_strName { get; set; }
        public string m_strGridID { get; set; }
        public ulong m_ulMap;

        private List<PlayerBodyObject> playerObjects { get; set; }
        private CultureInfo m_objFormatProvider;

        private ulong m_ulEquipedWeapon = 0;

        private List<ulong> m_listItemGUIDs;

        public ulong GetMap()
        {
            return m_ulMap;
        }

        public void SetMap(ulong ulNewMap)
        {
            m_ulMap = ulNewMap;
        }

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
        // guid/name/WEAPON/HELMET/GLOVES/BODY/SHOULDER/CHARM/POTION
        //WEAPON = 2,
        //HELMET = 3,
        //GLOVES = 4,
        //BODY = 5,
        //SHOULDER = 6,
        //CHARM = 7,
        //POTION = 8,
        public string getPlayerObjectSerialized(List<Item> m_listItems, List<ItemTemplate_DB> m_listItemTemplates)
        {
            string strData = "";
            string strEquipment = "";

            ulong[] ulEquipmentEntrys = { 0, 0, 0, 0, 0, 0, 0 };

            // gives every item that is owned by the current player and equipped
            // maybe it's not import what type those items are, it's only important to get the entrys
            //if (m_listItems.Exists(item => (item.getOwner() == m_uiGUID && item.getState() == 2)))
            //{
            //    List<Item> tmpItemList = m_listItems.FindAll(item => (item.getOwner() == m_uiGUID && item.getState() == 2));
            //
            //    foreach(Item tmpItem in tmpItemList)
            //    {
            //        strEquipment += "/" + tmpItem.getEntry();
            //    }
            //}

            // well it is important to know where those items should be shown
            foreach (ulong ulItemGUID in m_listItemGUIDs)
            {
                Item objItem = m_listItems.Find(item => (item.getGUID() == ulItemGUID));

                // only equipped items
                if (objItem.getState() == 2)
                {
                    ItemTemplate_DB objItemTemplate = m_listItemTemplates.Find(template => (template.getEntry() == objItem.getEntry()));

                    // Type 2 - 8 
                    if (objItemTemplate.getItemTemplateClass(0) > 1 && objItemTemplate.getItemTemplateClass(0) < 9)
                    {
                        ulEquipmentEntrys[objItemTemplate.getItemTemplateClass(0) - 2] = objItem.getEntry();
                    }
                }
            }

            // and now serialize it
            for (int i = 0; i < 7; i++)
                strEquipment += "/" + ulEquipmentEntrys[i];


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

        public void UpdateMap(ulong NewMap, float pos_x, float pos_z)
        {
            // set Grid
            int gridX = (int)(pos_x / 32);
            int gridY = (int)(pos_z / 32);

            m_strGridID = NewMap.ToString() + "|" + gridX.ToString() + "|" + gridY.ToString();

            m_ulMap = NewMap;
        }

        public void updatePlayerBodyTransform(UInt16 uiType, float newPosX, float newPosY, float newPosZ, float newRotX, float newRotY, float newRotZ, float newRotW)
        {
            (playerObjects.Find(x => x.m_uiType == uiType)).setTransform(newPosX, newPosY, newPosZ, newRotX, newRotY, newRotZ, newRotW);

            // check if it's the head
            //if(uiType == 0)
            //{
            //    // check if grid is same (Client can not change map!)
            //    string strTempGrid = m_ulMap.ToString() + "|" + newPosX.ToString() + "|" + newPosY.ToString();
            //
            //    // Grid changed
            //    if(m_strGridID != strTempGrid)
            //    {
            //        PlayerChangeGrid(strTempGrid, PLAYER_GRIDCHANGE_TYPE.BY_MOVEMENT);
            //    }
            //}
        }

        public void PlayerChangeGrid(string strNewGrid, PLAYER_GRIDCHANGE_TYPE eChangeType)
        {
            switch(eChangeType)
            {
                case PLAYER_GRIDCHANGE_TYPE.BY_MOVEMENT:
                    {
                        // TODO: This is where one could implement a check if the distances is too far or if the player could even move

                        // Update Grid
                        m_strGridID = strNewGrid;

                        // Load Objects
                    }
                    break;
                default:
                    {

                    }break;
            }
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

        /*
        public PlayerObject(UInt64 uiGUID, Socket Socket, string strName, ulong ulMap, float fPosX, float fPosY)
        {
            this.m_uiGUID = uiGUID;
            this.m_Socket = Socket;
            this.m_Socket.NoDelay = true;
            this.m_strName = strName;
            this.m_ulMap = ulMap;

            m_objFormatProvider = CultureInfo.CreateSpecificCulture("en-US");

            playerObjects = new List<PlayerBodyObject>();
            m_listItemGUIDs = new List<ulong>();

            // Create members, state 0 = inactive
            playerObjects.Add(new PlayerBodyObject(0, 0));
            playerObjects.Add(new PlayerBodyObject(1, 0));
            playerObjects.Add(new PlayerBodyObject(2, 0));

            // set Grid
            m_strGridID = ulMap.ToString() + "|" + fPosX.ToString() + "|" + fPosY.ToString();
        }*/


        public PlayerObject(UInt64 uiGUID, Socket Socket, string strName, List<Item> lItems, ulong ulMap, float fPosX, float fPosY)
        {
            this.m_uiGUID = uiGUID;
            this.m_Socket = Socket;
            this.m_Socket.NoDelay = true;
            this.m_strName = strName;
            this.m_ulMap = ulMap;

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

            // set Grid
            int gridX = (int)(fPosX / 32);
            int gridY = (int)(fPosY / 32);

            m_strGridID = ulMap.ToString() + "|" + gridX.ToString() + "|" + gridY.ToString();
        }

        public void Dispose()
        {
            //foreach(PlayerBodyObject plrbdy in playerObjects)
            //{
            //    plrbdy.Dispose();
            //}
            //
            //playerObjects.Clear();
            //playerObjects = null;
            //m_listItemGUIDs.Clear();
            //m_listItemGUIDs = null;
            //m_Socket.Close();
            //m_Socket = null;
            //m_objFormatProvider = null;
        }

        //~PlayerObject()
        //{
        //    playerObjects.Clear();
        //    m_listItemGUIDs.Clear();
        //    m_Socket.Close();
        //    m_Socket = null;
        //    m_objFormatProvider = null;
        //}
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

            for (int i = 0; i < 9; i++)
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

        public void Dispose()
        {
            m_fPosition = null;
            m_fRotation = null;
        }
    }
}
