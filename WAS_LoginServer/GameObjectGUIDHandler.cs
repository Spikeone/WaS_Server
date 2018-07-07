using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WAS_LoginServer
{
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
}
