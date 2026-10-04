using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Elin.RuntimeTestPipe;

var queue = new ConcurrentQueue<string>();
var replies = new List<string>();
var errors = new List<Exception>();
bool running = true;
var loop = new CommandPump<string>(queue, () => running,
    cmd => cmd == "throw" ? throw new InvalidOperationException("fixture") : cmd,
    (cmd, result) => replies.Add(cmd + "=" + result), errors.Add).Run();
void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
Require(loop.MoveNext(), "Pump stopped before the first command arrived (installed EPipe regression).");
queue.Enqueue("first"); queue.Enqueue("second");
Require(loop.MoveNext() && replies.Count == 1 && replies[0] == "first=first", "Dispatch must process one command on one main-thread step.");
Require(loop.MoveNext() && replies.Count == 2, "Second command lost or duplicated.");
Require(loop.MoveNext() && replies.Count == 2, "Empty frame must stay alive without redispatch.");
queue.Enqueue("throw"); queue.Enqueue("after_error");
Require(loop.MoveNext() && errors.Count == 1, "Command failure must be observed without stopping pump.");
Require(loop.MoveNext() && replies[^1] == "after_error=after_error", "Pump did not recover after a command exception.");
running = false;
Require(!loop.MoveNext(), "Teardown must terminate pump.");
Require(TestSessionGuard.Allows("world_11", "RUNTIME_TEST", true), "Dedicated fixture rejected.");
Require(!TestSessionGuard.Allows("world_10", "RUNTIME_TEST", true), "Normal slot allowed.");
Require(!TestSessionGuard.Allows("world_11", "ordinary", true), "Ordinary character allowed.");
Require(!TestSessionGuard.Allows("world_11", "RUNTIME_TEST", false), "Disabled scripting allowed.");
Console.WriteLine("PASS: idle arrival, single dispatch, no duplicate, exception recovery, teardown, slot/name/scripting guards.");
