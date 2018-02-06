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
	
    void StoreLoaded(WorldAnchorStore store)
    {
        this.store = store;
        GetAllAnchors();
    }

    void GetAllAnchors()
    {
        string[] ids = this.store.GetAllIds();
        DebugManager.Instance.PrintToInfoLog("persistence :" + store.anchorCount);
        for (int index = 0; index < ids.Length; index++)
        {
            ObjectMarker om = ObjectLocator.Instance.CreateMarker();
            store.Load(ids[index], om.gameObject);
            DebugManager.Instance.PrintToInfoLog(ids[index] + "@" + om.transform.position);
        }
    }

	public void AddAnchor(GameObject go)
    {
        if (store == null)
        {
            DebugManager.Instance.PrintToInfoLog("Store uninitialised");
            return;
        }

        WorldAnchor wa = go.AddComponent<WorldAnchor>();

        bool saved = store.Save(Random.Range(0, 1000).ToString(), wa);
        DebugManager.Instance.PrintToInfoLog("saved:" + saved + "@" + go.transform.position);
    }
}
