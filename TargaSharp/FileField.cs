using System;
using TargaSharp.Helpers;

namespace TargaSharp
{
    /// <summary>
    /// A TGA file field 
    /// </summary>
    public abstract class FileField : ByteSized, IFileField, IByteConvert
    {
        /// <summary>
        /// Creates a new instance of the <see cref="FileField"/> class.
        /// </summary>
        /// <param name="size"></param>
        public FileField(uint size) : base(size) { }

        /// <summary>
        /// Creates a new instance of the <see cref="FileField"/> class.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="offset"></param>
        /// <param name="number"></param>
        public FileField(uint size, uint offset, uint number) : base(size)
        {
            Offset = offset;
            Number = number;  
        }

        /// <summary>
        /// Creates a new instance of the <see cref="FileField"/> class.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="offset"></param>
        /// <param name="number"></param>
        /// <param name="bytes"></param>
        protected FileField(uint size, uint offset, uint number, byte[] bytes) 
            : this(size, offset, number)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(bytes);
#else
            if (bytes is null) throw new ArgumentNullException(nameof(bytes));
#endif
            if (!ValidateByteLength(bytes)) throw new ArgumentException("Invalid byte array length"); 
            FromBytes(bytes);
        }
        
        /// <summary>
        /// The field number.
        /// </summary>
        public uint Number { get; private set; }

        /// <summary>
        /// The field offset position.
        /// </summary>
        public uint Offset { get; set; }


        /// <summary>
        /// Convert the field to bytes.
        /// </summary>
        /// <returns></returns>
        public abstract byte[]? ToBytes();


        /// <summary>
        /// Define how to create this object from bytes.
        /// </summary>
        /// <param name="bytes"></param>
        protected abstract void FromBytes(byte[] bytes);
        
        /// <summary>
        /// Given a byte array in the constructor, determine that the length is valid.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        protected virtual bool ValidateByteLength(byte[] bytes) => ByteSize == bytes.Length;
    }
}