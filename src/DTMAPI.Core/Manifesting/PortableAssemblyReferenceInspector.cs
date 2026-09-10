using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using DTMAPI.Internal.Authoring;

namespace DTMAPI.Core.Manifesting
{
    /// <summary>Reads only the ECMA-335 AssemblyRef table and never loads the inspected assembly.</summary>
    internal static class PortableAssemblyReferenceInspector
    {
        private const uint PeSignature = 0x00004550;
        private const uint MetadataSignature = 0x424A5342;
        private const int AssemblyRefTable = 35;

        public static PortableAssemblyMetadata Inspect(string path)
        {
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
                    return Inspect(reader, stream.Length);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is BadImageFormatException || ex is OverflowException || ex is ArgumentException)
            {
                throw InvalidMetadata(ex);
            }
        }

        public static PortableAssemblyMetadata Inspect(byte[] image)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            try
            {
                using (var stream = new MemoryStream(image, writable: false))
                using (var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
                    return Inspect(reader, stream.Length);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is BadImageFormatException || ex is OverflowException || ex is ArgumentException)
            {
                throw InvalidMetadata(ex);
            }
        }

        private static ManagedModClassificationException InvalidMetadata(Exception ex) =>
            new ManagedModClassificationException(
                "entry-assembly-metadata-invalid",
                "Entry DLL metadata could not be inspected before load: " + ex.GetType().Name + ": " + ex.Message);

        private static PortableAssemblyMetadata Inspect(BinaryReader reader, long length)
        {
            if (length < 128 || ReadUInt16(reader, 0, length) != 0x5A4D)
                throw new BadImageFormatException("Entry DLL does not have an MZ header.");

            uint peOffsetValue = ReadUInt32(reader, 0x3c, length);
            long peOffset = peOffsetValue;
            if (ReadUInt32(reader, peOffset, length) != PeSignature)
                throw new BadImageFormatException("Entry DLL does not have a PE signature.");

            ushort sectionCount = ReadUInt16(reader, peOffset + 6, length);
            ushort optionalHeaderSize = ReadUInt16(reader, peOffset + 20, length);
            long optionalHeader = peOffset + 24;
            ushort magic = ReadUInt16(reader, optionalHeader, length);
            int dataDirectoryStart = magic == 0x10b ? 96 : magic == 0x20b ? 112 : 0;
            if (dataDirectoryStart == 0 || optionalHeaderSize < dataDirectoryStart + (15 * 8))
                throw new BadImageFormatException("Entry DLL has an unsupported PE optional header.");

            long cliDirectory = optionalHeader + dataDirectoryStart + (14 * 8);
            uint cliRva = ReadUInt32(reader, cliDirectory, length);
            uint cliSize = ReadUInt32(reader, cliDirectory + 4, length);
            if (cliRva == 0 || cliSize < 16)
                throw new BadImageFormatException("Entry DLL has no CLR metadata directory.");

            long sectionHeaders = optionalHeader + optionalHeaderSize;
            var sections = new List<PeSection>(sectionCount);
            for (int index = 0; index < sectionCount; index++)
            {
                long header = sectionHeaders + (index * 40L);
                sections.Add(new PeSection(
                    ReadUInt32(reader, header + 8, length),
                    ReadUInt32(reader, header + 12, length),
                    ReadUInt32(reader, header + 16, length),
                    ReadUInt32(reader, header + 20, length)));
            }

            long cliOffset = ResolveRva(cliRva, sections, length);
            uint metadataRva = ReadUInt32(reader, cliOffset + 8, length);
            uint metadataSize = ReadUInt32(reader, cliOffset + 12, length);
            long metadataOffset = ResolveRva(metadataRva, sections, length);
            EnsureRange(metadataOffset, metadataSize, length);
            return ReadMetadata(reader, metadataOffset, metadataSize, length);
        }

