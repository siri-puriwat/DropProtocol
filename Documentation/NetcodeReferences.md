# Netcode References

Official documentation for the multiplayer stack DropProtocol runs on, mapped to the code that
uses it. Read this alongside [Networking.md](Networking.md), which records *our* decisions; these
pages explain the engine behaviour those decisions rely on.

## Versions

| Package | Version | Manual |
|---|---|---|
| Netcode for GameObjects (NGO) | 2.13.2 | https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/manual/ |
| Unity Transport (UTP) | 2.7.3 | https://docs.unity3d.com/Packages/com.unity.transport@2.7/manual/index.html |
| Multiplayer Services SDK | 2.3.1 | https://docs.unity3d.com/Packages/com.unity.services.multiplayer@2.3/manual/index.html |
| Multiplayer Play Mode | 2.0.2 | https://docs.unity3d.com/Packages/com.unity.multiplayer.playmode@2.0/manual/index.html |
| Multiplayer Tools | 2.2.11 | https://docs.unity3d.com/Packages/com.unity.multiplayer.tools@2.2/manual/index.html |

The NGO manual also ships offline as Markdown inside the package cache:
`Library/PackageCache/com.unity.netcode.gameobjects@<hash>/Documentation~/`. Same page structure as
the links below (`.html` ↔ `.md`). The older `docs-multiplayer.unity3d.com` site redirects here.

All NGO links below are relative to the NGO manual root above.

## Concepts (read first)

| Page | Link | Why it matters here |
|---|---|---|
| Networking concepts | `networking-concepts.html` | Vocabulary: server, host, client, authority, ownership. |
| Authority | `terms-concepts/authority.html` | Basis for the authority rules and the server-authoritative movement decision. |
| Client-server | `terms-concepts/client-server.html` | Our topology. Distributed authority is *not* used. |
| Listen server host architecture | `learn/listenserverhostarchitecture.html` | The host is server + client in one process; explains why `OnServerStopped` and `OnClientStopped` both fire on the host. |
| Ownership | `terms-concepts/ownership.html` | Owner (input) vs authority (simulation). `NetworkPlayer` splits along this line. |

## Session and connections

Code: `NetworkSession`, `SessionRules`, `NetworkRoot` prefab.

| Page | Link | Used for |
|---|---|---|
| NetworkManager | `components/core/networkmanager.html` | `StartHost`, `StartClient`, `Shutdown`, singleton rules, why it must be a scene root. |
| Connection approval | `basics/connection-approval.html` | `ConnectionApprovalCallback`, `Approved`, `Reason`, `CreatePlayerObject = false`. |
| Max players | `basics/maxnumberplayers.html` | Pattern behind `SessionRules.CanAccept`. |
| Connection events | `advanced-topics/connection-events.html` | `OnConnectionEvent`, `OnClientStopped`, `OnServerStopped`, `DisconnectReason`. |
| Transports | `advanced-topics/transports.html` | `UnityTransport.SetConnectionData(address, port, listenAddress)`. |
| Unity Relay | `relay/relay.html` | Deferred step: join codes once the project is linked to UGS. |
| Session management | `advanced-topics/session-management.html` | Reference for future reconnection / identity handling. |
| Reconnecting mid-game | `advanced-topics/reconnecting-mid-game.html` | Not implemented; read before adding it. |

## Objects and spawning

Code: `PlayerSpawner`, `SpawnSlots`, `PlayerCharacter.prefab`, `DefaultNetworkPrefabs.asset`.

| Page | Link | Used for |
|---|---|---|
| NetworkObject | `components/core/networkobject.html` | `GlobalObjectIdHash`, network prefab registration, `DestroyWithScene`. |
| NetworkBehaviour | `components/core/networkbehaviour.html` | `OnNetworkSpawn` / `OnNetworkDespawn`, `IsServer`, `IsOwner`. |
| Synchronizing & order of operations | `components/core/networkbehaviour-synchronize.html` | Why `NetworkVariable`s are written *after* spawn and why `Start` sees synced values. |
| PlayerObjects and player prefabs | `components/core/playerobjects.html` | What `NetworkConfig.PlayerPrefab` would do; we replaced it with `PlayerSpawner`. |
| Object spawning | `basics/object-spawning.html` | `SpawnAsPlayerObject`, `SpawnWithOwnership`, `destroyWithScene`. |
| Spawning synchronization | `basics/spawning-synchronization.html` | How late joiners receive objects spawned before they connected. |
| In-scene placed NetworkObjects | `basics/scenemanagement/inscene-placed-networkobjects.html` | `PlayerSpawner` is one; explains its hash and spawn timing. |
| Network prefab handler | `advanced-topics/network-prefab-handler.html` | Future: pooling or per-client prefab overrides. |

## Command flow and replication

Code: `NetworkPlayer`, `NetworkCommandSource`, `NetworkTransform` on the player prefab.

