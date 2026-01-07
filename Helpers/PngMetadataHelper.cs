using System.Text;

namespace ComfyGrimoire.Helpers;

public class PngMetadataHelper
{
    public static Dictionary<string, string> ReadMetadata(Stream stream)
    {
        var metadata = new Dictionary<string, string>();
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        // Check PNG signature
        var signature = reader.ReadBytes(8);
        if (!signature.SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
        {
            return metadata;
        }

        while (stream.Position < stream.Length)
        {
            var lengthBytes = reader.ReadBytes(4);
            if (lengthBytes.Length < 4) break;
            Array.Reverse(lengthBytes); // PNG is Big Endian
            var length = BitConverter.ToUInt32(lengthBytes, 0);

            var typeBytes = reader.ReadBytes(4);
            var type = Encoding.ASCII.GetString(typeBytes);

            if (type == "tEXt")
            {
                var data = reader.ReadBytes((int)length);
                var nullSeparatorIndex = Array.IndexOf(data, (byte)0);
                if (nullSeparatorIndex > 0)
                {
                    var key = Encoding.Latin1.GetString(data, 0, nullSeparatorIndex);
                    var value = Encoding.Latin1.GetString(data, nullSeparatorIndex + 1, data.Length - nullSeparatorIndex - 1);
                    metadata[key] = value;
                }
            }
            else if (type == "iTXt")
            {
                 // iTXt structure:
                 // Keyword:             1-79 bytes (character string)
                 // Null separator:      1 byte
                 // Compression flag:    1 byte
                 // Compression method:  1 byte
                 // Language tag:        0 or more bytes (character string)
                 // Null separator:      1 byte
                 // Translated keyword:  0 or more bytes (UTF-8 string)
                 // Null separator:      1 byte
                 // Text:                0 or more bytes (UTF-8 string)

                var data = reader.ReadBytes((int)length);
                var ms = new MemoryStream(data);
                using var chunkReader = new BinaryReader(ms);
                
                var keywordBytes = new List<byte>();
                while(true) {
                    var b = chunkReader.ReadByte();
                    if (b == 0) break;
                    keywordBytes.Add(b);
                }
                var key = Encoding.UTF8.GetString(keywordBytes.ToArray());

                var compressionFlag = chunkReader.ReadByte();
                var compressionMethod = chunkReader.ReadByte();
                
                // Skip Language tag
                while(chunkReader.ReadByte() != 0) {}
                
                // Skip Translated keyword
                while(chunkReader.ReadByte() != 0) {}

                var textBytes = chunkReader.ReadBytes((int)(ms.Length - ms.Position));
                var text = Encoding.UTF8.GetString(textBytes); // Assumption: not compressed

                 if (compressionFlag == 0)
                 {
                    metadata[key] = text;
                 }
                 else 
                 {
                     // Compression support omitted for simplicity as ComfyUI mostly uses tEXt or uncompressed iTXt(unlikely for prompts but possible)
                     // If needed, DEFLATE decompression logic should be added here.
                     metadata[key] = "[Compressed Data - Not Supported]";
                 }
            }
            else
            {
                stream.Seek(length, SeekOrigin.Current);
            }

            // CRC
            stream.Seek(4, SeekOrigin.Current);

            if (type == "IEND") break;
        }

        return metadata;
    }
}