        private static PortableAssemblyMetadata ReadMetadata(BinaryReader reader, long metadataOffset, uint metadataSize, long fileLength)
        {
            long metadataEnd = checked(metadataOffset + metadataSize);
            if (ReadUInt32(reader, metadataOffset, fileLength) != MetadataSignature)
                throw new BadImageFormatException("CLR metadata signature is invalid.");
            uint versionLength = ReadUInt32(reader, metadataOffset + 12, fileLength);
            if (versionLength > 1024 * 1024)
                throw new BadImageFormatException("CLR metadata version string is unreasonably large.");
            long streamHeader = Align4(checked(metadataOffset + 16 + versionLength));
            ushort streamCount = ReadUInt16(reader, streamHeader + 2, fileLength);
            streamHeader += 4;
            if (streamCount == 0 || streamCount > 64)
                throw new BadImageFormatException("CLR metadata stream count is invalid.");

            MetadataStream? tables = null;
            MetadataStream? strings = null;
            MetadataStream? blobs = null;
            MetadataStream? guids = null;
            for (int index = 0; index < streamCount; index++)
            {
                uint offset = ReadUInt32(reader, streamHeader, fileLength);
                uint size = ReadUInt32(reader, streamHeader + 4, fileLength);
                string name = ReadStreamName(reader, streamHeader + 8, metadataEnd, out int nameBytes);
                streamHeader = Align4(streamHeader + 8 + nameBytes);
                long absolute = checked(metadataOffset + offset);
                EnsureRange(absolute, size, metadataEnd);
                var value = new MetadataStream(absolute, size);
                if (name == "#~" || name == "#-")
                    tables = value;
                else if (name == "#Strings")
                    strings = value;
                else if (name == "#Blob")
                    blobs = value;
                else if (name == "#GUID")
                    guids = value;
            }
            if (!tables.HasValue || !strings.HasValue || !blobs.HasValue || !guids.HasValue)
                throw new BadImageFormatException("CLR metadata is missing #~/#-, #Strings, #Blob, or #GUID.");

            MetadataStream tableStream = tables.Value;
            MetadataStream stringStream = strings.Value;
            MetadataStream blobStream = blobs.Value;
            MetadataStream guidStream = guids.Value;
            long cursor = tableStream.Offset;
            EnsureRange(cursor, 24, checked(tableStream.Offset + tableStream.Size));
            byte heapSizes = ReadByte(reader, cursor + 6, fileLength);
            ulong valid = ReadUInt64(reader, cursor + 8, fileLength);
            cursor += 24;

            var rowCounts = new uint[64];
            for (int table = 0; table < 64; table++)
            {
                if ((valid & (1UL << table)) == 0)
                    continue;
                rowCounts[table] = ReadUInt32(reader, cursor, fileLength);
                cursor += 4;
            }

            int stringIndexSize = (heapSizes & 0x01) != 0 ? 4 : 2;
            int guidIndexSize = (heapSizes & 0x02) != 0 ? 4 : 2;
            int blobIndexSize = (heapSizes & 0x04) != 0 ? 4 : 2;
            var tableOffsets = new long[64];
            long currentTableOffset = cursor;
            for (int table = 0; table <= 41; table++)
            {
                tableOffsets[table] = currentTableOffset;
                if (rowCounts[table] == 0)
                    continue;
                int rowSize = GetRowSize(table, rowCounts, stringIndexSize, guidIndexSize, blobIndexSize);
                currentTableOffset = checked(currentTableOffset + checked((long)rowCounts[table] * rowSize));
            }

            uint assemblyRefCount = rowCounts[AssemblyRefTable];
            int assemblyRefRowSize = 12 + blobIndexSize + stringIndexSize + stringIndexSize + blobIndexSize;
            long assemblyRefOffset = tableOffsets[AssemblyRefTable];
            EnsureRange(assemblyRefOffset, checked((long)assemblyRefCount * assemblyRefRowSize), checked(tableStream.Offset + tableStream.Size));
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var identities = new List<PackageAssemblyIdentity>();
            for (uint row = 0; row < assemblyRefCount; row++)
            {
                long nameIndexOffset = assemblyRefOffset + checked((long)row * assemblyRefRowSize) + 12 + blobIndexSize;
                uint nameIndex = ReadIndex(reader, nameIndexOffset, stringIndexSize, fileLength);
                string name = ReadHeapString(reader, stringStream, nameIndex, fileLength);
                if (name.Length == 0)
                    throw new BadImageFormatException("AssemblyRef contains an empty assembly name.");
                names.Add(name);
                long referenceOffset = assemblyRefOffset + checked((long)row * assemblyRefRowSize);
                identities.Add(ReadIdentity(reader, referenceOffset, definition: false, stringIndexSize, blobIndexSize, stringStream, blobStream, fileLength));
            }
            string targetFramework = ReadTargetFramework(
                reader,
                rowCounts,
                tableOffsets,
                stringIndexSize,
                blobIndexSize,
                stringStream,
                blobStream,
                fileLength);
            string assemblyName = ReadAssemblyName(reader, rowCounts, tableOffsets, stringIndexSize, blobIndexSize, stringStream, fileLength);
            string assemblyVersion = ReadAssemblyVersion(reader, rowCounts, tableOffsets, fileLength);
            string moduleMvid = ReadModuleMvid(reader, rowCounts, tableOffsets, stringIndexSize, guidIndexSize, guidStream, fileLength);
            return new PortableAssemblyMetadata(
                names.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray(),
                targetFramework,
                assemblyName,
                assemblyVersion,
                moduleMvid,
                ReadIdentity(reader, tableOffsets[32], definition: true, stringIndexSize, blobIndexSize, stringStream, blobStream, fileLength),
                identities, ReadDefinedTypes(reader, rowCounts, tableOffsets, stringIndexSize, guidIndexSize, blobIndexSize, stringStream, fileLength));
        }

