using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DTMAPI.Abstractions;

namespace DTMAPI.Core.Services
{
    internal sealed partial class PlatformRuntimeServices
    {
        private readonly Dictionary<string, Command> commands = new Dictionary<string, Command>(StringComparer.OrdinalIgnoreCase);

        internal IReadOnlyList<DtmCommandInfo> GetCommandHelp()
        {
            lock (gate) return commands.Values.Distinct().Select(c => c.Info).OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }

        internal Task<DtmCommandResult> ExecuteCommand(string commandLine, CancellationToken cancellationToken = default)
            => ExecuteCommand(null, commandLine, cancellationToken);

        internal Task<DtmCommandResult> ExecuteAuthorCommand(string owner, string commandLine, string requestId, out IDtmScheduledWork? scheduled)
            => ExecuteCommand(null, commandLine, CancellationToken.None, out scheduled, owner, requestId);

        private Task<DtmCommandResult> ExecuteCommand(Owner? requester, string commandLine, CancellationToken cancellationToken)
            => ExecuteCommand(requester, commandLine, cancellationToken, out _);

        private Task<DtmCommandResult> ExecuteCommand(Owner? requester, string commandLine, CancellationToken cancellationToken,
            out IDtmScheduledWork? scheduled, string? allowedOwner = null, string? suppliedRequestId = null)
        {
            scheduled = null;
            string requestId = suppliedRequestId ?? Guid.NewGuid().ToString("N");
            if (!TryParseCommand(commandLine, out string[] words, out string parseError))
                return CommandRejected(requestId, "", "invalid-command", parseError);
            Command command;
            CommandContext context;
            lock (gate)
            {
                if (requester != null && !requester.Active) return CommandRejected(requestId, requester.Id, "owner-closed", "Command requester is closed.");
                if (words.Length == 0 || words[0].Equals("help", StringComparison.OrdinalIgnoreCase))
                {
                    // Help is a read-only snapshot; it never invokes an author callback.
                    var output = GetCommandHelp().Where(info => allowedOwner == null || info.OwnerId.Equals(allowedOwner, StringComparison.OrdinalIgnoreCase))
                        .Take(32).Select(info => Bounded(info.Name + " " + info.Parameters + " — " + info.Description, 128)).ToArray();
                    return Task.FromResult(new DtmCommandResult(requestId, "", new DtmWorkResult(DtmWorkStatus.Succeeded), output));
                }
                if (!commands.TryGetValue(words[0], out command)) return CommandRejected(requestId, "", "unknown-command", "Unknown command. Use help to list registered names.");
                if (allowedOwner != null && !command.Owner.Id.Equals(allowedOwner, StringComparison.OrdinalIgnoreCase))
                    return CommandRejected(requestId, allowedOwner, "command-owner-mismatch", "This authenticated source request cannot execute another owner's command.");
                context = new CommandContext(requestId, command.Owner.Id, words.Skip(1).ToArray(), isMainThread);
            }
            IDtmScheduledWork work = Enqueue(command.Owner, () =>
                {
                    Action<IDtmCommandContext>? handler;
                    lock (gate) handler = command.Handler;
                    if (handler == null) throw new InvalidOperationException("Command registration closed before execution.");
                    try { handler(context); } finally { context.Close(); }
                }, command.Info.Scope, cancellationToken, TimeSpan.Zero, false, null);
            scheduled = work;
            return CompleteCommand(work, context);
        }

        private static async Task<DtmCommandResult> CompleteCommand(IDtmScheduledWork work, CommandContext context)
        {
            DtmWorkResult result = await work.Completion.ConfigureAwait(false);
            context.Close();
            return new DtmCommandResult(context.RequestId, context.OwnerId, result, context.GetOutput());
        }

        private static Task<DtmCommandResult> CommandRejected(string id, string owner, string code, string message)
            => Task.FromResult(new DtmCommandResult(id, owner, new DtmWorkResult(DtmWorkStatus.Rejected, code, message), Array.Empty<string>()));

        internal static bool TryParseCommand(string? input, out string[] words, out string error)
        {
            words = Array.Empty<string>(); error = "";
            if (input == null || input.Length > 4096) { error = "Command input must be non-null and at most 4096 characters."; return false; }
            var result = new List<string>();
            var word = new StringBuilder();
            char quote = '\0';
            bool escaped = false, hasWord = false;
            foreach (char ch in input)
            {
                if (ch == '\0' || (char.IsControl(ch) && !char.IsWhiteSpace(ch))) { error = "Control characters are not supported."; return false; }
                if (escaped) { word.Append(ch); escaped = false; hasWord = true; }
                else if (ch == '\\') { escaped = true; hasWord = true; }
                else if (quote != '\0') { if (ch == quote) quote = '\0'; else word.Append(ch); }
                else if (ch == '"' || ch == '\'') { quote = ch; hasWord = true; }
                else if (char.IsWhiteSpace(ch))
                {
                    if (hasWord) { result.Add(word.ToString()); word.Clear(); hasWord = false; }
                }
                else { word.Append(ch); hasWord = true; }
                if (result.Count > 64) { error = "Too many command arguments."; return false; }
            }
            if (escaped || quote != '\0') { error = "Unterminated quote or escape."; return false; }
            if (hasWord) result.Add(word.ToString());
            if (result.Count > 65) { error = "Too many command arguments."; return false; }
            words = result.ToArray(); return true;
        }

