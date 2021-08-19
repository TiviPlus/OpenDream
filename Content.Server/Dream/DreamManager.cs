﻿using Content.Server.DM;
using Content.Server.Dream.MetaObjects;
using Content.Server.Dream.NativeProcs;
using Content.Shared.Dream;
using Content.Shared.Json;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Content.Server.Dream {
    class DreamManager : IDreamManager {
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IDreamMapManager _dreamMapManager = default!;

        public DreamObjectTree ObjectTree { get; private set; }
        public DreamObject WorldInstance { get; private set; }
        public int DMExceptionCount { get; set; }

        // Global state that may not really (really really) belong here
        public DreamList WorldContentsList { get; set; }
        public Dictionary<DreamObject, DreamList> AreaContents { get; set; } = new();
        public Random Random { get; set; } = new();

        private Dictionary<DreamObject, NetUserId> _clientToUserId = new();

        public void Initialize() {
            DreamCompiledJson json = LoadJson();
            if (json == null)
                return;

            ObjectTree = new DreamObjectTree(json);
            SetMetaObjects();
            DreamProcNative.SetupNativeProcs(ObjectTree);

            _dreamMapManager.Initialize();
            WorldInstance = ObjectTree.CreateObject(DreamPath.World);
            ObjectTree.Root.ObjectDefinition.GlobalVariables["world"].Value = new DreamValue(WorldInstance);
            WorldInstance.InitSpawn(new DreamProcArguments(null));

            if (json.GlobalInitProc != null) {
                var globalInitProc = new DMProc("(global init)", null, null, null, json.GlobalInitProc.Bytecode, true);
                globalInitProc.Spawn(WorldInstance, new DreamProcArguments(new(), new()));
            }

            _dreamMapManager.LoadMaps(json.Maps);
            WorldInstance.SpawnProc("New");

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        }

        public void Shutdown() {

        }

        public IPlayerSession GetSessionFromClient(DreamObject client) {
            return _playerManager.GetSessionByUserId(_clientToUserId[client]);
        }

        public DreamObject GetClientFromMob(DreamObject mob) {
            foreach (DreamObject client in _clientToUserId.Keys) {
                if (client.GetVariable("mob").GetValueAsDreamObject() == mob)
                    return client;
            }

            return null;
        }

        private DreamCompiledJson LoadJson() {
            string jsonPath = _configManager.GetCVar<string>(OpenDreamCVars.JsonPath);
            if (string.IsNullOrEmpty(jsonPath) || !File.Exists(jsonPath)) {
                Logger.Error("Error while loading the compiled json. The opendream.json_path CVar may be empty, or points to a file that doesn't exist");

                return null;
            }

            string jsonSource = File.ReadAllText(jsonPath);
            return JsonSerializer.Deserialize<DreamCompiledJson>(jsonSource);
        }

        private void SetMetaObjects() {
            ObjectTree.SetMetaObject(DreamPath.Root, new DreamMetaObjectRoot());
            ObjectTree.SetMetaObject(DreamPath.List, new DreamMetaObjectList());
            ObjectTree.SetMetaObject(DreamPath.Client, new DreamMetaObjectClient());
            ObjectTree.SetMetaObject(DreamPath.World, new DreamMetaObjectWorld());
            ObjectTree.SetMetaObject(DreamPath.Datum, new DreamMetaObjectDatum());
            ObjectTree.SetMetaObject(DreamPath.Regex, new DreamMetaObjectRegex());
            ObjectTree.SetMetaObject(DreamPath.Atom, new DreamMetaObjectAtom());
            ObjectTree.SetMetaObject(DreamPath.Area, new DreamMetaObjectArea());
            ObjectTree.SetMetaObject(DreamPath.Turf, new DreamMetaObjectTurf());
            ObjectTree.SetMetaObject(DreamPath.Movable, new DreamMetaObjectMovable());
            ObjectTree.SetMetaObject(DreamPath.Mob, new DreamMetaObjectMob());
        }

        private void OnPlayerStatusChanged(object sender, SessionStatusEventArgs e) {
            IPlayerSession session = e.Session;

            switch (e.NewStatus) {
                case SessionStatus.Connected:
                    e.Session.JoinGame();
                    break;
                case SessionStatus.InGame: {
                    DreamObject client = ObjectTree.CreateObject(DreamPath.Client);

                    _clientToUserId[client] = session.UserId;
                    session.Data.ContentDataUncast = new PlayerSessionData(client);
                    client.InitSpawn(new DreamProcArguments(new() { DreamValue.Null }));

                    break;
                }
            }
        }
    }
}