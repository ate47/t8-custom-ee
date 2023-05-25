using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace T8CustomEE
{
    internal class Library
    {
        internal class ReplaceData
        {
            internal ulong Script;
            internal uint Function;
            internal byte[] Data;
            internal bool Injected = false;
            internal byte[] PreviousInjection = Array.Empty<byte>();

            public ReplaceData(ulong script, uint function, byte[] data)
            {
                Script = script;
                Function = function;
                Data = data;
            }
        }

        internal static int LastPID = 0;


        internal static ReplaceData[] ReplaceScripts = {
            new ReplaceData(T8s64Hash("scripts/zm_common/zm_utility.csc"), 0xe51dc2d8, new byte[]{ 0x0d, 0x00, 0x8a, 0x01, 0x01, 0x00, 0x3c, 0x00 }),
            new ReplaceData(T8s64Hash("scripts/zm_common/zm_utility.gsc"), 0xe51dc2d8, new byte[]{ 0x0d, 0x00, 0x8a, 0x01, 0x01, 0x00, 0x3c, 0x00 })
        };

        public static bool Reverse(ref string Message)
        {
            ProcessEx bo4 = "blackops4";
            if (bo4 is null)
            {
                foreach (ReplaceData replacedData in ReplaceScripts)
                {
                    replacedData.Injected = false;
                }
                Message = "No game process found for Black Ops 4.";
                LastPID = 0;
                return true;
            }

            if (LastPID != bo4.BaseProcess.Id)
            {
                foreach (ReplaceData replacedData in ReplaceScripts)
                {
                    replacedData.Injected = false;
                }
                Message = "Not the same Black Ops 4 instance, ignoring.";
                return true;
            }

            bo4.OpenHandle();

            try
            {
                Console.WriteLine($"s_assetPool:ScriptParseTree => {bo4[0x912ABB0]}");
                var sptGlob = bo4.GetValue<ulong>(bo4[0x912ABB0]);
                var sptCount = bo4.GetValue<int>(bo4[0x912ABB0 + 0x14]);
                var SPTEntries = bo4.GetArray<T8SPT>(sptGlob, sptCount);

                // the script to inject

                int reverted = 0;

                for (int i = 0; i < SPTEntries.Length; i++)
                {
                    var spt = SPTEntries[i];


                    var vm = bo4.GetValue<byte>(spt.Buffer + 0x7);

                    if (vm != 0x36) // latest version of BO4 is VM36
                    {
                        continue;
                    }

                    foreach (ReplaceData replacedData in ReplaceScripts)
                    {
                        if (!replacedData.Injected)
                        {
                            continue; // not injected
                        }
                        if (replacedData.Script != spt.ScriptName)
                        {
                            continue;
                        }
                        var exportCount = bo4.GetValue<ushort>(spt.Buffer + 0x1E);
                        var exportStart = bo4.GetValue<uint>(spt.Buffer + 0x30);
                        var ExportEntries = bo4.GetArray<T8Export>(spt.Buffer + exportStart, (int)exportCount);

                        for (int j = 0; j < ExportEntries.Length; j++)
                        {
                            var export = ExportEntries[j];

                            if (!(export.FunctionID == replacedData.Function))
                            {
                                continue; // not our function
                            }
                            var ByteCodeAddress = spt.Buffer + export.ByteCodeAddress;

                            // we know this thing can contain a return true;

                            bo4.SetBytes(ByteCodeAddress, replacedData.PreviousInjection);
                            replacedData.Injected = false;

                            reverted++;
                        }
                    }
                }
                if (reverted == 0)
                {
                    Message = "Nothing to revert";
                    return true;

                }
                Message = "Reverted " + reverted;
                return true;
            }
            finally
            {
                bo4.CloseHandle();
            }
        }


        public static bool Inject(ref string Message)
        { 
            ProcessEx bo4 = "blackops4";
            if (bo4 is null)
            {
                Message = "No game process found for Black Ops 4.";
                return false;
            }

            foreach (ReplaceData replacedData in ReplaceScripts)
            {
                replacedData.Injected = false;
            }

            LastPID = bo4.BaseProcess.Id;

            bo4.OpenHandle();
            bool closed = false;

            try
            {
                Console.WriteLine($"s_assetPool:ScriptParseTree => {bo4[0x912ABB0]}");
                var sptGlob = bo4.GetValue<ulong>(bo4[0x912ABB0]);
                var sptCount = bo4.GetValue<int>(bo4[0x912ABB0 + 0x14]);
                var SPTEntries = bo4.GetArray<T8SPT>(sptGlob, sptCount);

                // the script to inject

                int injected = 0;

                string s = "";

                for (int i = 0; i < SPTEntries.Length; i++)
                {
                    var spt = SPTEntries[i];


                    var vm = bo4.GetValue<byte>(spt.Buffer + 0x7);

                    if (vm != 0x36) // latest version of BO4 is VM36
                    {
                        Message = $"found script with unknown VM {vm:x}";
                        string Message2 = "";
                        if (injected != 0)
                        {
                            closed = true;
                            bo4.CloseHandle();

                            Message += "Reverse: ";
                            Reverse(ref Message2);
                            Message += Message2;
                        }
                        return false;
                    }

                    foreach (ReplaceData replacedData in ReplaceScripts)
                    {
                        if (replacedData.Script != spt.ScriptName)
                        {
                            continue;
                        }
                        var exportCount = bo4.GetValue<ushort>(spt.Buffer + 0x1E);
                        var exportStart = bo4.GetValue<uint>(spt.Buffer + 0x30);
                        var ExportEntries = bo4.GetArray<T8Export>(spt.Buffer + exportStart, (int)exportCount);

                        for (int j = 0; j < ExportEntries.Length; j++)
                        {
                            var export = ExportEntries[j];

                            if (!(export.FunctionID == replacedData.Function))
                            {
                                continue; // not our function
                            }
                            var ByteCodeAddress = spt.Buffer + export.ByteCodeAddress;
                            s += $"\nfunction_{export.FunctionID:x} Offset: 0x{export.ByteCodeAddress:x} ";
                            replacedData.PreviousInjection = bo4.GetBytes(ByteCodeAddress, replacedData.Data.Length);
                            foreach (byte b in replacedData.PreviousInjection)
                            {
                                s += $"{b:x2}";
                            }

                            // we know this thing can contain a return true;

                            bo4.SetBytes(ByteCodeAddress, replacedData.Data);
                            replacedData.Injected = true;

                            injected++;
                        }
                    }
                }
                if (injected == 0)
                {
                    Message = "Can't find zm_utility script";
                    return false;

                }
                Message = "Injected " + injected + " " + s;
                return true;
            }
            finally
            {
                if (!closed)
                {
                    bo4.CloseHandle();
                }
            }

        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct T8SPT
        {
            public PointerEx ScriptName;
            public long pad0;
            public PointerEx Buffer;
            public int Size;
            public int Unk0;
        };
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct T8Export
        {
            public uint crc32;
            public uint ByteCodeAddress;
            public uint FunctionID;
            public uint Namespace;
            public uint Namespace1;
            public byte NumParams;
            public byte Flags;
            public ushort pad;
        };

        public static ulong HashFNV1a(byte[] bytes, ulong fnv64Offset = 14695981039346656037, ulong fnv64Prime = 0x100000001b3)
        {
            ulong hash = fnv64Offset;

            for (var i = 0; i < bytes.Length; i++)
            {
                hash = hash ^ bytes[i];
                hash *= fnv64Prime;
            }

            return hash;
        }
        public static ulong T8s64Hash(string input)
        {
            return 0x7FFFFFFFFFFFFFFF & HashFNV1a(Encoding.ASCII.GetBytes(input.ToLower()));
        }
    }
}