        private static bool IsCommandToken(string? token)
            => !string.IsNullOrWhiteSpace(token) && token!.Length <= 64 && token.All(ch =>
                (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '-' || ch == '_' || ch == '.');

        private IDisposable RegisterCommand(Owner owner, string name, Action<IDtmCommandContext> handler,
            DtmWorkScope scope, string description, string parameters, string? alias)
        {
            RequireMainThread(); ValidateScope(scope);
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (!IsCommandToken(name) || (alias != null && (!IsCommandToken(alias) || alias.Equals("help", StringComparison.OrdinalIgnoreCase))))
                throw new ArgumentException("Command name/alias must contain only letters, digits, '.', '_' or '-'; help is reserved as an alias.");
            if (description == null || description.Length > 512 || parameters == null || parameters.Length > 256)
                throw new ArgumentException("Command description/parameters are missing or too long.");
            lock (gate)
            {
                owner.RequireActive();
                if (snapshot.Phase == DtmRuntimePhase.ShuttingDown) throw new ObjectDisposedException(nameof(PlatformRuntimeServices));
                if (owner.Commands.Count >= 64) throw new InvalidOperationException("Owner command capacity exceeded.");
                string canonical = owner.Id + "/" + name;
                foreach (string key in alias == null ? new[] { canonical } : new[] { canonical, alias })
                    if (commands.TryGetValue(key, out Command conflict)) throw new InvalidOperationException("Command '" + key + "' is already registered by " + conflict.Owner.Id + ".");
                var command = new Command(owner, new DtmCommandInfo(canonical, owner.Id, description, parameters, scope), alias, handler);
                commands.Add(canonical, command);
                if (alias != null) commands.Add(alias, command);
                owner.Commands.Add(command);
                return command;
            }
        }

        private void RemoveCommandLocked(Command command)
        {
            if (command.Handler == null) return;
            command.Handler = null;
            commands.Remove(command.Info.Name);
            if (command.Alias != null) commands.Remove(command.Alias);
            command.Owner.Commands.Remove(command);
        }

        private sealed class Command : IDisposable
        {
            internal readonly Owner Owner;
            internal readonly DtmCommandInfo Info;
            internal readonly string? Alias;
            internal Action<IDtmCommandContext>? Handler;
            internal Command(Owner owner, DtmCommandInfo info, string? alias, Action<IDtmCommandContext> handler)
            { Owner = owner; Info = info; Alias = alias; Handler = handler; }
            public void Dispose() { lock (Owner.Runtime.gate) Owner.Runtime.RemoveCommandLocked(this); }
        }

        private sealed class CommandContext : IDtmCommandContext
        {
            private readonly object outputGate = new object();
            private readonly Func<bool> isMainThread;
            private readonly List<string> lines = new List<string>();
            private int characters;
            private bool closed;
            public string RequestId { get; }
            public string OwnerId { get; }
            public IReadOnlyList<string> Arguments { get; }
            internal CommandContext(string id, string owner, string[] arguments, Func<bool> isMainThread)
            { RequestId = id; OwnerId = owner; Arguments = Array.AsReadOnly(arguments); this.isMainThread = isMainThread; }
            public bool WriteLine(string text)
            {
                if (!isMainThread()) throw new InvalidOperationException("Command output is synchronous and Runtime-thread only.");
                lock (outputGate)
                {
                    if (closed || text == null || lines.Count >= 32 || text.Length > 4096 - characters) return false;
                    lines.Add(text); characters += text.Length; return true;
                }
            }
            internal void Close() { lock (outputGate) closed = true; }
            internal string[] GetOutput() { lock (outputGate) return lines.ToArray(); }
        }

        private sealed partial class Owner
        {
            public IDisposable Register(string name, Action<IDtmCommandContext> handler, DtmWorkScope scope = DtmWorkScope.ModOwner,
                string description = "", string parameters = "", string? alias = null)
                => Runtime.RegisterCommand(this, name, handler, scope, description, parameters, alias);
            public IReadOnlyList<DtmCommandInfo> GetHelp() { lock (Runtime.gate) { RequireActive(); return Runtime.GetCommandHelp(); } }
            public Task<DtmCommandResult> Execute(string commandLine, CancellationToken cancellationToken = default)
                => Runtime.ExecuteCommand(this, commandLine, cancellationToken);
        }
    }
}
