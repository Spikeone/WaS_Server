using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WAS_LoginServer
{
    public class Grid
    {
        private string strGridID;
        private int[] iPosition = new int[2];
        private ulong ulMapId;
        private List<ulong> ulPlayersLoaded;

        public Grid(ulong ulMapId, int posX, int posY)
        {
            iPosition[0] = posX;
            iPosition[1] = posY;
            this.ulMapId = ulMapId;

            strGridID = ulMapId.ToString() + "|" + iPosition[0].ToString() + "|" + iPosition[1].ToString();

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

        public void getPosition(ref int posx, ref int posy)
        {
            posx = iPosition[0];
            posy = iPosition[1];
        }

        public string getStringID()
        {
            return strGridID;
        }
    }
}
