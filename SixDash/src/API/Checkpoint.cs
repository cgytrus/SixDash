using System;
using System.Collections.Generic;

using JetBrains.Annotations;

using UnityEngine;

using Object = UnityEngine.Object;

namespace SixDash.API;

/// <summary>
/// APIs related to checkpoints.
/// </summary>
[PublicAPI]
public static class Checkpoint {
    /// <summary>
    /// Fired when a checkpoint is created.
    /// </summary>
    public static event Action<CheckpointScript>? place;

    /// <summary>
    /// Fired when a checkpoint is stored.
    /// </summary>
    public static event Action<CheckpointScript>? store;

    /// <summary>
    /// Fired when a checkpoint is removed.
    /// </summary>
    public static event Action? remove;

    /// <summary>
    /// List of all stored checkpoints.
    /// </summary>
    public static IReadOnlyList<CheckpointScript> allCheckpoints => checkpoints;

    /// <summary>
    /// The offset to use when calling <see cref="GetLatest"/> and <see cref="RemoveLatest"/>
    /// </summary>
    public static int current {
        get => _current;
        set => _current = Mathf.Clamp(value, 0, checkpoints.Count);
    }

    private static readonly List<CheckpointScript> checkpoints = new();
    private static int _current;

    private static int _originalApiCalls;

    internal static void Patch() {
        On.PlayerScript.MakeCheckpoint += (orig, self) => {
            orig(self);
            if(_originalApiCalls > 0) {
                _originalApiCalls--;
                return;
            }
            CheckpointScript checkpoint = PlayerScript.GetRecentCheckpoint().GetComponent<CheckpointScript>();
            place?.Invoke(checkpoint);
            checkpoint.Store();
        };

        On.PauseMenuManager.DeleteCheckpoint += (orig, self) => {
            orig(self);
            for(int i = checkpoints.Count - 1; i >= 0; i--)
                if(!checkpoints[i])
                    checkpoints.RemoveAt(i);
            remove?.Invoke();
        };

        World.levelLoading += () => {
            _current = 0;
            checkpoints.Clear();
        };
    }

    /// <summary>
    /// Creates and stores a checkpoint.<br/>
    /// Replaces <see cref="PlayerScript.MakeCheckpoint"/>.
    /// </summary>
    /// <returns>The created checkpoint.</returns>
    /// <seealso cref="Create"/>
    /// <seealso cref="Store"/>
    public static CheckpointScript? Mark() {
        CheckpointScript? checkpoint = Create();
        if(checkpoint)
            checkpoint!.Store();
        return checkpoint;
    }

    /// <summary>
    /// Creates a checkpoint in a <see cref="GameObject.SetActive">disabled</see> state.
    /// </summary>
    /// <returns>The created checkpoint.</returns>
    public static CheckpointScript? Create() {
        if(!Player.scriptInstance)
            return null;

        _originalApiCalls++;
        Player.scriptInstance!.MakeCheckpoint();

        GameObject checkpointObj = PlayerScript.GetRecentCheckpoint();
        checkpointObj.SetActive(false);
        CheckpointScript checkpoint = checkpointObj.GetComponent<CheckpointScript>();
        place?.Invoke(checkpoint);
        return checkpoint;
    }

    /// <summary>
    /// Stores a checkpoint in the checkpoint list and <see cref="GameObject.SetActive">enables</see> it.
    /// </summary>
    /// <param name="checkpoint">The checkpoint to tore in the list.</param>
    public static void Store(this CheckpointScript checkpoint) {
        checkpoints.Add(checkpoint);
        checkpoint.gameObject.SetActive(true);
        store?.Invoke(checkpoint);
    }

    /// <summary>
    /// Gets the latest <see cref="GameObject.activeInHierarchy">enabled</see> checkpoint from the checkpoint list,
    /// with the index offset by <see cref="current"/> from the list's end.<br/>
    /// Replaces <see cref="PlayerScript.GetRecentCheckpoint"/>.
    /// </summary>
    /// <returns>The acquired checkpoint.</returns>
    public static CheckpointScript? GetLatest() {
        int index = GetLatestIndex();
        return index < 0 ? null : checkpoints[index];
    }

    /// <summary>
    /// Removes the latest checkpoint stored in the checkpoint list,
    /// with index offset by <see cref="current"/> from the list's end.<br/>
    /// Replaces <see cref="PauseMenuManager.DeleteCheckpoint"/>.
    /// </summary>
    public static void RemoveLatest() {
        int index = GetLatestIndex();
        if(index < 0)
            return;
        Object.Destroy(checkpoints[index].gameObject);
        checkpoints.RemoveAt(index);
    }

    private static int GetLatestIndex() {
        for(int i = checkpoints.Count - 1 - current;; i--) {
            if(i < 0)
                break;
            if(!checkpoints[i] || !checkpoints[i].gameObject.activeInHierarchy)
                continue;
            if(i < checkpoints.Count)
                return i;
        }
        return -1;
    }
}
