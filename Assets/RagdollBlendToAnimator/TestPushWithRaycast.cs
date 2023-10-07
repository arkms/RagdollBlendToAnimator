using System.Collections;
using UnityEngine;

public class TestPushWithRaycast : MonoBehaviour
{
	public float m_pushForce = 30f;
	
	// Cache
	Camera m_cam;
    public RagDollControl m_characterRagDollControl;
    
    // Internal variables
	WaitForSeconds ws0_025 = new WaitForSeconds(0.025f);
	WaitForSeconds ws0_05 = new WaitForSeconds(0.05f);

	void Start()
	{
		m_cam = Camera.main;
	}

	void Update ()
	{
		if (Input.GetMouseButtonDown(0))
		{
			// Shooting from center of screen
			Ray ray = m_cam.ScreenPointToRay(new Vector3(Screen.width/2.0f, Screen.height/2.0f, 0f));
			if (Physics.Raycast(ray,out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Body")))
			{
				if (hit.rigidbody != null) // Security
				{
					m_characterRagDollControl.Ragdolled=true;
					
					// Effect
					StartCoroutine(PushCoroutine(hit.rigidbody, ray.direction, m_pushForce));
                    Debug.DrawLine(ray.origin, hit.point, Color.red, 1f);
				}
			}
		}

		//Reseteamos RagDoll
		if (Input.GetKeyDown(KeyCode.Space))
		{
			ResetRagDoll();
		}
	}

	IEnumerator PushCoroutine(Rigidbody _bobyPartRb, Vector3 _direction, float _force)
	{
		yield return ws0_025;
		_bobyPartRb.AddForce(_direction * _force, ForceMode.VelocityChange);
		yield return ws0_05;
		ResetRagDoll();
	}

    void ResetRagDoll()
    {
	    m_characterRagDollControl.Ragdolled = false;
    }
}
