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
