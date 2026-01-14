using System;

namespace LWFMS
{
    public class FSMTypeAttribute : Attribute
    {
        public string FSMName;
        public bool isFirst;
        public FSMTypeAttribute(string FSMName, bool isFirst)
        {
            this.FSMName = FSMName;
            this.isFirst = isFirst;
        }


    }
}