        private static string[] ReadDefinedTypes(BinaryReader reader, uint[] rows, long[] offsets, int strings, int guids, int blobs, MetadataStream heap, long length)
        {
            var parents = new Dictionary<uint, uint>(); int indexSize = Table(rows, 2);
            for (uint row = 0; row < rows[41]; row++)
            {
                long offset = offsets[41] + (long)row * indexSize * 2;
                uint child = ReadIndex(reader, offset, indexSize, length), parent = ReadIndex(reader, offset + indexSize, indexSize, length);
                if (child == 0 || child > rows[2] || parent == 0 || parent > rows[2] || parents.ContainsKey(child)) throw new BadImageFormatException("Invalid nested type row.");
                parents.Add(child, parent);
            }
            int size = GetRowSize(2, rows, strings, guids, blobs);
            string Name(uint row, int depth)
            {
                if (depth > 64) throw new BadImageFormatException("Nested type depth/cycle.");
                long offset = offsets[2] + (long)(row - 1) * size + 4;
                string name = ReadHeapString(reader, heap, ReadIndex(reader, offset, strings, length), length);
                string ns = ReadHeapString(reader, heap, ReadIndex(reader, offset + strings, strings, length), length);
                return parents.TryGetValue(row, out uint parent) ? Name(parent, depth + 1) + "+" + name : ns.Length == 0 ? name : ns + "." + name;
            }
            return Enumerable.Range(1, checked((int)rows[2])).Select(row => Name((uint)row, 0)).ToArray();
        }