| Page | Link | Used for |
|---|---|---|
| RPC | `advanced-topics/message-system/rpc.html` | `[Rpc(SendTo.Server)]`, `RpcInvokePermission.Owner`, local execution on the host. |
| Reliability | `advanced-topics/message-system/reliability.html` | Why `SubmitCommandRpc` is `RpcDelivery.Unreliable`. |
| RPC params | `advanced-topics/message-system/rpc-params.html` | `RpcParams`, sender id, target overrides. |
| RPC vs NetworkVariables | `learn/rpcvnetvar.html` | Events (commands) via RPC, state (`PlayerIndex`) via NetworkVariable. |
| NetworkVariable | `basics/networkvariable.html` | `NetworkVariable<int> PlayerIndex`, `OnValueChanged`, write permissions. |
| NetworkTransform | `components/helper/networktransform.html` | `AuthorityMode.Server`, axis sync flags, `Interpolate`, `UseHalfFloatPrecision`, `Teleport`. |
| Network time and ticks | `advanced-topics/networktime-ticks.html` | `NetworkTickSystem.Tick` drives command sends; `TickRate = 30`. |
| Ticks and update rates | `learn/ticks-and-update-rates.html` | Choosing tick rate vs frame rate. |
| Network update loop | `advanced-topics/network-update-loop-system/index.html` | Where NGO processes messages relative to `Update`; explains RPC arrival timing. |

## Serialization

Code: `PlayerCommand.NetworkSerialize`, `PlayerCommandSerializationTests`.

| Page | Link | Used for |
|---|---|---|
| Serialization overview | `advanced-topics/serialization/serialization-overview.html` | What can cross the wire. |
| INetworkSerializable | `advanced-topics/serialization/inetworkserializable.html` | The interface `PlayerCommand` implements. |
| BufferSerializer | `advanced-topics/bufferserializer.html` | `SerializeValue` on both read and write paths. |
| FastBufferWriter / Reader | `advanced-topics/fastbufferwriter-fastbufferreader.html` | Used directly by the EditMode round-trip test. |
| INetworkSerializeByMemcpy | `advanced-topics/serialization/inetworkserializebymemcpy.html` | Alternative for plain unmanaged structs; not used because the buttons are packed. |

## Scene flow

Code: `NetworkSession.StartHost`, `Bootstrap`, `MainMenuController`.

| Page | Link | Used for |
|---|---|---|
| Scene management overview | `basics/scenemanagement/scene-management-overview.html` | Integrated scene management is on (`EnableSceneManagement`). |
| Using NetworkSceneManager | `basics/scenemanagement/using-networkscenemanager.html` | `SceneManager.LoadScene(name, LoadSceneMode.Single)` from the host. |
| Scene events | `basics/scenemanagement/scene-events.html` | `OnSceneEvent`, `OnLoadEventCompleted` if load-complete hooks are needed. |
| Client synchronization mode | `basics/scenemanagement/client-synchronization-mode.html` | How a joining client is brought to the host's scene. |
| Timing considerations | `basics/scenemanagement/timing-considerations.html` | Spawning during scene transitions; why `PlayerSpawner` lives in the gameplay scene. |

## Latency

Background for the movement-authority decision and for any future prediction work.

| Page | Link | Relevance |
|---|---|---|
| Understanding latency | `learn/lagandpacketloss.html` | What one round trip of input latency means for remote clients. |
| Dealing with latency | `learn/dealing-with-latency.html` | Survey of options: server-auth, owner-auth, prediction. |
| Client-side interpolation | `learn/clientside-interpolation.html` | What `NetworkTransform.Interpolate` does on non-authority instances. |
| Client anticipation | `advanced-topics/client-anticipation.html` | NGO's built-in lightweight prediction. First candidate if input lag proves unacceptable. |

## Testing and debugging

Code: `NetworkPlayerHostTests`; manual runs with Multiplayer Play Mode.

| Page | Link | Relevance |
|---|---|---|
| Testing locally | `tutorials/testing/testing_locally.html` | Multiplayer Play Mode and multi-instance workflows. |
| Testing client connection management | `tutorials/testing/testing_client_connection_management.html` | Approval / disconnect test patterns. |
| Testing with artificial conditions | `tutorials/testing/testing_with_artificial_conditions.html` | Network simulator (Multiplayer Tools) for latency and loss. |
| Logging | `basics/logging.html` | `NetworkManager.LogLevel`. |
| Troubleshooting / error messages | `troubleshooting/troubleshooting.html`, `troubleshooting/error-messages.html` | First stop for NGO console errors. |
| Boss Room sample | `samples/bossroom/bossroom-landing.html`, `samples/bossroom/architecture.html` | Unity's reference co-op project; same host-authoritative shape as DropProtocol. |

## Other packages

| Package | Page | Relevance |
|---|---|---|
| Unity Transport | https://docs.unity3d.com/Packages/com.unity.transport@2.7/manual/index.html | UDP layer under NGO: pipelines, reliability, connection timeouts (about 60 s by default before a failed join reports). |
| Multiplayer Services SDK | https://docs.unity3d.com/Packages/com.unity.services.multiplayer@2.3/manual/index.html | Sessions, Relay, join codes. Needed when Relay lands; requires a linked UGS project and Authentication. |
| Multiplayer Play Mode | https://docs.unity3d.com/Packages/com.unity.multiplayer.playmode@2.0/manual/index.html | Virtual players in one Editor; used for the two-player verification in the README. |
| Multiplayer Tools | https://docs.unity3d.com/Packages/com.unity.multiplayer.tools@2.2/manual/index.html | Network Profiler, Runtime Network Stats Monitor (Milestone 8 overlay), Network Simulator. |
