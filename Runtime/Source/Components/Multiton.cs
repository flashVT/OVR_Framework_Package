//////////////////////////////////////////////////////////////////////////////////
//
//  Architecture of Scent(TM) Software Suite - OVR Unity Framework
//  Copyright (C) 2018-2019 OVR Tech LLC
//
//    OVR Unity Framework is proprietary software: you can modify it under the
//    terms of the Development Kit Agreement signed before receiving the
//    software. This Software is owned by OVR Tech LLC and any modifications,
//    changes, or alterations are subject to the terms of the Agreement.
//
//    OVR Unity Framework is distributed in the hope that it will be useful,for
//    testing purposes, and for feedback, but WITHOUT ANY WARRANTY; without even
//    the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
//    See the Agreement for more details.
//
//    You should have received a copy of the Development Kit Agreement before
//    receiving the OVR Unity Framework. If not, this software may be in violation
//    of the Agreement. Please contact info@ovrtechnology.com or click the link
//    below to request the document.
//
//  https://ovrtechnology.com/contact/
//
//////////////////////////////////////////////////////////////////////////////////

using OVR.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OVR.Components
{
  public abstract class Multiton<T> : MonoBehaviour where T : Multiton<T>
  {
    private static List<T> m_Instances = new List<T>();
    public static T Instance
    {
      get
      {
        if (m_Instances.Count == 0)
        {
          var preexisting = GameObject.FindObjectOfType(typeof(T)) as T;
          if (preexisting != null)
          {
            preexisting.Init();
            m_Instances.Add(preexisting);
          }
          return preexisting;
        }

        return m_Instances.Where(x => x.enabled).First();
      }
    }

    public static bool Exists()
    {
      return m_Instances.Any(x => x.enabled);
    }

    // If no other monobehaviour request the instance in an awake function
    // executing before this one, no need to search the object.
    protected virtual void Awake()
    {
      if (!m_Instances.Any(x => x == this))
      {
        var self = this as T;
        self.Init();
        Debug.Log(string.Format("[{0}] Awake\n[Scene] {1}\n[Path] {2}", typeof(T).Name, SceneManager.GetActiveScene().name, transform.GetPath()));
        m_Instances.Add(self);
      }
    }

    protected virtual void OnDestroy()
    {
      Debug.Log(string.Format("<b>[{0}]</b> OnDestroy\n[Scene] {1}\n[Path] {2}", typeof(T).Name, SceneManager.GetActiveScene().name, transform.GetPath()));
      m_Instances.RemoveAll(x => x == this);
    }

    // This function is called when the instance is used the first time
    // Put all the initializations you need here, as you would do in Awake
    public virtual void Init() { }

    // Make sure the instance isn't referenced anymore when the user quit, just in case.
    private void OnApplicationQuit()
    {
      m_Instances.Clear();
    }
  }
}
