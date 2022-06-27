using System.Reflection;

namespace GICutscenes.Mergers.GIMKV.MKV.Generics
{
    internal abstract class MKVContainerElement
    {
        public readonly byte[] _signature;

        protected MKVContainerElement(byte[] signature)
        {
            _signature = signature;
        }

        public virtual byte[] ToBytes()
        {
            List<byte> byteRes = new List<byte>();
            foreach (var field in GetType().GetFields().Select(f => f.GetValue(this)).Where(f => f != null && f.GetType() != typeof(byte[]))) // Iterating through every mkvelement in variable
            {
                MethodInfo method = field.GetType().GetMethod("ToBytes") ?? throw new Exception($"{field.GetType().Name} doesn't have any ToBytes method.");
                byte[] res = (byte[])method.Invoke(field, Array.Empty<object>()) ?? throw new Exception($"Unable to invoke the ToBytes method of the class {field.GetType().Name}.");
                byteRes.AddRange(res);
            }
            byteRes.InsertRange(0, GIMKV.FieldLength((uint)byteRes.Count));
            byteRes.InsertRange(0, _signature);
            return byteRes.ToArray();
        }
    }
}