        private static PackageAssemblyIdentity ReadIdentity(BinaryReader reader, long offset, bool definition,
            int stringsSize, int blobsSize, MetadataStream strings, MetadataStream blobs, long length)
        {
            long version = offset + (definition ? 4 : 0);
            long flags = version + 8;
            long key = flags + 4;
            uint keyIndex = ReadIndex(reader, key, blobsSize, length);
            byte[] bytes = keyIndex == 0 ? Array.Empty<byte>() : ReadBlob(reader, blobs, keyIndex, length);
            if (bytes.Length > 0 && (definition || (ReadUInt32(reader, flags, length) & 1) != 0))
            {
                using (SHA1 sha = SHA1.Create()) bytes = sha.ComputeHash(bytes).Reverse().Take(8).ToArray();
            }
            if (bytes.Length != 0 && bytes.Length != 8) throw new BadImageFormatException("Assembly public key token must contain eight bytes.");
            return new PackageAssemblyIdentity
            {
                Name = ReadHeapString(reader, strings, ReadIndex(reader, key + blobsSize, stringsSize, length), length),
                AssemblyVersion = ReadUInt16(reader, version, length) + "." + ReadUInt16(reader, version + 2, length) + "." + ReadUInt16(reader, version + 4, length) + "." + ReadUInt16(reader, version + 6, length),
                Culture = ReadHeapString(reader, strings, ReadIndex(reader, key + blobsSize + stringsSize, stringsSize, length), length),
                PublicKeyToken = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant()
            };
        }

        private static string ReadAssemblyVersion(BinaryReader reader, uint[] rows, long[] tableOffsets, long fileLength)
        {
            if (rows[32] != 1)
                throw new BadImageFormatException("CodeMod entry metadata must contain exactly one Assembly row.");
            long offset = tableOffsets[32] + 4;
            return ReadUInt16(reader, offset, fileLength) + "." +
                ReadUInt16(reader, offset + 2, fileLength) + "." +
                ReadUInt16(reader, offset + 4, fileLength) + "." +
                ReadUInt16(reader, offset + 6, fileLength);
        }

        private static string ReadModuleMvid(
            BinaryReader reader,
            uint[] rows,
            long[] tableOffsets,
            int stringIndexSize,
            int guidIndexSize,
            MetadataStream guids,
            long fileLength)
        {
            if (rows[0] != 1)
                throw new BadImageFormatException("CodeMod entry metadata must contain exactly one Module row.");
            long mvidIndexOffset = tableOffsets[0] + 2 + stringIndexSize;
            uint mvidIndex = ReadIndex(reader, mvidIndexOffset, guidIndexSize, fileLength);
            if (mvidIndex == 0)
                throw new BadImageFormatException("CodeMod entry Module MVID is missing.");
            long offset = checked(guids.Offset + checked((long)(mvidIndex - 1) * 16));
            EnsureRange(offset, 16, checked(guids.Offset + guids.Size));
            reader.BaseStream.Position = offset;
            byte[] bytes = reader.ReadBytes(16);
            if (bytes.Length != 16)
                throw new EndOfStreamException("CodeMod entry Module MVID is truncated.");
            return new Guid(bytes).ToString("D");
        }

        private static string ReadAssemblyName(
            BinaryReader reader,
            uint[] rows,
            long[] tableOffsets,
            int stringIndexSize,
            int blobIndexSize,
            MetadataStream strings,
            long fileLength)
        {
            if (rows[32] != 1)
                throw new BadImageFormatException("CodeMod entry metadata must contain exactly one Assembly row.");
            long nameOffset = tableOffsets[32] + 16 + blobIndexSize;
            uint nameIndex = ReadIndex(reader, nameOffset, stringIndexSize, fileLength);
            string name = ReadHeapString(reader, strings, nameIndex, fileLength);
            if (name.Length == 0)
                throw new BadImageFormatException("CodeMod entry Assembly name is empty.");
            return name;
        }

