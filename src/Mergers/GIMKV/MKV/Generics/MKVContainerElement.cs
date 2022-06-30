using System.Diagnostics.CodeAnalysis;

namespace GICutscenes.Mergers.GIMKV.MKV.Generics
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] // Resolving Trim analysis warning 
    internal abstract class MKVContainerElement: IMKVToBytes
    {
        public readonly byte[] _signature;

        protected MKVContainerElement(byte[] signature)
        {
            _signature = signature;
        }

        public virtual byte[] ToBytes()
        {
            List<byte> byteRes = new();
            foreach (IMKVToBytes field in GetType().GetFields().Select(f => f.GetValue(this)).Where(f => f != null && f.GetType() != typeof(byte[]))) // Iterating through every mkvelement in variable
            {
                //MethodInfo method = field.GetType().GetMethod("ToBytes") ?? throw new MissingMethodException($"{field.GetType().Name} doesn't have any ToBytes method.");
                //(byte[])method.Invoke(field, Array.Empty<object>()) ?? throw new MethodAccessException($"Unable to invoke the ToBytes method of the class {field.GetType().Name}.");
                byte[] res = field.ToBytes();
                byteRes.AddRange(res);
            }
            byteRes.InsertRange(0, GIMKV.FieldLength((uint)byteRes.Count));
            byteRes.InsertRange(0, _signature);
            return byteRes.ToArray();
        }
    }
}