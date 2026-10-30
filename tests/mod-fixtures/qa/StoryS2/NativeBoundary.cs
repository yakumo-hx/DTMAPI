using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DolocTown;
using DolocTown.GameData;
using Yarn.Unity;
using TextLineProvider = DolocTown.TextLineProvider;
using Runner = DolocTown.DialogueRunner;

namespace DTMAPI.Internal.StoryS2
{
    internal sealed class NativeBoundary
    {
        internal readonly FieldInfo ManagerProject, ManagerProvider, ManagerRunner, RunnerProject, RunnerProvider, RunnerDialogue, ProviderProject, RuntimeStrings;
        internal readonly ConstructorInfo ManagerConstructor, RunnerConstructor, MetadataConstructor;
        internal readonly MethodInfo StartDialogue;
        private readonly Type metadataEntry;
        private readonly FieldInfo metadataId, metadataTags;
        internal NativeBoundary()
        {
            CheckIdentity(typeof(DialogueManager).Assembly, "ABB5DCBCE5EAA5C4453215BE770D2052224FA0A8B40FB7034F9BC3E6F442E869");
            CheckIdentity(typeof(Yarn.Dialogue).Assembly, "CA7965362FE57159DE58C2BBA5E818DE5964D73EA68C9A13DD18DAB8AC4399B3");
            CheckIdentity(typeof(YarnProject).Assembly, "6DED28540862A88C33C392FF62BF1DEC00F71E757B302E46A154D606DBE0491E");
            CheckIdentity(typeof(Google.Protobuf.MessageExtensions).Assembly, "973D35695C54714C1E9A2C9AF4C884C8C60A0B635397D12817E6B2AF4788CB82");
            ManagerProject = Field(typeof(DialogueManager), "project", typeof(YarnProject), true);
            ManagerProvider = Field(typeof(DialogueManager), "lineProvider", typeof(TextLineProvider), true);
            ManagerRunner = Field(typeof(DialogueManager), "dialogueRunner", typeof(Runner), false);
            RunnerProject = Field(typeof(Runner), "project", typeof(YarnProject), false);
            RunnerProvider = Field(typeof(Runner), "lineProvider", typeof(TextLineProvider), false);
            RunnerDialogue = Field(typeof(Runner), "dialogue", typeof(Yarn.Dialogue), false);
            ProviderProject = Field(typeof(TextLineProvider), "yarnProject", typeof(YarnProject), false);
            RuntimeStrings = Field(typeof(Localization), "_runtimeStringTable", typeof(Dictionary<string, string>), false);
            ManagerConstructor = Constructor(typeof(DialogueManager), typeof(string), typeof(DialogueVariableStorage),
                typeof(Dictionary<string, DialogueData>), typeof(List<DialogueNodeData>), typeof(Dictionary<string, int>),
                typeof(Dictionary<string, DateInfo>), typeof(DialogueHistoryManager));
            RunnerConstructor = Constructor(typeof(Runner), typeof(YarnProject), typeof(TextLineProvider), typeof(DialogueVariableStorage));
            metadataEntry = typeof(YarnProject).Assembly.GetType("Yarn.Unity.LineMetadataTableEntry", true)!;
            Candidate.Require(metadataEntry.IsValueType, "S2-NATIVE-METADATA-TYPE");
            metadataId = Field(metadataEntry, "ID", typeof(string), false);
            metadataTags = Field(metadataEntry, "Metadata", typeof(string[]), false);
            MetadataConstructor = Constructor(typeof(LineMetadata), typeof(IEnumerable<>).MakeGenericType(metadataEntry));
            StartDialogue = Method(typeof(Runner), "StartDialogue", typeof(bool), false, typeof(string), typeof(string));
        }
        internal static void CheckIdentity(Assembly assembly, string expected)
        {
            Candidate.Require(Candidate.Hash(File.ReadAllBytes(assembly.Location)) == expected,
                "S2-NATIVE-IDENTITY:" + assembly.GetName().Name);
        }
        internal static FieldInfo Field(Type type, string name, Type value, bool isStatic)
        {
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
            Candidate.Require(field != null && field.FieldType == value && field.IsStatic == isStatic, "S2-NATIVE-FIELD:" + type.FullName + "." + name);
            return field!;
        }
        private static ConstructorInfo Constructor(Type type, params Type[] parameters) =>
            type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, parameters, null)
            ?? throw new MissingMethodException("S2-NATIVE-CTOR:" + type.FullName);
        internal static MethodInfo Method(Type type, string name, Type returns, bool isStatic, params Type[] parameters)
        {
            var method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, null, parameters, null);
            Candidate.Require(method != null && method.ReturnType == returns && method.IsStatic == isStatic, "S2-NATIVE-METHOD:" + type.FullName + "." + name);
            return method!;
        }
        internal Runner Runner(DialogueManager manager) => (Runner)ManagerRunner.GetValue(manager)!;
        internal Yarn.Dialogue Dialogue(Runner runner) => (Yarn.Dialogue)RunnerDialogue.GetValue(runner)!;
        internal LineMetadata Metadata(IReadOnlyList<KeyValuePair<string, string[]>> entries)
        {
            var array = Array.CreateInstance(metadataEntry, entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                var row = Activator.CreateInstance(metadataEntry)!;
                metadataId.SetValue(row, entries[i].Key);
                metadataTags.SetValue(row, entries[i].Value);
                array.SetValue(row, i);
            }
            return (LineMetadata)MetadataConstructor.Invoke(new object[] { array });
        }
    }
}
