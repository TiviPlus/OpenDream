﻿using OpenDreamServer.Dream.Procs;
using OpenDreamServer.Net;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamServer.Dream.Objects.MetaObjects {
    class DreamMetaObjectClient : DreamMetaObjectRoot {
        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            base.OnObjectCreated(dreamObject, creationArguments);

            //New() is not called here
        }

        public override void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue, DreamValue oldVariableValue) {
            if (variableName == "eye") {
                string ckey = dreamObject.GetVariable("ckey").GetValueAsString();
                DreamObject eye = variableValue.GetValueAsDreamObjectOfType(DreamPath.Atom);
                UInt16 eyeID = DreamMetaObjectAtom.AtomIDs[eye];

                Program.DreamStateManager.AddClientEyeIDDelta(ckey, eyeID);
            }

            base.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            if (variableName == "key" || variableName == "ckey") {
                return new DreamValue(Program.ClientToConnection[dreamObject].CKey);
            } else {
                return base.OnVariableGet(dreamObject, variableName, variableValue);
            }
        }

        public override DreamValue OperatorOutput(DreamValue a, DreamValue b) {
            DreamConnection connection = Program.ClientToConnection[a.GetValueAsDreamObjectOfType(DreamPath.Client)];

            connection.OutputDreamValue(b);
            return new DreamValue(0);
        }
    }
}