# Plugin.UplayServer.Rest

Local stand-in for the `*-ubiservices.ubi.com` REST + WebSocket surface that embedded UbiServices SDK talks to directly this is separate from, and in addition to, the `uplay_r1_loader64.dll` surface emulated by `Uplay.upc_r1`.

A `ServerApp` plugin: `[HTTP]`- and `[WS]`-attributed static methods merged into the port-443 server. Companion to `Plugin.UplayServer.Storm`, which serves the UDP Storm/MPE layer.

`Resources/*.json` are verbatim captures of real Ubisoft responses, loaded as embedded resources. They are game configuration data, and the emulator cannot work without them.
