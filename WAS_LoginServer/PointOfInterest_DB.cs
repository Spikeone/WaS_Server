using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WAS_LoginServer
{
    class PointOfInterest_DB
    {
        // From DB
        private ulong m_ulEntry;
        private ulong m_ulGroup;
        private ulong m_ulType;
        private ulong m_ulMap;

        private float[] m_fPosition = new float[6];

        // Not from DB
        private string m_strGridID;

        public ulong GetPOIEntry() { return m_ulEntry; }
        public ulong GetPOIGroup() { return m_ulGroup; }
        public bool HasPOIGroup() { return m_ulGroup != 0; }
        public ulong GetPOIType() { return m_ulType; }
        public bool IsPOIType(ulong ulCheckType) { return m_ulType == ulCheckType; }
        public ulong GetPOIMap() { return m_ulMap; }
        public bool IsPOIOnMap(ulong ulCheckMap) { return m_ulMap == ulCheckMap; }

        public string GetPOIGridID() { return m_strGridID; }

        public void GetPosition(ref float pos_x, ref float pos_y, ref float pos_z, ref float rot_x, ref float rot_y, ref float rot_z)
        {
            pos_x = m_fPosition[0];
            pos_y = m_fPosition[1];
            pos_z = m_fPosition[2];
            rot_x = m_fPosition[3];
            rot_y = m_fPosition[4];
            rot_z = m_fPosition[6];
        }

        public void GetPosition(ref float[] position)
        {
            for (int i = 0; i < 6; i++)
                position[i] = m_fPosition[i];
        }

        public PointOfInterest_DB(ulong ulEntry, ulong ulGroup, ulong ulType, ulong ulMap, float[] position) : this(ulEntry, ulGroup, ulType, ulMap)
        {
            for(int i = 0; i < 6; i++)
                m_fPosition[i] = position[i];

            int gridX = (int)(position[0] / 32);
            int gridY = (int)(position[1] / 32);

            m_strGridID = ulMap.ToString() + "|" + gridX.ToString() + "|" + gridY.ToString();

            m_ulEntry = ulEntry;
            m_ulGroup = ulGroup;
            m_ulType = ulType;
            m_ulMap = ulMap;
        }

        public PointOfInterest_DB(ulong ulEntry, ulong ulGroup, ulong ulType, ulong ulMap, float pos_x, float pos_y, float pos_z, float rot_x, float rot_y, float rot_z) : this(ulEntry, ulGroup, ulType, ulMap)
        {
            m_fPosition[0] = pos_x;
            m_fPosition[1] = pos_y;
            m_fPosition[2] = pos_z;
            m_fPosition[3] = rot_x;
            m_fPosition[4] = rot_y;
            m_fPosition[5] = rot_z;

            int gridX = (int)(pos_x / 32);
            int gridY = (int)(pos_y / 32);

            m_strGridID = ulMap.ToString() + "|" + gridX.ToString() + "|" + gridY.ToString();

            m_ulEntry = ulEntry;
            m_ulGroup = ulGroup;
            m_ulType = ulType;
            m_ulMap = ulMap;
        }

        private PointOfInterest_DB(ulong ulEntry, ulong ulGroup, ulong ulType, ulong ulMap)
        {
            m_ulEntry = ulEntry;
            m_ulGroup = ulGroup;
            m_ulType = ulType;
            m_ulMap = ulMap;
        }

    }
}