        private static string ReadTargetFramework(
            BinaryReader reader,
            uint[] rows,
            long[] tableOffsets,
            int stringIndexSize,
            int blobIndexSize,
            MetadataStream strings,
            MetadataStream blobs,
            long fileLength)
        {
            int parentSize = Coded(rows, 5, 6, 4, 1, 2, 8, 9, 10, 0, 14, 23, 20, 17, 26, 27, 32, 35, 38, 39, 40, 42, 44, 43);
            int attributeTypeSize = Coded(rows, 3, 6, 10);
            int rowSize = parentSize + attributeTypeSize + blobIndexSize;
            for (uint row = 1; row <= rows[12]; row++)
            {
                long offset = tableOffsets[12] + checked((long)(row - 1) * rowSize);
                uint parent = ReadIndex(reader, offset, parentSize, fileLength);
                if ((parent & 31) != 14 || (parent >> 5) != 1) // Assembly row 1
                    continue;
                uint constructor = ReadIndex(reader, offset + parentSize, attributeTypeSize, fileLength);
                if ((constructor & 7) != 3) // MemberRef
                    continue;
                uint memberRefRow = constructor >> 3;
                if (memberRefRow == 0 || memberRefRow > rows[10])
                    throw new BadImageFormatException("TargetFramework custom attribute has an invalid constructor MemberRef.");
                if (!IsTargetFrameworkConstructor(reader, rows, tableOffsets, memberRefRow, stringIndexSize, blobIndexSize, strings, fileLength))
                    continue;
                uint blobIndex = ReadIndex(reader, offset + parentSize + attributeTypeSize, blobIndexSize, fileLength);
                return ReadTargetFrameworkValue(reader, blobs, blobIndex, fileLength);
            }
            return string.Empty;
        }

        private static bool IsTargetFrameworkConstructor(
            BinaryReader reader,
            uint[] rows,
            long[] tableOffsets,
            uint memberRefRow,
            int stringIndexSize,
            int blobIndexSize,
            MetadataStream strings,
            long fileLength)
        {
            int parentSize = Coded(rows, 3, 2, 1, 26, 6, 27);
            int memberRefRowSize = parentSize + stringIndexSize + blobIndexSize;
            long memberOffset = tableOffsets[10] + checked((long)(memberRefRow - 1) * memberRefRowSize);
            uint parent = ReadIndex(reader, memberOffset, parentSize, fileLength);
            uint nameIndex = ReadIndex(reader, memberOffset + parentSize, stringIndexSize, fileLength);
            if (!ReadHeapString(reader, strings, nameIndex, fileLength).Equals(".ctor", StringComparison.Ordinal))
                return false;
            if ((parent & 7) != 1) // TypeRef
                return false;
            uint typeRefRow = parent >> 3;
            if (typeRefRow == 0 || typeRefRow > rows[1])
                throw new BadImageFormatException("Custom-attribute MemberRef has an invalid TypeRef parent.");

            int resolutionScopeSize = Coded(rows, 2, 0, 26, 35, 1);
            int typeRefRowSize = resolutionScopeSize + (2 * stringIndexSize);
            long typeOffset = tableOffsets[1] + checked((long)(typeRefRow - 1) * typeRefRowSize);
            uint typeNameIndex = ReadIndex(reader, typeOffset + resolutionScopeSize, stringIndexSize, fileLength);
            uint namespaceIndex = ReadIndex(reader, typeOffset + resolutionScopeSize + stringIndexSize, stringIndexSize, fileLength);
            string typeName = ReadHeapString(reader, strings, typeNameIndex, fileLength);
            string typeNamespace = ReadHeapString(reader, strings, namespaceIndex, fileLength);
            return typeName.Equals("TargetFrameworkAttribute", StringComparison.Ordinal) &&
                typeNamespace.Equals("System.Runtime.Versioning", StringComparison.Ordinal);
        }

        private static string ReadTargetFrameworkValue(BinaryReader reader, MetadataStream blobs, uint blobIndex, long fileLength)
        {
            byte[] payload = ReadBlob(reader, blobs, blobIndex, fileLength);
            if (payload.Length < 3 || payload[0] != 1 || payload[1] != 0)
                throw new BadImageFormatException("TargetFrameworkAttribute has an invalid custom-attribute prolog.");
            int cursor = 2;
            if (payload[cursor] == 0xff)
                throw new BadImageFormatException("TargetFrameworkAttribute contains a null framework name.");
            uint byteCount = ReadCompressedUInt32(payload, ref cursor);
            if (byteCount > int.MaxValue || cursor > payload.Length || byteCount > payload.Length - cursor)
                throw new BadImageFormatException("TargetFrameworkAttribute framework name is truncated.");
            return new UTF8Encoding(false, true).GetString(payload, cursor, checked((int)byteCount));
        }

