﻿using Content.Shared.Interface;
using Robust.Client.Input;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using System;
using System.Collections.Generic;
using Key = Robust.Client.Input.Keyboard.Key;

namespace Content.Client.Input {
    class DreamMacroManager : IDreamMacroManager {
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        private const string InputContextPrefix = "macroSet_";

        // Macros with +REP that are currently held
        private List<MacroDescriptor> _activeMacros = new();

        public void LoadMacroSets(List<MacroSetDescriptor> macroSets) {
            IInputContextContainer contexts = _inputManager.Contexts;

            foreach (MacroSetDescriptor macroSet in macroSets) {
                IInputCmdContext context = contexts.New(InputContextPrefix + macroSet.Name, "common");

                foreach (MacroDescriptor macro in macroSet.Macros) {
                    BoundKeyFunction function = new BoundKeyFunction(macro.Id);
                    KeyBindingRegistration binding = CreateMacroBinding(function, macro.Name);

                    context.AddFunction(function);
                    _inputManager.RegisterBinding(in binding);
                    _inputManager.SetInputCommand(function, InputCmdHandler.FromDelegate(_ => OnMacroPress(macro), _ => OnMacroRelease(macro)));
                }
            }
        }

        public void SetActiveMacroSet(MacroSetDescriptor macroSet) {
            _inputManager.Contexts.SetActiveContext(InputContextPrefix + macroSet.Name);
        }

        public void RunActiveMacros() {
            DreamCommandSystem commandSystem = _entitySystemManager.GetEntitySystem<DreamCommandSystem>();

            foreach (MacroDescriptor macro in _activeMacros) {
                commandSystem.RunCommand(macro.Command);
            }
        }

        private void OnMacroPress(MacroDescriptor macro) {
            if (String.IsNullOrEmpty(macro.Command))
                return;

            _entitySystemManager.GetEntitySystem<DreamCommandSystem>().RunCommand(macro.Command);
            if (macro.Name.EndsWith("+REP")) {
                _activeMacros.Add(macro);
            }
        }

        private void OnMacroRelease(MacroDescriptor macro) {
            _activeMacros.Remove(macro);
            if (!macro.Name.EndsWith("+UP") || String.IsNullOrEmpty(macro.Command))
                return;

            _entitySystemManager.GetEntitySystem<DreamCommandSystem>().RunCommand(macro.Command);
        }

        private KeyBindingRegistration CreateMacroBinding(BoundKeyFunction function, string macroName) {
            macroName = macroName.Replace("SHIFT+", String.Empty);
            macroName = macroName.Replace("CTRL+", String.Empty);
            macroName = macroName.Replace("ALT+", String.Empty);
            macroName = macroName.Replace("+UP", String.Empty);
            macroName = macroName.Replace("+REP", String.Empty);

            //TODO: modifiers
            return new KeyBindingRegistration() {
                BaseKey = KeyNameToKey(macroName),
                Function = function
            };
        }

        private static Key KeyNameToKey(string keyName) {
            return keyName.ToUpper() switch {
                "A" => Key.A,
                "B" => Key.B,
                "C" => Key.C,
                "D" => Key.D,
                "E" => Key.E,
                "F" => Key.F,
                "G" => Key.G,
                "H" => Key.H,
                "I" => Key.I,
                "J" => Key.J,
                "K" => Key.K,
                "L" => Key.L,
                "M" => Key.M,
                "N" => Key.N,
                "O" => Key.O,
                "P" => Key.P,
                "Q" => Key.Q,
                "R" => Key.R,
                "S" => Key.S,
                "T" => Key.T,
                "U" => Key.U,
                "V" => Key.V,
                "W" => Key.W,
                "X" => Key.X,
                "Y" => Key.Y,
                "Z" => Key.Z,
                "0" => Key.Num0,
                "1" => Key.Num1,
                "2" => Key.Num2,
                "3" => Key.Num3,
                "4" => Key.Num4,
                "5" => Key.Num5,
                "6" => Key.Num6,
                "7" => Key.Num7,
                "8" => Key.Num8,
                "9" => Key.Num9,
                "NUMPAD0" => Key.NumpadNum0,
                "NUMPAD1" => Key.NumpadNum1,
                "NUMPAD2" => Key.NumpadNum2,
                "NUMPAD3" => Key.NumpadNum3,
                "NUMPAD4" => Key.NumpadNum4,
                "NUMPAD5" => Key.NumpadNum5,
                "NUMPAD6" => Key.NumpadNum6,
                "NUMPAD7" => Key.NumpadNum7,
                "NUMPAD8" => Key.NumpadNum8,
                "NUMPAD9" => Key.NumpadNum9,
                "NORTH" => Key.Up,
                "SOUTH" => Key.Down,
                "EAST" => Key.Right,
                "WEST" => Key.Left,
                "NORTHWEST" => Key.Home,
                "SOUTHWEST" => Key.End,
                "NORTHEAST" => Key.PageUp,
                "SOUTHEAST" => Key.PageDown,
                //"CENTER" => Key.Clear,
                "RETURN" => Key.Return,
                "ESCAPE" => Key.Escape,
                "TAB" => Key.Tab,
                "SPACE" => Key.Space,
                "BACK" => Key.BackSpace,
                "INSERT" => Key.Insert,
                "DELETE" => Key.Delete,
                "PAUSE" => Key.Pause,
                //"SNAPSHOT" => Key.PrintScreen,
                "LWIN" => Key.LSystem,
                "RWIN" => Key.RSystem,
                //"APPS" => Key.Apps,
                "MULTIPLY" => Key.NumpadMultiply,
                "ADD" => Key.NumpadAdd,
                "SUBTRACT" => Key.NumpadSubtract,
                "DIVIDE" => Key.NumpadDivide,
                //TODO: Right shift/ctrl/alt
                "SHIFT" => Key.Shift,
                "CTRL" => Key.Control,
                "ALT" => Key.Alt,
                _ => throw new Exception("Invalid key name \"" + keyName + "\"")
            };
        }
    }

    interface IDreamMacroManager {
        public void LoadMacroSets(List<MacroSetDescriptor> macroSets);
        public void SetActiveMacroSet(MacroSetDescriptor macroSet);
        public void RunActiveMacros();
    }
}