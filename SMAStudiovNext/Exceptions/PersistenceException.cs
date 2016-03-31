using System;

namespace SMAStudiovNext.Exceptions
{
    [Serializable]
    public class PersistenceException : Exception
    {
        public PersistenceException(string message)
            : base(message)
        {

        }
    }
}
