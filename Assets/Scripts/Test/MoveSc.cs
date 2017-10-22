using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveSc : MonoBehaviour
{

    public Vector3 moveend;
    public Vector3 viewend;
    public float speed;

    private Vector3 m_MoveBegin;
    private Vector3 m_MoveEnd;

    private Quaternion m_ViewBegin;
    private Quaternion m_ViewEnd;

    void Start()
    {
        m_MoveBegin = transform.position;
        m_MoveEnd = transform.position + moveend;

        m_ViewBegin = transform.rotation;
        m_ViewEnd = Quaternion.Euler(transform.eulerAngles + viewend);
    }
	
	void Update ()
	{
	    float time = Mathf.Sin(Time.time*speed)*0.5f + 0.5f;

        transform.position = Vector3.Lerp(m_MoveBegin, m_MoveEnd, time);

	    transform.rotation = Quaternion.Lerp(m_ViewBegin, m_ViewEnd, time);
	}
}
