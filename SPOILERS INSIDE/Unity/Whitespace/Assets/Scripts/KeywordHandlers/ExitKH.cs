using System;
using System.Collections.Generic;
using UnityEngine;

public class ExitKH : IKeywordHandler {
    public readonly Dictionary<string, Action> keywords;

    public ExitKH() {
        keywords = new() {
            {"ASCEND", MediumExit}, //0.0100.00.10.111.01.
            {"BREACH", MediumExit},
            {"DEPART", MediumExit},
            {"EGRESS", MediumExit}, //10.000.0011.10.0100.0100
            {"EMERGE", MediumExit},
            {"ESCAPE", MediumExit},
            {"EXIT", EasyExit },
            {"FREE", EasyExit},
            {"GONE", EasyExit},
            {"LEAVE", MediumExit},
            {"OPEN", EasyExit},
            {"OUT", EasyExit }, //0000.0110.0101.
            {"UNLOCK", MediumExit}
        };
    }

    public bool TryExecute() {
        foreach (var kvp in keywords) {
            if (InputHandler.Instance.EndsWith(kvp.Key)) {
                kvp.Value();
                return true;
            }
        }
        Debug.Log("ExitKH failed");
        return false;
    }

    public void EasyExit() {
        Debug.Log("You see the light, but don't trust it.");
    }

    public void MediumExit() {
        Debug.Log("You've made contact. You really are free.");
    }
}