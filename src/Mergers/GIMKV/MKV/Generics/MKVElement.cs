using System.Text;

namespace GICutscenes.Mergers.GIMKV.MKV.Generics
{
    internal class MKVElement<T>
    {
        private readonly byte[] _signature;
        private readonly T _data;

        public MKVElement(byte[] signature, T data)
        {
            _signature = signature;
            _data = data;
        }

        public byte[] ToBytes()
        {
            List<byte> fieldBytes = new List<byte>(_signature);
            byte[] fieldData = DataToBytes();
            if (_data.GetType().IsPrimitive)
            {
                Array.Reverse(fieldData);
                fieldData = GIMKV.TrimZeroes(fieldData);
            }
            byte[] fieldLength = GIMKV.FieldLength((uint)fieldData.Length);
            fieldBytes.AddRange(fieldLength);
            fieldBytes.AddRange(fieldData);
            return fieldBytes.ToArray();
        }

        private byte[] DataToBytes()
        {
            return _data switch
            {
                string s => Encoding.UTF8.GetBytes(s),  // TODO: Verify if encoding is alright
                byte[] d => d,
                // Basic fields
                byte b => new[] { b },
                char c => BitConverter.GetBytes(c),
                ushort s => BitConverter.GetBytes(s),
                short s => BitConverter.GetBytes(s),
                int s => BitConverter.GetBytes(s),
                uint s => BitConverter.GetBytes(s),
                long s => BitConverter.GetBytes(s),
                ulong s => BitConverter.GetBytes(s),
                float s => BitConverter.GetBytes(s),
                double s => BitConverter.GetBytes(s),
                _ => throw new Exception($"Primitive data type {_data.GetType().Name} is not supported by the class MKVElement<T>")
            };
        }
    }
}