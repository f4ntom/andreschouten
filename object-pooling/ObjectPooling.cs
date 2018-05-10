using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectPooling : MonoBehaviour
{
	private static ObjectPooling _instance;
	public static ObjectPooling Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = FindObjectOfType<ObjectPooling> ();

				if (_instance == null)
				{
					GameObject ObjectPoolingGO = new GameObject (typeof (ObjectPooling).Name);
					_instance = ObjectPoolingGO.AddComponent<ObjectPooling> ();
				}
			}
			return _instance;
		}
	}

	private Dictionary<GameObject, List<GameObject>> _pool = new Dictionary<GameObject, List<GameObject>> ();
	private Dictionary<GameObject, GameObject> _instantiatedObjects = new Dictionary<GameObject, GameObject> ();

	#region Unity methods
	private void Awake ()
	{
		_instance = this;
	}
	#endregion

	public void CreatePool (GameObject prefabGO, int poolSize)
	{
		if (prefabGO == null)
		{
			throw new ArgumentNullException ("prefabGO");
		}

		if (poolSize <= 0)
		{
			throw new ArgumentOutOfRangeException ("poolSize", "poolSize should be greater than zero.");
		}

		// if pool already contains the prefab just do nothing
		if (_pool.ContainsKey (prefabGO)) return;

		List<GameObject> pooledObjects = new List<GameObject> ();

		// create the desired number of pooled objects for the specified prefab
		for (int i = 0; i < poolSize; i++)
		{
			GameObject pooledGO = GameObject.Instantiate (prefabGO);
			pooledGO.SetActive (false);
			pooledGO.transform.SetParent (transform, false);
			pooledObjects.Add (pooledGO);
		}

		_pool.Add (prefabGO, pooledObjects);
	}

	// get the pooled object with the 
	// referenced MonoBehaviour attached to it
	public T GetPooledObject<T> (T prefab) where T : MonoBehaviour
	{
		List<GameObject> pooledObjects = null;
		GameObject prefabGO = prefab.gameObject;

		// try to get the list of pooled objects if there are any
		if (_pool.TryGetValue (prefabGO, out pooledObjects))
		{
			GameObject pooledGO = null;

			// remove all null references to objects
			// which were destroyed in the meantime
			// and get the first non-null reference
			while (pooledGO == null && pooledObjects.Count > 0)
			{
				pooledGO = pooledObjects.First ();
				pooledObjects.RemoveAt (0);
			}

			if (pooledGO != null)
			{
				// set object active 
				// and return the desired component
				pooledGO.SetActive (true);
				_instantiatedObjects.Add (pooledGO, prefabGO);
				return pooledGO.GetComponent<T> ();
			}
		}

		// if nothing returned yet just create a new instance
		// and return
		GameObject instantiatedGO = GameObject.Instantiate (prefabGO);
		_instantiatedObjects.Add (instantiatedGO, prefabGO);
		return instantiatedGO.GetComponent<T> ();
	}

	public void ReturnToPool (GameObject instantiatedGO)
	{
		GameObject prefabGO = null;

		// try to get the prefab we made the copy from
		if (_instantiatedObjects.TryGetValue (instantiatedGO, out prefabGO))
		{
			// if the pool does not contain that prefab
			// create a new list for that prefab
			if (!_pool.ContainsKey (prefabGO))
			{
				_pool.Add (prefabGO, new List<GameObject> ());
			}

			// deactivate the instance and return it to the pool
			instantiatedGO.SetActive (false);
			instantiatedGO.transform.SetParent (transform, false);
			_instantiatedObjects.Remove (instantiatedGO);
			_pool[prefabGO].Add (instantiatedGO);
		}
		else
		{
			// if it's not in the dictionary of instantiated objects
			// just destroy the gameobject
			Destroy (instantiatedGO);
		}
	}

	public void ResetPool (GameObject prefabGO)
	{
		// if prefab not existing in pool just do nothing
		if (_pool.ContainsKey (prefabGO)) return;

		List<GameObject> pooledObjects = _pool[prefabGO];

		// destroy all pooled objects
		for (int i = 0; i < pooledObjects.Count; i++)
		{
			Destroy (pooledObjects[i]);
		}

		pooledObjects.Clear ();
	}
}