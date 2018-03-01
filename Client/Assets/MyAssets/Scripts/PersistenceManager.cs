using System.Collections;
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

    public void ClearAllAnchors()
    {
        if(store != null)
        {
            store.Clear();
        }
    }

    void StoreLoaded(WorldAnchorStore store)
    {
        this.store = store;
        GetAllAnchors();
    }

    void GetAllAnchors()
    {
        string[] ids = this.store.GetAllIds();
        DebugManager.Instance.PrintToInfoLog("persistence : " + store.anchorCount);
        for (int index = 0; index < ids.Length; index++)
        {
            ObjectMarker om = ObjectLocator.Instance.CreateMarker();

            ///[0]: Marker type
            ///[1]: Marker confidence score
            ///[2]: Marker additional info(random number for now)
            string[] chunks = ids[index].Split(':');
            om.SetProperties(Vector3.zero, chunks[0], float.Parse(chunks[1]));

            store.Load(ids[index], om.gameObject);
            DebugManager.Instance.PrintToInfoLog(ids[index] + "@" + om.transform.position);
        }
    }

	public void AddAnchor(ObjectMarker marker)
    {
        GameObject go = marker.gameObject;

        if (store == null)
        {
            DebugManager.Instance.PrintToInfoLog("Store uninitialised");
            return;
        }

        WorldAnchor wa = go.AddComponent<WorldAnchor>();

        bool saved = store.Save(marker.type + ":" + marker.confScore + ":" + Random.Range(0, 100000).ToString(), wa);
        DebugManager.Instance.PrintToInfoLog("saved:" + saved + "@" + go.transform.position);
    }
}