        private static byte[] ReadBlob(BinaryReader reader, MetadataStream blobs, uint index, long fileLength)
        {
            if (index == 0 || index >= blobs.Size)
                throw new BadImageFormatException("Custom-attribute blob index is outside #Blob.");
            long offset = checked(blobs.Offset + index);
            byte first = ReadByte(reader, offset, fileLength);
            int prefixBytes;
            uint size;
            if ((first & 0x80) == 0)
            {
                prefixBytes = 1;
                size = first;
            }
            else if ((first & 0xc0) == 0x80)
            {
                prefixBytes = 2;
                size = (uint)(((first & 0x3f) << 8) | ReadByte(reader, offset + 1, fileLength));
            }
            else if ((first & 0xe0) == 0xc0)
            {
                prefixBytes = 4;
                size = (uint)(((first & 0x1f) << 24) |
                    (ReadByte(reader, offset + 1, fileLength) << 16) |
                    (ReadByte(reader, offset + 2, fileLength) << 8) |
                    ReadByte(reader, offset + 3, fileLength));
            }
            else
            {
                throw new BadImageFormatException("#Blob contains an invalid compressed length.");
            }
            long payloadOffset = offset + prefixBytes;
            long blobEnd = checked(blobs.Offset + blobs.Size);
            EnsureRange(payloadOffset, size, blobEnd);
            if (size > 1024 * 1024)
                throw new BadImageFormatException("Custom-attribute blob is unreasonably large.");
            reader.BaseStream.Position = payloadOffset;
            byte[] payload = reader.ReadBytes(checked((int)size));
            if (payload.Length != size)
                throw new EndOfStreamException("Custom-attribute blob is truncated.");
            return payload;
        }

        private static uint ReadCompressedUInt32(byte[] bytes, ref int cursor)
        {
            if (cursor >= bytes.Length)
                throw new BadImageFormatException("Serialized string length is missing.");
            byte first = bytes[cursor++];
            if ((first & 0x80) == 0)
                return first;
            if ((first & 0xc0) == 0x80)
            {
                if (cursor >= bytes.Length)
                    throw new BadImageFormatException("Serialized string length is truncated.");
                return (uint)(((first & 0x3f) << 8) | bytes[cursor++]);
            }
            if ((first & 0xe0) == 0xc0)
            {
                if (cursor + 3 > bytes.Length)
                    throw new BadImageFormatException("Serialized string length is truncated.");
                return (uint)(((first & 0x1f) << 24) | (bytes[cursor++] << 16) | (bytes[cursor++] << 8) | bytes[cursor++]);
            }
            throw new BadImageFormatException("Serialized string length is invalid.");
        }

