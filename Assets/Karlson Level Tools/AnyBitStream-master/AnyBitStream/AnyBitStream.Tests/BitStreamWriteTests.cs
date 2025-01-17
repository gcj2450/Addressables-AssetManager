
namespace AnyBitStream.Tests
{
    public class BitStreamWriteTests
    {
        public void Should_WriteBits()
        {
            var stream = new BitStream(true);
            stream.WriteBits(2, 2);
            stream.WriteBits(7, 4);
            //Assert.AreEqual(2 + 4, stream.BitsPosition);
            var bytes = stream.ToArray();
            // 01 + 1110 = 30
            //Assert.AreEqual(new byte[] { 30 }, bytes);
        }

        public void Should_WriteManyBits()
        {
            var stream = new BitStream(true);
            stream.WriteBits(2, 2);
            stream.WriteBits(7, 4);
            stream.WriteBits(3301, 12);
            stream.WriteBits(933, 10);
            stream.WriteBits(29, 5);
            stream.WriteBits(13, 5);
            // 38 bits total
            //Assert.AreEqual(6, stream.BitsPosition);
            //Assert.AreEqual(4, stream.Position);
            var bytes = stream.ToArray();
            // 01 + 1110 + 1010 0111 0011 + 1010 0101 11 + 1011 1 + 1011 0
            // byte 0 = 0111 1010 = 94
            // byte 1 = 1001 1100 = 57
            // byte 2 = 1110 1001 = 151
            // byte 3 = 0111 1011 = 222
            // byte 4 = 1101 1000 = 27
            //Assert.AreEqual(new byte[] { 94, 57, 151, 222, 27 }, bytes);
        }

        public void Should_WriteCustomBits()
        {
            var stream = new BitStream(true);
            stream.Write((UInt2)2);
            stream.Write((UInt4)7);
            //Assert.AreEqual(UInt2.BitSize + UInt4.BitSize, stream.BitsPosition);
            var bytes = stream.ToArray();
            // 01 + 1110 = 30
            //Assert.AreEqual(new byte[] { 30 }, bytes);
        }

        public void Should_WriteManyCustomBits()
        {
            var stream = new BitStream(true);
            stream.Write((UInt2)2);
            stream.Write((UInt4)7);
            stream.Write((UInt12)3301);
            stream.Write((UInt10)933);
            stream.Write((UInt5)29);
            stream.Write((UInt5)13);
            // 38 bits total
            //Assert.AreEqual(6, stream.BitsPosition);
            //Assert.AreEqual(4, stream.Position);
            // 5 bytes total
            var bytes = stream.ToArray();
            // 01 + 1110 + 1010 0111 0011 + 1010 0101 11 + 1011 1 + 1011 0
            // byte 0 = 0111 1010 = 94
            // byte 1 = 1001 1100 = 57
            // byte 2 = 1110 1001 = 151
            // byte 3 = 0111 1011 = 222
            // byte 4 = 1101 1000 = 27
            //Assert.AreEqual(new byte[] { 94, 57, 151, 222, 27 }, bytes);
        }

        public void Should_WriteBitsThenBytes()
        {
            var stream = new BitStream(true);
            stream.Write((UInt2)2);
            stream.Write((UInt4)7);
            // 6 bits written
            //Assert.AreEqual(UInt2.BitSize + UInt4.BitSize, stream.BitsPosition);
            // write a full byte. We are unaligned, so the bits will be flushed first.
            stream.WriteByte(0xFF);

            var bytes = stream.ToArray();
            // 01 + 1110 + 11 = 222
            // 1111 11-- = 63 (remainder is filled in)
            //Assert.AreEqual(new byte[] { 222, 63 }, bytes);
        }

        public void ShouldNot_WriteBitsThenBytes()
        {
            var stream = new BitStream();
            stream.Write((UInt2)2);
            stream.Write((UInt4)7);
            // 6 bits written
            //Assert.AreEqual(UInt2.BitSize + UInt4.BitSize, stream.BitsPosition);
            // we are unaligned, this should fail
            //Assert.Throws<StreamUnalignedException>(() => stream.WriteByte(0xFF));
        }

        public void Should_Align()
        {
            var stream = new BitStream();
            stream.Write((UInt2)2);
            stream.Write((UInt4)7);
            // 6 bits written
            //Assert.AreEqual(UInt2.BitSize + UInt4.BitSize, stream.BitsPosition);
            // align the bits (write 2 0 bits)
            stream.Align();
            // write a full byte.
            stream.WriteByte(0xFF);

            var bytes = stream.ToArray();
            // 01 + 1110 = 30
            // 1111 1111 = 255
            //Assert.AreEqual(new byte[] { 30, 255 }, bytes);
        }
    }
}
