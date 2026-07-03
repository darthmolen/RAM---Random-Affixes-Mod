# MapUnlockProject

Builds the Harmony DLL used by `MapUnlockMod`.

The mod keeps the vanilla `World.MapEnabled` behavior and additionally treats the
map as enabled for the local player when their persistent `mapUnlockEnabled` CVar
is greater than zero.