        private static int GetRowSize(int table, uint[] rows, int strings, int guids, int blobs)
        {
            switch (table)
            {
                case 0: return 2 + strings + (3 * guids); // Module
                case 1: return Coded(rows, 2, 0, 26, 35, 1) + (2 * strings); // TypeRef
                case 2: return 4 + (2 * strings) + Coded(rows, 2, 2, 1, 27) + Table(rows, 4) + Table(rows, 6); // TypeDef
                case 3: return Table(rows, 4);
                case 4: return 2 + strings + blobs;
                case 5: return Table(rows, 6);
                case 6: return 8 + strings + blobs + Table(rows, 8);
                case 7: return Table(rows, 8);
                case 8: return 4 + strings;
                case 9: return Table(rows, 2) + Coded(rows, 2, 2, 1, 27);
                case 10: return Coded(rows, 3, 2, 1, 26, 6, 27) + strings + blobs;
                case 11: return 2 + Coded(rows, 2, 4, 8, 23) + blobs;
                case 12: return Coded(rows, 5, 6, 4, 1, 2, 8, 9, 10, 0, 14, 23, 20, 17, 26, 27, 32, 35, 38, 39, 40, 42, 44, 43) + Coded(rows, 3, 6, 10) + blobs;
                case 13: return Coded(rows, 1, 4, 8) + blobs;
                case 14: return 2 + Coded(rows, 2, 2, 6, 32) + blobs;
                case 15: return 6 + Table(rows, 2);
                case 16: return 4 + Table(rows, 4);
                case 17: return blobs;
                case 18: return Table(rows, 2) + Table(rows, 20);
                case 19: return Table(rows, 20);
                case 20: return 2 + strings + Coded(rows, 2, 2, 1, 27);
                case 21: return Table(rows, 2) + Table(rows, 23);
                case 22: return Table(rows, 23);
                case 23: return 2 + strings + blobs;
                case 24: return 2 + Table(rows, 6) + Coded(rows, 1, 20, 23);
                case 25: return Table(rows, 2) + (2 * Coded(rows, 1, 6, 10));
                case 26: return strings;
                case 27: return blobs;
                case 28: return 2 + Coded(rows, 1, 4, 6) + strings + Table(rows, 26);
                case 29: return 4 + Table(rows, 4);
                case 30: return 8;
                case 31: return 4;
                case 32: return 16 + blobs + (2 * strings);
                case 33: return 4;
                case 34: return 12;
                case 35: return 12 + (2 * blobs) + (2 * strings);
                case 36: return 4 + Table(rows, 35);
                case 37: return 12 + Table(rows, 35);
                case 38: return 4 + strings + blobs;
                case 39: return 8 + (2 * strings) + Coded(rows, 2, 38, 35, 39);
                case 40: return 8 + strings + Coded(rows, 2, 38, 35, 39);
                case 41: return 2 * Table(rows, 2);
                default: throw new BadImageFormatException("Unsupported CLR metadata table " + table + ".");
            }
        }

        private static int Table(uint[] rows, int table) => rows[table] < 65536 ? 2 : 4;

        private static int Coded(uint[] rows, int tagBits, params int[] tables)
        {
            uint maximum = tables.Select(table => rows[table]).DefaultIfEmpty().Max();
            return maximum < (1u << (16 - tagBits)) ? 2 : 4;
        }

        private static long ResolveRva(uint rva, IEnumerable<PeSection> sections, long fileLength)
        {
            foreach (PeSection section in sections)
            {
                ulong width = Math.Max(section.VirtualSize, section.RawSize);
                if ((ulong)rva < section.VirtualAddress || (ulong)rva >= section.VirtualAddress + width)
                    continue;
                ulong delta = (ulong)rva - section.VirtualAddress;
                if (delta >= section.RawSize)
                    break;
                long offset = checked((long)(section.RawPointer + delta));
                EnsureRange(offset, 1, fileLength);
                return offset;
            }
            throw new BadImageFormatException("CLR RVA does not map to a file-backed PE section.");
        }

        private static string ReadStreamName(BinaryReader reader, long offset, long end, out int bytesRead)
        {
            var bytes = new List<byte>(16);
            for (bytesRead = 0; bytesRead < 32 && offset + bytesRead < end; bytesRead++)
            {
                byte value = ReadByte(reader, offset + bytesRead, end);
                if (value == 0)
                {
                    bytesRead++;
                    return Encoding.ASCII.GetString(bytes.ToArray());
                }
                bytes.Add(value);
            }
            throw new BadImageFormatException("CLR metadata stream name is not terminated.");
        }

