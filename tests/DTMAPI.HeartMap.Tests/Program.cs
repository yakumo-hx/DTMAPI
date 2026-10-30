using System;
using DTMAPI.Internal.HeartMap;

var layout = new HeartLayout();
Check(layout.HasStandingClearance(HeartLayout.SpawnX, HeartLayout.SpawnY), "safe starting ledge");
Check(layout.HasStandingClearance(12, 30) && layout.HasStandingClearance(34, 30), "both lobe landings have two-cell clearance");
Check(layout.Solid[23, 34] && !layout.Solid[12, 34] && !layout.Solid[35, 34], "two lobes and a central notch");
Check(!layout.Solid[23, 4] && layout.Solid[18, 4] && layout.Solid[29, 4], "narrow lower point");
foreach (var cell in layout.Platforms) Check(!layout.Solid[cell.X, cell.Y], "platform cannot occupy solid terrain");
// Corrupt a real geometric invariant: an isolated cavity must be rejected.
layout.Solid[1, 1] = false;
layout.Obstacles.RemoveAll(p => p.X == 1 && p.Y == 1);
bool rejected = false;
try { layout.Validate(); } catch (InvalidOperationException) { rejected = true; }
Check(rejected, "disconnected cavity rejected");
Console.WriteLine("Heart layout checks passed. Actual jumping and collision require native gameplay.");
TransitionTests.Run();
FileInputTests.Run();
static void Check(bool condition, string detail) { if (!condition) throw new Exception(detail); }
