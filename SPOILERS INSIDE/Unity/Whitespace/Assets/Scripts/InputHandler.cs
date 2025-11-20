using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class SubmitNode {
    public char? value;
    public int depth;
    public Action func;
    public SubmitNode left;
    public SubmitNode right;
    public SubmitNode parent;
    public bool parentWentRight;

    public static SubmitNode NewNodeLeft(SubmitNode parent) {
        parent.left = new() {
            parent = parent,
            parentWentRight = false,
            depth = parent.depth + 1,
            value = null,
            func = null
        };
        return parent.left;
    }

    public static SubmitNode NewNodeRight(SubmitNode parent) {
        parent.right = new() {
            parent = parent,
            parentWentRight = true,
            depth = parent.depth + 1,
            value = null,
            func = null
        };
        return parent.right;
    }

    public char GetOrExec() {
        if (!value.HasValue) {
            if (func != null) func();
            else Debug.Log("GetOrExec on Node failed.");
            return '_';
        }
        return value.Value;
    }

    public static void BadSubmission() {
        Debug.Log("Insufficient Input!");
    }
}

class SubmitTree {
    public int count;
    public int maxDepth;
    public SubmitNode head;

    public static SubmitTree BuildTree() {
        SubmitTree tree = new() {
            count = 32,
            maxDepth = 5,
            head = new() {
                value = null,
                depth = 0,
                func = SubmitNode.BadSubmission,
            }
        };

        Queue<SubmitNode> tq = new();
        tq.Enqueue(tree.head);
        char letter = 'A';

        while (tq.Count > 0) {
            SubmitNode current = tq.Dequeue();
            
            SubmitNode l = SubmitNode.NewNodeLeft(current);
            
            if (letter <= 'Z') l.value = letter;
            else l.func = SubmitNode.BadSubmission;
            letter++;
            
            if (l.depth < tree.maxDepth - 1) tq.Enqueue(l); //don't enqueue if equal to final depth
            


            SubmitNode r = SubmitNode.NewNodeRight(current);
            
            if (letter <= 'Z') r.value = letter;
            else r.func = SubmitNode.BadSubmission;
            letter++;
            
            if (r.depth < tree.maxDepth - 1) tq.Enqueue(r);
        }

        return tree;
    }

}

public class InputHandler : MonoBehaviour {

    private List<bool> iBuffer; //stores the 1 and 0 values from the player
    public int BitBufferSize;

    private StringBuilder cBuffer; //converts iBuffer into a character and stores it, up to 128 characters
    private SubmitTree sTree;
    private List<string> History;

      ////////////////////////
     ///Singleton Paradigm///
    ////////////////////////
    public static InputHandler Instance { get; private set; }

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

    public void Start() {
        iBuffer = new();
        cBuffer = new(128);
        History = new();
        sTree = SubmitTree.BuildTree();
    }

      /////////////////////////////
     ///Binary Public Interface///
    /////////////////////////////
    public void Insert(bool right) {
        iBuffer.Add(right);
        if (iBuffer.Count >= BitBufferSize) Pop();
        Print("Inserted into iBuffer.");
        Debug.Log($"If submit, output: {ReadLetter()}");
    }

    public char ReadLetter() {
        SubmitNode current = sTree.head;

        var iBufferCopy = new List<bool>(iBuffer);

        while (iBufferCopy.Count > 0 && current != null) {
            if (Pop(iBufferCopy)) current = current.right;
            else current = current.left;
        }

        return (current != null && current.value.HasValue) ? current.value.Value : '_';
    }

    public void Submit() {
        SubmitNode current = sTree.head;
        while (iBuffer.Count > 0 && current != null) {
            if (Pop()) current = current.right;
            else current = current.left;
        }
        if (current != null) {
            char letter = current.GetOrExec();
            cBuffer.Append(letter);
            Debug.Log($"Submission Thusfar: {cBuffer}");
            GameManager.Instance.NotifyHandlers();
        } else Debug.Log("Submission Error: Overflow, current is null.");
    }

    // When the user presses Backspace, the last bool they inserted is deleted.
    // The length is reduced. 
    public bool Backspace() {
        bool ret = iBuffer[^1];
        iBuffer.RemoveAt(iBuffer.Count - 1);
        return ret;
    }

    // When the user types POP, the first bool is popped. Also helper function.
    // The length is reduced.
    public bool Pop() {
        return Pop(iBuffer);
    }

    private bool Pop(List<bool> b) {
        bool ret = b[0];
        b.RemoveAt(0);
        return ret;
    }

    public int BitLength() => iBuffer.Count;

    public void Print(string msg) {
        Debug.Log($"{msg} ({iBuffer.Count}): {string.Join("", iBuffer.Select(b => (b) ? 1 : 0))}");
    }

      /////////////////////////////
     ///String Public Interface///
    /////////////////////////////
    public void Clear() {
        iBuffer.Clear();
        cBuffer.Clear();
    }

    public bool EndsWith(string value) {
        if (cBuffer.Length < value.Length) return false;

        for (int i = 0; i < value.Length; i++) {
            if (cBuffer[cBuffer.Length - value.Length + i] != value[i]) return false;
        }

        History.Add(value);
        cBuffer.Clear();
        return true;
    }

}