﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.WSA.Persistence;
using UnityEngine.VR.WSA;
using HoloToolkit.Unity;

public class PersistenceManager : Singleton<PersistenceManager>
{
    public WorldAnchorStore store;

	// Use this for initialization
	void Start () {
        WorldAnchorStore.GetAsync(StoreLoaded);
	}

    /// <summary>
    /// Clears all anchors.
    /// </summary>
    public void ClearAllAnchors()
    {
        if(store != null)
        {
            store.Clear();
        }
        else
            DebugManager.Instance.PrintToInfoLog("Store uninitialised");
    }

    /// <summary>
    /// This is called once the world anchor store has been fetched.
    /// </summary>
    /// <param name="store">The store.</param>
    void StoreLoaded(WorldAnchorStore store)
    {
        this.store = store;
        GetAllAnchors();
    }

    /// <summary>
    /// Gets all anchors.
    /// </summary>
    void GetAllAnchors()
    {
        string[] ids = this.store.GetAllIds();
        DebugManager.Instance.PrintToInfoLog("persistence : " + store.anchorCount + " " + ids.Length);
        for (int index = 0; index < ids.Length; index++)
        {
            ///[0]: Marker type
            ///[1]: Marker confidence score
            ///[2]: Marker additional info(random number for now)
            string[] chunks = ids[index].Split(':');

            RecognisedObject obj = new RecognisedObject
            {
                type = chunks[0],
                score = float.Parse(chunks[1])
            };
            ObjectMarker om = ObjectLocator.Instance.AttemptToDropMarker(Vector3.zero, obj);
            om.markerName = ids[index];

            store.Load(ids[index], om.gameObject);
            DebugManager.Instance.PrintToInfoLog(ids[index] + "@" + om.transform.position);
        }
    }

    /// <summary>
    /// Adds the anchor.
    /// </summary>
    /// <param name="marker">The marker.</param>
    public void AddAnchor(ObjectMarker marker)
    {
        GameObject go = marker.gameObject;

        if (store == null)
        {
            DebugManager.Instance.PrintToInfoLog("Store uninitialised");
            return;
        }

        WorldAnchor wa = go.GetComponent<WorldAnchor>();
        if (wa == null)
        {  
            wa = go.AddComponent<WorldAnchor>();
        }
        else
            DebugManager.Instance.PrintToInfoLog("Already anchored:" + marker.markerName);


        bool saved = store.Save(marker.markerName, wa);
        DebugManager.Instance.PrintToInfoLog("saved:" + marker.markerName + "::" + saved + "@" + go.transform.position);
    }

    /// <summary>
    /// Deletes the anchor.
    /// </summary>
    /// <param name="om">The om.</param>
    public void DeleteAnchor(ObjectMarker om)
    {
        string markerName = om.markerName;
        WorldAnchor wa = om.GetComponent<WorldAnchor>();
        if (wa)
            DestroyImmediate(om.GetComponent<WorldAnchor>());

        DebugManager.Instance.PrintToInfoLog("Anchor " + markerName + " deleted: " + (store != null ? store.Delete(markerName).ToString() : "No store.") + ":: " + wa);
    }

    /// <summary>
    /// Determines whether the specified marker is anchored.
    /// </summary>
    /// <param name="om">The om.</param>
    /// <returns>
    ///   <c>true</c> if the specified om is anchored; otherwise, <c>false</c>.
    /// </returns>
    public bool IsAnchor(ObjectMarker om)
    {
        return (om.GetComponent<WorldAnchor>() != null);
    }
}