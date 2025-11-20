using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class GameManager : MonoBehaviour {

    private List<IKeywordHandler> handlers;

      ////////////////////////
     ///Singleton Paradigm///
    ////////////////////////
    public static GameManager Instance { get; private set; }

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    private void Start() {
        handlers = new() {
            new ExitKH()
        };
    }

    //////////////////////////////

    public void NotifyHandlers() {
        foreach (var handler in handlers) {
            handler.TryExecute();
        }
    }
}
