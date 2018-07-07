using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WAS_LoginServer
{
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
