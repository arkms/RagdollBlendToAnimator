using UnityEngine;
using System.Collections.Generic;

// Based on https://github.com/nbzeman/Ragdoll/blob/master/Assets/Scripts/RagdollHelper.cs

public class RagDollControl : MonoBehaviour
{
	enum RagdollState{ animated, ragdolled, blendToAnim };
	
	const float kMecanimToGetUpTransitionTime=0.05f;
	
	[Header("Settings")] 
	[SerializeField] bool m_adjustPosition;
	[SerializeField] bool m_adjustRotation;
    
    // Cache
    Transform m_transform;
    Animator m_animator;
    
    // Local variables
    RagdollState m_state = RagdollState.animated;
    Vector3 m_hipPositionRb, m_headPositionRb, m_feetPositionRb;
    List<BodyPart> m_bodyParts;
    Rigidbody[] m_ragdollRigidbodies;
    
    //Blend variables
    [Header("Blend")]
    [SerializeField] float m_ragdollToMecanimBlendTime = 0.5f;
    float m_ragdollingEndTime = -100; // Time control
    
    [Header("Override references")]
    public Transform m_leftFoot;
    public Transform m_rightFoot;

    // Enable or disable ragdoll
    public bool Ragdolled
	{
		get
		{
			return m_state!=RagdollState.animated;
		}
		
		set
		{
			if (value)
			{
				if (m_state != RagdollState.animated)
					return;
				
				SetRagDoll(true); // Turn on ragdoll
				m_animator.enabled = false;
				m_state=RagdollState.ragdolled;
			}
			else
			{
				if (m_state != RagdollState.ragdolled)
					return;
				
				SetRagDoll(false); // Turn off ragdoll
				m_ragdollingEndTime=Time.time; // save when we starting blending
				m_animator.enabled = true;
				m_state=RagdollState.blendToAnim;
				enabled = true;

				// update current position and rotation of rigidbodys
				int bodyPartCount = m_bodyParts.Count;
				for (int i = 0; i < bodyPartCount; i++)
				{
					Transform bodyTransform = m_bodyParts[i].transform;
					m_bodyParts[i].storedPosition = bodyTransform.position;
					m_bodyParts[i].storedRotation = bodyTransform.rotation;
				}

				// Get bones position and get point between feet
				Vector3 leftFootPosition = m_leftFoot ? m_leftFoot.position : m_animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
				Vector3 rightFootPosition = m_rightFoot ? m_rightFoot.position : m_animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
				m_feetPositionRb = 0.5f * (leftFootPosition + rightFootPosition);
				m_headPositionRb = m_animator.GetBoneTransform(HumanBodyBones.Head).position;
				m_hipPositionRb = m_animator.GetBoneTransform(HumanBodyBones.Hips).position;
				
			}	//if value==false	
		} //set
	} // Ragdolled

	// Turn on or turn off ragdool
	void SetRagDoll(bool _enable)
	{
		_enable = !_enable;
		foreach (Rigidbody rb in m_ragdollRigidbodies)
		{
			if(rb.transform != m_transform) // Is not root transform
				rb.isKinematic = _enable;
		}
	}
	
	void Start ()
	{
		m_transform = transform;
		m_animator = GetComponent<Animator>();
		m_ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
		m_bodyParts = new List<BodyPart>();

		if (!m_animator.isHuman) // Security
		{
			Debug.LogError("This only works on Humanoid");
			return;
		}
		
		Debug.Log(m_animator.GetBoneTransform(HumanBodyBones.LeftFoot), m_animator.GetBoneTransform(HumanBodyBones.LeftFoot));
		
		// Create list of body parts
		foreach (Rigidbody rb in m_ragdollRigidbodies)
		{
			if (rb.transform == m_transform) continue;
			BodyPart bodyPart=new BodyPart
			{
				transform = rb.transform
			};
			m_bodyParts.Add(bodyPart);
		}
		
		// Ragdoll start disabled
		SetRagDoll(false);
	}
	
	void LateUpdate()
	{
		if (m_state != RagdollState.blendToAnim)
			return;
		
		if (Time.time<=m_ragdollingEndTime+kMecanimToGetUpTransitionTime) // Blending time
		{
			if (m_adjustPosition)
			{
				// Set the position to set back to hips (Center of mass)
				Vector3 animatedToRagdolled = m_hipPositionRb - m_animator.GetBoneTransform(HumanBodyBones.Hips).position;
				Vector3 newRootPosition = m_transform.position + animatedToRagdolled;
				// Search the highest position of collision
				RaycastHit[] hits = Physics.RaycastAll(new Ray(newRootPosition,Vector3.down));
				newRootPosition.y=0;
				for (int i = 0; i< hits.Length; i++)
				{
					if (!hits[i].transform.IsChildOf(m_transform))
					{
						newRootPosition.y = Mathf.Max(newRootPosition.y, hits[i].point.y);
					}
				}

				m_transform.position = newRootPosition;
			}

			if (m_adjustRotation)
			{
				// Body look forward between head and feet
				Vector3 ragdolledDirection=m_headPositionRb-m_feetPositionRb;
				ragdolledDirection.y=0;
				
				Vector3 leftFootPosition = m_leftFoot ? m_leftFoot.position : m_animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
				Vector3 rightFootPosition = m_rightFoot ? m_rightFoot.position : m_animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
				Vector3 meanFeetPosition = 0.5f * (leftFootPosition + rightFootPosition);
				Vector3 animatedDirection = m_animator.GetBoneTransform(HumanBodyBones.Head).position - meanFeetPosition;
				animatedDirection.y=0;
				
				m_transform.rotation *= Quaternion.FromToRotation(animatedDirection.normalized, ragdolledDirection.normalized);
			}
			
		}
		
		// Clamp animation
		float ragdollBlendAmount = 1.0f - (Time.time-m_ragdollingEndTime-kMecanimToGetUpTransitionTime) / m_ragdollToMecanimBlendTime;
		ragdollBlendAmount = Mathf.Clamp01(ragdollBlendAmount);

		// Tween position and rotation between ragdoll and animation positions and rotation
		int bodyPartCount = m_bodyParts.Count;
		for (int i = 0; i < bodyPartCount; i++)
		{
			if (m_bodyParts[i].transform == m_transform)
				continue;
			
			// Only upadte hip position
			if (m_bodyParts[i].transform == m_animator.GetBoneTransform(HumanBodyBones.Hips))
				m_bodyParts[i].transform.position = Vector3.Lerp(m_bodyParts[i].transform.position, m_bodyParts[i].storedPosition, ragdollBlendAmount);
			
			m_bodyParts[i].transform.rotation = Quaternion.Slerp(m_bodyParts[i].transform.rotation, m_bodyParts[i].storedRotation, ragdollBlendAmount);
		}

		// blend is donde?
		if (ragdollBlendAmount==0)
		{
			m_state = RagdollState.animated;
			enabled = false;
		}
	}
	
	class BodyPart
	{
		public Transform transform;
		public Vector3 storedPosition;
		public Quaternion storedRotation;
	}
}