        private static string ReadHeapString(BinaryReader reader, MetadataStream strings, uint index, long fileLength)
        {
            if (index >= strings.Size)
                throw new BadImageFormatException("AssemblyRef string index is outside #Strings.");
            long start = checked(strings.Offset + index);
            long end = checked(strings.Offset + strings.Size);
            var bytes = new List<byte>(64);
            for (long cursor = start; cursor < end && bytes.Count < 4096; cursor++)
            {
                byte value = ReadByte(reader, cursor, fileLength);
                if (value == 0)
                    return new UTF8Encoding(false, true).GetString(bytes.ToArray());
                bytes.Add(value);
            }
            throw new BadImageFormatException("AssemblyRef string is not terminated or is too large.");
        }

        private static byte ReadByte(BinaryReader reader, long offset, long length)
        {
            EnsureRange(offset, 1, length);
            reader.BaseStream.Position = offset;
            return reader.ReadByte();
        }

        private static ushort ReadUInt16(BinaryReader reader, long offset, long length)
        {
            EnsureRange(offset, 2, length);
            reader.BaseStream.Position = offset;
            return reader.ReadUInt16();
        }

        private static uint ReadUInt32(BinaryReader reader, long offset, long length)
        {
            EnsureRange(offset, 4, length);
            reader.BaseStream.Position = offset;
            return reader.ReadUInt32();
        }

        private static ulong ReadUInt64(BinaryReader reader, long offset, long length)
        {
            EnsureRange(offset, 8, length);
            reader.BaseStream.Position = offset;
            return reader.ReadUInt64();
        }

        private static uint ReadIndex(BinaryReader reader, long offset, int size, long length) =>
            size == 2 ? ReadUInt16(reader, offset, length) : ReadUInt32(reader, offset, length);

        private static long Align4(long value) => checked((value + 3) & ~3L);

        private static void EnsureRange(long offset, long size, long length)
        {
            if (offset < 0 || size < 0 || offset > length || size > length - offset)
                throw new BadImageFormatException("Portable assembly metadata points outside the file.");
        }

        private readonly struct PeSection
        {
            public PeSection(uint virtualSize, uint virtualAddress, uint rawSize, uint rawPointer)
            {
                VirtualSize = virtualSize;
                VirtualAddress = virtualAddress;
                RawSize = rawSize;
                RawPointer = rawPointer;
            }

            public uint VirtualSize { get; }
            public uint VirtualAddress { get; }
            public uint RawSize { get; }
            public uint RawPointer { get; }
        }

        private readonly struct MetadataStream
        {
            public MetadataStream(long offset, uint size)
            {
                Offset = offset;
                Size = size;
            }

            public long Offset { get; }
            public uint Size { get; }
        }
    }

    internal sealed class PortableAssemblyMetadata
    {
        public PortableAssemblyMetadata(IReadOnlyList<string> assemblyReferences, string targetFramework, string assemblyName, string assemblyVersion, string moduleMvid,
            PackageAssemblyIdentity? identity = null, IReadOnlyList<PackageAssemblyIdentity>? referenceIdentities = null, IReadOnlyList<string>? definedTypes = null)
        {
            AssemblyReferences = assemblyReferences ?? Array.Empty<string>();
            TargetFramework = targetFramework ?? string.Empty;
            AssemblyName = assemblyName ?? string.Empty;
            AssemblyVersion = assemblyVersion ?? string.Empty;
            ModuleMvid = moduleMvid ?? string.Empty;
            Identity = identity ?? new PackageAssemblyIdentity { Name = AssemblyName, AssemblyVersion = AssemblyVersion };
            ReferenceIdentities = referenceIdentities ?? Array.Empty<PackageAssemblyIdentity>();
            DefinedTypes = definedTypes ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> AssemblyReferences { get; }
        public string TargetFramework { get; }
        public string AssemblyName { get; }
        public string AssemblyVersion { get; }
        public string ModuleMvid { get; }
        public PackageAssemblyIdentity Identity { get; }
        public IReadOnlyList<PackageAssemblyIdentity> ReferenceIdentities { get; }
        public IReadOnlyList<string> DefinedTypes { get; }
    }
}
