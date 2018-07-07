using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace WAS_LoginServer
{
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

        public GameObject_DB(UInt64 uiGUID, UInt64 uiEntry, UInt64 uiMap, float fPosX, float fPosY, float fPosZ, float fRotX, float fRotY, float fRotZ, UInt64 uiSpawntime, UInt64 uiState)
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
}
