using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace WAS_LoginServer
{
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
}
