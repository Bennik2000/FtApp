namespace TXTCommunication.Fischertechnik.Txt.Response
{
    public abstract class ResponseBase
    {
        public uint ResponseId { get; set; }

        public virtual int GetResponseLength()
        {
            return sizeof(uint);
        }

        public abstract void FromByteArray(byte[] bytes);
    }
}