using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class HotFixMonoBehavior
{
    protected GameObject gameObject;

    public void Init(GameObject go)
    {
        gameObject = go;
    }
    public virtual void Awake()
    {
        
    }
    // Start is called before the first frame update
    public virtual void Start()
    {
       
    }

    // Update is called once per frame
    public virtual void Update()
    {
         
    }

    public virtual void OnDestroy()
    {
       
    }

    public virtual void OnEnable()
    {
        
    }
    public virtual void OnDisable()
    {
       
    }
}